using System.Net;
using System.Net.Mime;
using Business.Attribute;
using Business.Business.Interfaces.FileSystem;
using Business.Utils.Helper;
using BusinessModels.General;
using BusinessModels.System.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace ResApi.Controllers.Files;

[Route("api/[controller]")]
[ApiController]
public class FilesController(IOptions<AppSettings> options, IFileSystemBusinessLayer fileServe, IFolderSystemBusinessLayer folderServe) : ControllerBase
{
    private const long MaxFileSize = 10L * 1024L * 1024L * 1024L; // 10GB, adjust to your need
    private readonly string[] _permittedExtensions = [];
    private readonly string _targetFilePath = options.Value.FileFolder;


    [HttpGet("get-file")]
    public IActionResult GetFile(string id)
    {
        var file = fileServe.Get(id);
        if (file == null) return NotFound();
        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = file.FileName,
            Inline = true, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };
        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.ContentType = file.ContentType;
        Response.Headers.ContentType = file.ContentType;
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;
        return PhysicalFile(file.AbsolutePath, file.ContentType, true);
    }

    [HttpPost("upload-physical")]
    [DisableFormValueModelBinding]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadPhysical()
    {
        try
        {
            if (string.IsNullOrEmpty(Request.ContentType) || !MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", "The request couldn't be processed (Error 1).");
                return BadRequest(ModelState);
            }

            HttpContext.Request.Headers.TryGetValue("Folder", out StringValues folderValues);

            var user = HttpContext.User.Identity?.Name ?? string.Empty;
            var folderCodes = folderValues.ToString();

            if (string.IsNullOrEmpty(folderCodes))
            {
                ModelState.AddModelError("Header", "Folder path is required in the request header");
                return BadRequest(ModelState);
            }

            folderServe.GetRoot(user);
            var folder = folderServe.Get(user, folderCodes);
            if (folder == null)
            {
                ModelState.AddModelError("Folder", "Folder not found");
                return BadRequest(ModelState);
            }

            var boundary = MediaTypeHeaderValue.Parse(Request.ContentType).GetBoundary(int.MaxValue);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader && contentDisposition != null)
                {
                    if (!contentDisposition.HasFileContentDisposition())
                    {
                        ModelState.AddModelError("File", "The request couldn't be processed (Error 2).");
                    }
                    else
                    {
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value) ?? Path.GetRandomFileName();

                        FileInfoModel file = new FileInfoModel()
                        {
                            FileName = trustedFileNameForDisplay,
                        };
                        folderServe.CreateFile(folder, file);
                        (file.FileSize, file.ContentType) = await section.ProcessStreamedFileAndSave(file.AbsolutePath, ModelState);
                        await fileServe.UpdateAsync(file);
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            return Ok(ModelState);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("Exception", e.Message);
            return StatusCode(500, ModelState);
        }
    }
}
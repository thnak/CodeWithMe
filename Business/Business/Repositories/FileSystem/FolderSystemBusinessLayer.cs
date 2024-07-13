using System.Linq.Expressions;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.User;
using Business.Data.Interfaces.FileSystem;
using BusinessModels.General;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Business.Repositories.FileSystem;

public class FolderSystemBusinessLayer(IFolderSystemDatalayer folderSystemService, IFileSystemBusinessLayer fileSystemService, IUserBusinessLayer userService, IOptions<AppSettings> options) : IFolderSystemBusinessLayer
{
    private readonly string _workingDir = options.Value.FileFolder;

    public IAsyncEnumerable<FolderInfoModel> FindAsync(FilterDefinition<FolderInfoModel> filter, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(string keyWord, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public FolderInfoModel? Get(string key)
    {
        return folderSystemService.Get(key);
    }

    public IAsyncEnumerable<FolderInfoModel?> GetAsync(List<string> keys, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FolderInfoModel[], long)> GetAllAsync(int page, int size, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(FolderInfoModel model)
    {
        return folderSystemService.CreateAsync(model);
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FolderInfoModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        return folderSystemService.CreateAsync(models, cancellationTokenSource);
    }

    public Task<(bool, string)> UpdateAsync(FolderInfoModel model)
    {
        return folderSystemService.UpdateAsync(model);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FolderInfoModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        return folderSystemService.UpdateAsync(models, cancellationTokenSource);
    }

    public (bool, string) Delete(string key)
    {
        var folder = Get(key);
        if (folder == null) return (false, AppLang.Folder_could_not_be_found);

        if (folder.RelativePath == "/")
        {
            return (false, AppLang.Could_not_delete_root_folder);
        }

        var res = folderSystemService.Delete(key);
        if (res.Item1)
        {
            foreach (var content in folder.Contents)
            {
                if (content is { Type: FolderContentType.File or FolderContentType.HiddenFile })
                    fileSystemService.Delete(content.Id);
                else
                {
                    Delete(content.Id);
                }
            }
        }

        return res;
    }

    public FolderInfoModel? Get(string username, string relativePath, bool hashed = true)
    {
        username = hashed ? username : username.ComputeSha256Hash();
        var user = GetUser(username);
        return folderSystemService.Get(user?.UserName ?? string.Empty, relativePath);
    }

    public FolderInfoModel? GetRoot(string username)
    {
        var user = GetUser(username);
        if (user == null) return default;

        var folder = Get(user.Folder);
        if (folder == null)
        {
            folder = new FolderInfoModel()
            {
                RelativePath = "/",
                FolderName = "Home",
                ModifiedDate = DateTime.UtcNow,
                Username = user.UserName
            };
            var res = CreateAsync(folder).Result;
            if (res.Item1)
            {
                return Get(folder.Id.ToString());
            }

            return default;
        }

        return folder;
    }

    public (bool, string) CreateFile(FolderInfoModel folder, FileInfoModel file)
    {
        var newIndex = folder.Contents.Count + 1;
        var dateString = DateTime.UtcNow.ToString("dd-MM-yyy");
        var path = Path.Combine(_workingDir, dateString);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var filePath = Path.Combine(path, $"_file_{newIndex}.bin");
        file.AbsolutePath = filePath;
        var res = fileSystemService.CreateAsync(file).Result;
        if (res.Item1)
        {
            folder.Contents.Add(new FolderContent()
            {
                Id = file.Id.ToString(),
                Type = FolderContentType.File
            });
            UpdateAsync(folder);
            var _folder = Get(folder.Id.ToString())!;
            folder.Contents = _folder.Contents;
        }

        return res;
    }

    public (bool, string) CreateFile(string userName, FileInfoModel file)
    {
        var user = userService.Get(userName) ?? userService.GetAnonymous();
        var folder = GetRoot(user.UserName)!;
        return CreateFile(folder, file);
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public async Task<long> GetFolderByteSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        var folders = folderSystemService.Where(predicate, cancellationTokenSource);
        long total = 0;
        await foreach (var folder in folders)
        {
            foreach (var content in folder.Contents)
            {
                if (content is { Type: FolderContentType.File or FolderContentType.HiddenFile })
                {
                    var file = fileSystemService.Get(content.Id);
                    if (file == null)
                    {
                        Console.WriteLine($@"[Error] file by id {content} can not be found");
                        continue;
                    }

                    total += file.FileSize;
                }
                else
                {
                    ObjectId id = ObjectId.Parse(content.Id);
                    total += await GetFolderByteSize(e => e.Id == id, cancellationTokenSource);
                }
            }
        }

        return total;
    }

    public async Task<(long, long)> GetFolderContentsSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        var folders = folderSystemService.Where(predicate, cancellationTokenSource);
        long totalFolders = 0;
        long totalFiles = 0;
        await foreach (var folder in folders)
        {
            foreach (var content in folder.Contents)
            {
                if (content is { Type: FolderContentType.File or FolderContentType.HiddenFile })
                {
                    totalFiles++;
                }
                else
                {
                    ObjectId id = ObjectId.Parse(content.Id);
                    var (numFolders, numFiles) = await GetFolderContentsSize(e => e.Id == id, cancellationTokenSource);
                    totalFiles += numFiles;
                    totalFolders += numFolders;
                }
            }
        }

        return (totalFolders, totalFiles);
    }

    #region Private Mothods

    /// <summary>
    /// Get user. if user by the name that is not found return Anonymous user
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    private UserModel? GetUser(string username)
    {
        if (string.IsNullOrEmpty(username)) username = "Anonymous";
        var user = userService.Get(username);
        return user;
    }

    #endregion
}
﻿using System.Collections.Concurrent;
using Business.Business.Interfaces.FileSystem;
using Business.Services.Interfaces;
using Business.Utils.Helper;
using BusinessModels.General.EnumModel;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Business.Services.Services;

public class ThumbnailService(IServiceProvider serviceProvider, ILogger<ThumbnailService> logger) : IThumbnailService, IDisposable
{
    private readonly BlockingCollection<string> _thumbnailQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(8);
    private const int MaxDimension = 480; // Maximum width or height
    private bool _isProcessing;

    // To resolve database and image services
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Task AddThumbnailRequest(string imageId)
    {
        _thumbnailQueue.Add(imageId);
        // StartProcessing();
        return Task.CompletedTask;
    }

    private void StartProcessing()
    {
        if (_isProcessing)
        {
            return;
        }

        _isProcessing = true;
        Task.Run(() => StartAsync(_cancellationTokenSource.Token));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Thumbnail Service started.");

        // Continue processing until the app shuts down or cancellation is requested
        
        while (_thumbnailQueue.TryTake(out string? imageId, -1, cancellationToken))
        {
            if (string.IsNullOrEmpty(imageId)) continue;
            try
            {
                await _queueSemaphore.WaitAsync(cancellationToken); // Wait for a thumbnail request
                var id = imageId;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessThumbnailAsync(id, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error creating thumbnail for image {id}: {ex.Message}");
                    }
                    finally
                    {
                        _queueSemaphore.Release();
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Canceled");
            }
        }


        logger.LogInformation("Thumbnail Service stopped.");
    }

    private async Task ProcessThumbnailAsync(string imageId, CancellationToken cancellationToken)
    {
        try
        {
            // Use your service provider to resolve necessary services (DB access, image processing)
            using var scope = serviceProvider.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileSystemBusinessLayer>(); // Assumed IImageService handles image fetching

            // Fetch the image from the database
            var fileInfo = fileService.Get(imageId);
            if (fileInfo == null)
            {
                logger.LogWarning($"File with ID {imageId} not found.");
                return;
            }

            // check if file is an image 
            if (fileInfo.ContentType.IsImageFile())
            {
                await CreateImageThumbnail(fileInfo, fileService, cancellationToken);
            }
        }
        catch (Exception e)
        {
            logger.LogError($"Error creating thumbnail for image {imageId}: {e.Message}");
        }
    }

    private async Task CreateImageThumbnail(FileInfoModel fileInfo, IFileSystemBusinessLayer fileService, CancellationToken cancellationToken)
    {
        // Load the image from the absolute path
        var imagePath = fileInfo.AbsolutePath;
        if (!File.Exists(imagePath))
        {
            logger.LogWarning($"File at path {imagePath} does not exist.");
            return;
        }

        await using var imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var image = await Image.LoadAsync(imageStream, cancellationToken);

        // Define thumbnail size with aspect ratio
        var width = image.Width;
        var height = image.Height;

        if (width > height)
        {
            height = (int)(height * (MaxDimension / (double)width));
            width = MaxDimension;
        }
        else
        {
            width = (int)(width * (MaxDimension / (double)height));
            height = MaxDimension;
        }

        // Create a thumbnail
        using var thumbnailStream = new MemoryStream();
        using var extendedImage = new MemoryStream();

        await image.SaveAsWebpAsync(extendedImage, cancellationToken);

        image.Mutate(x => x.Resize(width, height)); // Resize with aspect ratio
        await image.SaveAsWebpAsync(thumbnailStream, cancellationToken); // Save as JPEG

        // Define the thumbnail path
        var thumbnailFileName = $"{fileInfo.Id}_thumb.webp";
        var extendedFileName = $"{fileInfo.Id}_ext.webp";
        var thumbnailPath = Path.Combine(Path.GetDirectoryName(imagePath)!, "thumbnails", thumbnailFileName);
        var extendImagePath = Path.Combine(Path.GetDirectoryName(imagePath)!, "thumbnails", extendedFileName);

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

        // Save the thumbnail

        var thumbnailSize = await SaveStream(thumbnailStream, thumbnailPath, cancellationToken);
        var extendedImageSize = await SaveStream(extendedImage, extendImagePath, cancellationToken);

        FileInfoModel thumbnailFile = new FileInfoModel()
        {
            FileName = thumbnailFileName,
            AbsolutePath = thumbnailPath,
            FileSize = thumbnailSize,
            Type = FileContentType.ThumbnailFile,
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
            ContentType = "image/webp"
        };

        FileInfoModel extendedFile = new FileInfoModel()
        {
            FileName = extendedFileName,
            AbsolutePath = extendImagePath,
            FileSize = extendedImageSize,
            Type = FileContentType.ThumbnailWebpFile,
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
            ContentType = "image/webp"
        };

        // Update the fileInfo with the thumbnail path
        fileInfo.Thumbnail = thumbnailFile.Id.ToString();
        fileInfo.ExtendResource.Add(new FileContents()
        {
            Id = extendedFile.Id.ToString(),
            Type = FileContentType.ThumbnailWebpFile
        });


        await fileService.CreateAsync(thumbnailFile, cancellationToken);
        await fileService.CreateAsync(extendedFile, cancellationToken);
        await fileService.UpdateAsync(fileInfo, cancellationToken); // Save updated file info to DB

        // use image

        lock (_thumbnailQueue)
        {
            logger.LogInformation($"Thumbnail created for image {fileInfo.Id}. {_thumbnailQueue.Count}");
        }
    }


    private async Task<long> SaveStream(Stream stream, string thumbnailPath, CancellationToken cancellationToken = default)
    {
        stream.SeekBeginOrigin(); // Reset stream position
        await using var thumbnailFileStream = new FileStream(thumbnailPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await stream.CopyToAsync(thumbnailFileStream, cancellationToken);
        thumbnailFileStream.SeekBeginOrigin();
        return thumbnailFileStream.Length;
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel(); // Stop the background process
    }

    public void Dispose()
    {
        _queueSemaphore.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
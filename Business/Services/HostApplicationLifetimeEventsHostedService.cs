using Business.Data.Interfaces.Advertisement;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.User;
using Business.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services;

public class HostApplicationLifetimeEventsHostedService(IHostApplicationLifetime hostApplicationLifetime, IServiceScopeFactory serviceScopeFactory, ILogger<HostApplicationLifetimeEventsHostedService> logger) : IHostedService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
        hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        hostApplicationLifetime.ApplicationStopped.Register(OnStopped);
        return Task.CompletedTask;
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cancellationTokenSource.CancelAsync();
    }

    private void OnStarted()
    {
        logger.LogInformation("OnStarted");
        using var scope = serviceScopeFactory.CreateScope();
        scope.ServiceProvider.GetService<IUserDataLayer>()!.InitializeAsync();
        scope.ServiceProvider.GetService<IFileSystemDatalayer>()?.InitializeAsync();
        scope.ServiceProvider.GetService<IFolderSystemDatalayer>()?.InitializeAsync();
        scope.ServiceProvider.GetService<IAdvertisementDataLayer>()?.InitializeAsync();
        var thumbnailService = scope.ServiceProvider.GetService<IThumbnailService>();
        if (thumbnailService != null)
        {
            _ = Task.Run(() => thumbnailService.StartAsync(_cancellationTokenSource.Token));
        }
    }

    private void OnStopping()
    {
        logger.LogInformation("OnStopping");
    }

    private void OnStopped()
    {
        logger.LogInformation("OnStopped");
    }
}
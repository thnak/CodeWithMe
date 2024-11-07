﻿using Business.Services.TaskQueueServices.Base;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Services.TaskQueueServices;

public class ParallelQueuedHostedService(IParallelBackgroundTaskQueue parallelBackgroundTaskQueue, IOptions<AppSettings> options, ILogger<ParallelQueuedHostedService> logger) : BackgroundService
{
    private readonly TaskFactory _factory = new(new LimitedConcurrencyLevelTaskScheduler(options.Value.BackgroundQueue.MaxParallelThreads));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("""{Name} is running with max {threads} threads.""", nameof(ParallelQueuedHostedService), options.Value.BackgroundQueue.MaxParallelThreads);
        return ProcessTaskQueueAsync(stoppingToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        // int count = 0;
        // int maxCount = Environment.ProcessorCount;
        // List<Task> tasks = [];
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                while (parallelBackgroundTaskQueue.TryDequeue(out Func<CancellationToken, ValueTask>? workItem))
                {
                    _ = _factory.StartNew(async () => await workItem(stoppingToken), stoppingToken).ConfigureAwait(false);
                    // var item = workItem;
                    // var task = _factory.StartNew(async () => await item(stoppingToken), stoppingToken);
                    // tasks.Add(task);
                    // count++;
                    // if (count != maxCount) continue;
                    // var worked = await Task.WhenAny(tasks);
                    // tasks.Remove(worked);
                    // if (parallelBackgroundTaskQueue.TryDequeue(out Func<CancellationToken, ValueTask>? workItem2))
                    // {
                    //     logger.LogInformation("Add task when a parallel task queue finished.");
                    //     tasks.Add(_factory.StartNew(async () => await workItem2(stoppingToken), stoppingToken));
                    // }
                    // logger.LogInformation("reset counter");
                    // count = 0;
                }

                // await Task.WhenAll(tasks);
                // tasks.Clear();
                // count = 0;
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(ParallelQueuedHostedService)} is stopping.");
        await base.StopAsync(stoppingToken);
    }
}
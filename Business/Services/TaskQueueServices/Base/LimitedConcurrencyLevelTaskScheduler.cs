﻿namespace Business.Services.TaskQueueServices.Base;

public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
{
    // Indicates whether the current thread is processing work items.
    [ThreadStatic] private static bool _currentThreadIsProcessingItems;

    /// <summary>
    /// The list of tasks to be executed, protected by lock(_lock)
    /// </summary>
    private readonly LinkedList<Task> _tasks = new();

    private readonly Lock _lock = new();

    /// <summary>
    /// The maximum concurrency level allowed by this scheduler.
    /// </summary>
    private readonly int _maxDegreeOfParallelism;

    /// <summary>
    /// Indicates whether the scheduler is currently processing work items.
    /// </summary>
    private int _delegatesQueuedOrRunning;

    /// <summary>
    /// Creates a new instance with the specified degree of parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <summary>
    /// Queues a task to the scheduler.
    /// </summary>
    /// <param name="task"></param>
    protected sealed override void QueueTask(Task task)
    {
        // Add the task to the list of tasks to be processed.  If there aren't enough
        // delegates currently queued or running to process tasks, schedule another.
        lock (_lock)
        {
            _tasks.AddLast(task);
            if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
            {
                ++_delegatesQueuedOrRunning;
                NotifyThreadPoolOfPendingWork();
            }
        }
    }

    /// <summary>
    /// Inform the ThreadPool that there's work to be executed for this scheduler.
    /// </summary>
    private void NotifyThreadPoolOfPendingWork()
    {
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            // Note that the current thread is now processing work items.
            // This is necessary to enable inlining of tasks into this thread.
            _currentThreadIsProcessingItems = true;
            try
            {
                // Process all available items in the queue.
                while (true)
                {
                    Task? item;
                    lock (_lock)
                    {
                        // When there are no more items to be processed,
                        // note that we're done processing, and get out.
                        if (_tasks.Count == 0)
                        {
                            --_delegatesQueuedOrRunning;
                            break;
                        }

                        // Get the next item from the queue
                        item = _tasks.First?.Value;
                        _tasks.RemoveFirst();
                    }

                    // Execute the task we pulled out of the queue
                    if (item != null) TryExecuteTask(item);
                }
            }
            // We're done processing items on the current thread
            finally
            {
                _currentThreadIsProcessingItems = false;
            }
        }, null);
    }

    /// <summary>
    /// Attempts to execute the specified task on the current thread.
    /// </summary>
    /// <param name="task"></param>
    /// <param name="taskWasPreviouslyQueued"></param>
    /// <returns></returns>
    protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // If this thread isn't already processing a task, we don't support inlining
        if (!_currentThreadIsProcessingItems) return false;

        // If the task was previously queued, remove it from the queue
        if (taskWasPreviouslyQueued)
            // Try to run the task.
            if (TryDequeue(task))
                return TryExecuteTask(task);
            else
                return false;
        return TryExecuteTask(task);
    }

    /// <summary>
    /// Attempt to remove a previously scheduled task from the scheduler.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    protected sealed override bool TryDequeue(Task task)
    {
        lock (_lock) return _tasks.Remove(task);
    }

    /// <summary>
    /// Gets the maximum concurrency level supported by this scheduler.
    /// </summary>
    public sealed override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

    /// <summary>
    /// Gets an enumerable of the tasks currently scheduled on this scheduler.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    protected sealed override IEnumerable<Task> GetScheduledTasks()
    {
        bool lockTaken = false;
        try
        {
            Monitor.TryEnter(_tasks, ref lockTaken);
            if (lockTaken) return _tasks;
            else throw new NotSupportedException();
        }
        finally
        {
            if (lockTaken) Monitor.Exit(_tasks);
        }
    }
}
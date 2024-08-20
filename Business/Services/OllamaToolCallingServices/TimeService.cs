﻿using System.Globalization;
using Business.Services.OllamaToolCallingServices.Interfaces;

namespace Business.Services.OllamaToolCallingServices;


public class TimeService : ITimeService
{
    public Task<string> GetCurrentTimeStamp(bool useUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(useUtc ? DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss") : DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<string> GetCurrentHour(bool useUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(useUtc ? DateTime.UtcNow.ToString("HH:mm") : DateTime.Now.ToString("HH:mm"));
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<string> CompareTime(string firstTime, string secondTime, string timeFormat = "HH:MM:SS", CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateTime.TryParseExact(firstTime, timeFormat, null, DateTimeStyles.None, out DateTime time1))
            {
                return Task.FromResult($"the {nameof(firstTime)} was wrong format. please try again");
            }

            if (!DateTime.TryParseExact(secondTime, timeFormat, null, DateTimeStyles.None, out DateTime time2))
            {
                return Task.FromResult($"the {nameof(secondTime)} was wrong format. please try again");
            }

            if (time1 == time2)
                return Task.FromResult("0");
            if (time1 > time2)
                return Task.FromResult("1");
            return Task.FromResult("-1");
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<string> TimeDifference(string timeString1, string timeString2, string timeFormat = "HH:MM:SS", CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateTime.TryParseExact(timeString1, timeFormat, null, DateTimeStyles.None, out DateTime time1))
            {
                return Task.FromResult($"the {nameof(timeString1)} was wrong format. please try again with {timeFormat}");
            }

            if (!DateTime.TryParseExact(timeString2, timeFormat, null, DateTimeStyles.None, out DateTime time2))
            {
                return Task.FromResult($"the {nameof(timeString2)} was wrong format. please try again with {timeFormat}");
            }

            if (time1 <= time2)
            {
                (time1, time2) = (time2, time1);
            }

            var timeSpan = time1 - time2;
            return Task.FromResult(timeSpan.ToString(@"hh\:mm\:ss"));
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }
}
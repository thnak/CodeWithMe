﻿using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace Business.Services.Http.CircuitBreakers;

public class IoTCircuitBreakerService(IOptions<AppSettings> options, ILogger<IoTCircuitBreakerService> logger)
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker = Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: options.Value.IoTCircuitBreaker.ExceptionsAllowedBeforeBreaking, durationOfBreak: TimeSpan.FromSeconds(options.Value.IoTCircuitBreaker.DurationOfBreakInSecond));

    // Break after 5 failures
    // Stop for 30 seconds

    public async Task<bool> TryProcessRequest(Func<Task> process)
    {
        if (_circuitBreaker.CircuitState == CircuitState.Open)
        {
            logger.LogWarning("Circuit is open, rejecting request.");
            return false;
        }

        try
        {
            await _circuitBreaker.ExecuteAsync(async () => { await process(); });
            return true;
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuit is open, rejecting request.");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Request failed: {ex.Message}");
            return false;
        }
    }
}
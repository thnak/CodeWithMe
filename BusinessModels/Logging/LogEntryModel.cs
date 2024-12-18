﻿using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.Logging;

public class LogEntryModel(LogLevel logLevel, string loggerName, string message, string? exception = null)
{
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string LogLevel { get; set; } = logLevel.ToString();
    public string Logger { get; set; } = loggerName;
    public string Message { get; set; } = message;
    public string Exception { get; set; } = exception ?? string.Empty;
}
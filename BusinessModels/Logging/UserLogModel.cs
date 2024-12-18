﻿using BusinessModels.Base;
using MessagePack;

namespace BusinessModels.Logging;

[MessagePackObject]
public class UserLogModel : BaseModelEntry
{
    [Key(1)] public string LogId { get; set; } = Guid.NewGuid().ToString();

    [Key(2)] public int Time { get; set; }


    [Key(3)] public string UserId { get; set; } = string.Empty;


    [Key(4)] public string ObjectName { get; set; } = string.Empty;


    [Key(5)] public string Classify { get; set; } = string.Empty;

    [Key(6)] public string Action { get; set; } = string.Empty;

    [Key(7)] public string OldValue { get; set; } = string.Empty;

    [Key(8)] public string NewValue { get; set; } = string.Empty;

    [Key(9)] public string Note { get; set; } = string.Empty;
}
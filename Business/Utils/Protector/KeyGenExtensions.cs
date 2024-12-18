﻿using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;

namespace Business.Utils.Protector;

public static class KeyGenExtensions
{
    public static string GenerateAliasKey(this ObjectId fileId, string salt)
    {
        using var sha256 = SHA256.Create();
        // Combine ObjectId, salt, and timestamp for enhanced uniqueness
        var input = $"{fileId}{salt}{DateTime.UtcNow.Ticks}";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Convert to hexadecimal and take the first 12 characters
        return BitConverter.ToString(hash).Replace("-", "").Substring(0, 12);
    }

    public static string GenerateAliasKey(this SHA256 sha256, ObjectId fileId, string salt)
    {
        // Combine ObjectId, salt, and timestamp for enhanced uniqueness
        var input = $"{fileId}{salt}{DateTime.UtcNow.Ticks}";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Convert to hexadecimal and take the first 12 characters
        return BitConverter.ToString(hash).Replace("-", "").Substring(0, 12);
    }
}
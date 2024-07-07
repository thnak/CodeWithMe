using System.Text.Json;
using Microsoft.JSInterop;
using Web.Client.Models;

namespace Web.Client.Services;

public class ProtectedSessionStorage(IJSRuntime jsRuntime)
{
    /// <summary>
    ///     Handler function to get key
    /// </summary>
    public Func<Task<string>>? KeyHandler { get; set; }

    private async Task<string> InitializeKeyAsync()
    {

        // Check if a key already exists
        var key = await jsRuntime.InvokeAsync<string>("protectedSessionStorage.getItem", "encryptionKey");
        if (string.IsNullOrEmpty(key))
        {
            // Generate a new key if none exists
            key = await jsRuntime.InvokeAsync<string>("protectedSessionStorage.generateKey");
            await jsRuntime.InvokeVoidAsync("protectedSessionStorage.setItem", "encryptionKey", key);
        }
        return key;
    }

    private Task<string> GetKey()
    {
        if (KeyHandler != null)
        {
            return KeyHandler.Invoke();
        }
        return InitializeKeyAsync();
    }

    public async Task SetAsync(string key, string value)
    {
        var password = await GetKey();
        var result = await jsRuntime.InvokeAsync<Dictionary<string, object>>("protectedSessionStorage.encryptWithPassword", password, value);
        result.TryGetValue("iv", out var iv);
        result.TryGetValue("data", out var encryptedData);
        result.TryGetValue("salt", out var salt);

        if (encryptedData != null) await jsRuntime.InvokeVoidAsync("protectedSessionStorage.setItem", key, encryptedData.ToString());
        if (iv != null) await jsRuntime.InvokeVoidAsync("protectedSessionStorage.setItem", key + "_iv", iv.ToString());
        if (salt != null) await jsRuntime.InvokeVoidAsync("protectedSessionStorage.setItem", key + "_salt", salt.ToString());
    }

    public async Task SetAsync(string key, object value)
    {
        var textPlant = JsonSerializer.Serialize(value);
        await SetAsync(key, textPlant);
    }


    public async Task<string> GetAsync(string key)
    {
        var password = await GetKey();
        var iv = await jsRuntime.InvokeAsync<string>("protectedSessionStorage.getItem", key + "_iv");
        var encryptedData = await jsRuntime.InvokeAsync<string>("protectedSessionStorage.getItem", key);
        var salt = await jsRuntime.InvokeAsync<string>("protectedSessionStorage.getItem", key + "_salt");

        if (string.IsNullOrEmpty(iv) || string.IsNullOrEmpty(encryptedData) || string.IsNullOrEmpty(salt))
        {
            return string.Empty;
        }

        return await jsRuntime.InvokeAsync<string>("protectedSessionStorage.decryptWithPassword", password, iv, encryptedData, salt);
    }

    public async Task<ProtectedBrowserStorageResult<T>> GetAsync<T>(string key)
    {
        var textPlan = await GetAsync(key);
        try
        {
            var re = JsonSerializer.Deserialize<T>(textPlan);
            return new ProtectedBrowserStorageResult<T>(true, re);
        }
        catch (Exception)
        {
            //
        }
        return new ProtectedBrowserStorageResult<T>(false, default);
    }

    public async Task RemoveAsync(string key)
    {
        await jsRuntime.InvokeVoidAsync("protectedSessionStorage.removeItem", key);
        await jsRuntime.InvokeVoidAsync("protectedSessionStorage.removeItem", key + "_iv");
        await jsRuntime.InvokeVoidAsync("protectedSessionStorage.removeItem", key + "_salt");
    }
}
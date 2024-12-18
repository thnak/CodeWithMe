using System.Xml.Linq;
using Business.Data.Interfaces;
using Business.Models.Authenticate;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Business.KeyManagement;

public interface IMongoDbXmlKeyProtectorRepository : IXmlRepository
{
    /// <summary>
    ///     Xóa key quá khứ
    /// </summary>
    /// <param name="cutoffDate"></param>
    /// <returns></returns>
    long CleanupOldKeys(DateTime cutoffDate);
}

public class MongoDbXmlKeyProtectorRepository : IMongoDbXmlKeyProtectorRepository
{
    private readonly IMongoCollection<DataProtectionKey> _collection;

    public MongoDbXmlKeyProtectorRepository(IServiceScopeFactory factory)
    {
        using var scope = factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IMongoDataLayerContext>();
        _collection = context.MongoDatabase.GetCollection<DataProtectionKey>("DataProtectionKeys");
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var keys = _collection.Find(FilterDefinition<DataProtectionKey>.Empty).ToList();
        return keys.Select(k => XElement.Parse(k.Xml)).ToList();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        var key = new DataProtectionKey
        {
            FriendlyName = friendlyName,
            Xml = element.ToString(SaveOptions.DisableFormatting)
        };
        _collection.InsertOne(key);
    }

    public long CleanupOldKeys(DateTime cutoffDate)
    {
        var filter = Builders<DataProtectionKey>.Filter.Lte(x => x.CreationTime, cutoffDate);
        var count = _collection.CountDocuments(filter);
        _collection.DeleteMany(filter);
        return count;
    }
}
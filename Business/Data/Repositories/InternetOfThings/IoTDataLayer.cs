﻿using System.Linq.Expressions;
using Business.Data.Interfaces;
using Business.Data.Interfaces.InternetOfThings;
using Business.Models;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Business.Data.Repositories.InternetOfThings;

public class IoTDataLayer : IIoTDataLayer
{
    public IoTDataLayer(IMongoDataLayerContext context, ILogger<IoTDataLayer> logger)
    {
        var options = new CreateCollectionOptions
        {
            TimeSeriesOptions = new TimeSeriesOptions("timestamp", "deviceId", TimeSeriesGranularity.Seconds, new Optional<int?>(), new Optional<int?>())
        };
        context.MongoDatabase.CreateCollection("IotDB", options);
        _dataDb = context.MongoDatabase.GetCollection<IoTRecord>("IotDB");
        this.logger = logger;
    }

    private const string SearchIndexString = "UserSearchIndex";
    private readonly IMongoCollection<IoTRecord> _dataDb;
    private readonly ILogger<IoTDataLayer> logger;

    private readonly SemaphoreSlim _semaphore = new(1000, 1000);

    public Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((true, string.Empty));
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<IoTRecord, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> FindAsync(FilterDefinition<IoTRecord> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<IoTRecord, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> Where(Expression<Func<IoTRecord, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<IoTRecord, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IoTRecord? Get(string key)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(IoTRecord[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> CreateAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return (true, AppLang.Create_successfully);
        }
        catch (Exception e)
        {
            logger.LogError(e, null);
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<IoTRecord> updates, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }
}
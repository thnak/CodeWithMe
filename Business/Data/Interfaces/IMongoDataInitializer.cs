namespace Business.Data.Interfaces;

public interface IMongoDataInitializer
{
    Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default);
}
using System;
using System.Threading;
using System.Threading.Tasks;
using IbasSupport.Web.Models;
using Microsoft.Azure.Cosmos;

namespace IbasSupport.Web.Services
{
    public interface ISupportRepository
    {
        Task<SupportMessage> CreateAsync(SupportMessage item, CancellationToken ct = default);
    }

    public class CosmosSupportRepository : ISupportRepository, IAsyncDisposable
    {
        private readonly CosmosClient _client;
        private readonly Container _container;

        public CosmosSupportRepository(string connectionString, string database, string container)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Missing Cosmos connection string");
            if (string.IsNullOrWhiteSpace(database))        throw new ArgumentException("Missing Cosmos database name");
            if (string.IsNullOrWhiteSpace(container))       throw new ArgumentException("Missing Cosmos container name");

            _client = new CosmosClient(connectionString, new CosmosClientOptions
            {
                ApplicationName = "IbasSupport.Web"
            });

            _container = _client.GetContainer(database, container);
        }

        public async Task<SupportMessage> CreateAsync(SupportMessage item, CancellationToken ct = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(item.category)) throw new ArgumentException("Partition key 'category' is required");
            if (string.IsNullOrWhiteSpace(item.id))
                item.id = Guid.NewGuid().ToString("N"); // generate if missing

            var response = await _container.CreateItemAsync(item, new PartitionKey(item.category), cancellationToken: ct);
            return response.Resource;
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
            await Task.CompletedTask;
        }
    }
}
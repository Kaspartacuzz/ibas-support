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

            // 1) Cosmos id (string) – guid hvis mangler
            if (string.IsNullOrWhiteSpace(item.id))
                item.id = Guid.NewGuid().ToString("N");

            // 2) requestId – sekvens per partition/kategori
            if (!item.requestId.HasValue || item.requestId <= 0)
                item.requestId = await GetNextCounterAsync($"request-counter::{item.category}", item.category, ct);

            // 3) userId – global sekvens (kun hvis user findes og mangler id)
            if (item.user is not null && (!item.user.userId.HasValue || item.user.userId <= 0))
                item.user.userId = await GetNextCounterAsync("user-counter", "meta", ct);

            // 4) dealerId – global sekvens (kun hvis dealer findes og mangler id)
            if (item.dealer is not null && (!item.dealer.dealerId.HasValue || item.dealer.dealerId <= 0))
                item.dealer.dealerId = await GetNextCounterAsync("dealer-counter", "meta", ct);

            var response = await _container.CreateItemAsync(item, new PartitionKey(item.category), cancellationToken: ct);
            return response.Resource;
        }

        private async Task<int> GetNextCounterAsync(string counterId, string partition, CancellationToken ct)
        {
            // Atomisk increment via PATCH – virker inden for partitionen
            try
            {
                var ops = new[] { PatchOperation.Increment("/value", 1) };
                var patched = await _container.PatchItemAsync<Counter>(counterId, new PartitionKey(partition), ops, cancellationToken: ct);
                return patched.Resource.value;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Første gang: opret tæller med værdi 1
                var created = await _container.CreateItemAsync(new Counter
                {
                    id = counterId,
                    category = partition, // containerens partition key = /category
                    value = 1
                }, new PartitionKey(partition), cancellationToken: ct);

                return created.Resource.value;
            }
        }

        private class Counter
        {
            public string id { get; set; } = default!;
            public string category { get; set; } = default!; // partition key-egenskab
            public int value { get; set; }
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
            await Task.CompletedTask;
        }
    }
}

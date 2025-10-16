using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IbasSupport.Web.Models;
using Microsoft.Azure.Cosmos;
using System.Security.Cryptography;

namespace IbasSupport.Web.Services
{
    public interface ISupportRepository
    {
        Task<SupportMessage> CreateAsync(SupportMessage item, CancellationToken ct = default);
        Task<IReadOnlyList<SupportMessage>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<SupportMessage>> GetByCategoryAsync(string category, CancellationToken ct = default);
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

            // Cosmos id (string) – GUID hvis mangler
            if (string.IsNullOrWhiteSpace(item.id))
                item.id = Guid.NewGuid().ToString("N");

            // requestId – tilfældigt 6-cifret tal hvis mangler
            if (!item.requestId.HasValue || item.requestId <= 0)
                item.requestId = CreateRandom6Digit();

            // userId – tilfældigt positivt int hvis mangler
            if (item.user is not null && (!item.user.userId.HasValue || item.user.userId <= 0))
                item.user.userId = CreateRandomPositiveInt();

            // dealerId – tilfældigt positivt int hvis mangler
            if (item.dealer is not null && (!item.dealer.dealerId.HasValue || item.dealer.dealerId <= 0))
                item.dealer.dealerId = CreateRandomPositiveInt();

            var response = await _container.CreateItemAsync(item, new PartitionKey(item.category), cancellationToken: ct);
            return response.Resource;
        }

        private static int CreateRandom6Digit() => RandomNumber(100000, 999999);

        private static int CreateRandomPositiveInt() => RandomNumber(1, int.MaxValue);

        private static int RandomNumber(int minInclusive, int maxInclusive)
        {
            if (minInclusive > maxInclusive)
                throw new ArgumentOutOfRangeException(nameof(minInclusive), "minInclusive must be <= maxInclusive");

            // Fyld 4 bytes med kryptografisk tilfældige tal (ingen stackalloc/pointers)
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            uint raw = BitConverter.ToUInt32(bytes, 0);

            // Map til [minInclusive, maxInclusive] inkl.
            long range = (long)maxInclusive - minInclusive + 1;
            long scaled = (long)(raw % range);
            return (int)(minInclusive + scaled);
        }

        public async Task<IReadOnlyList<SupportMessage>> GetAllAsync(CancellationToken ct = default)
        {
            var results = new List<SupportMessage>();

            // (Valgfrit filter) Hent kun "rigtige" henvendelser – filtrer evt. gamle meta-docs fra.
            var query = new QueryDefinition(@"
                SELECT * FROM c
                WHERE IS_DEFINED(c.description)
                  AND IS_DEFINED(c.user)
            ");

            var it = _container.GetItemQueryIterator<SupportMessage>(
                query,
                requestOptions: new QueryRequestOptions { MaxItemCount = 100 }
            );

            while (it.HasMoreResults)
            {
                var page = await it.ReadNextAsync(ct);
                results.AddRange(page);
            }

            results.Sort((a, b) => DateTime.Compare(b.dateTime, a.dateTime)); // nyeste først
            return results;
        }

        public async Task<IReadOnlyList<SupportMessage>> GetByCategoryAsync(string category, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(category)) return Array.Empty<SupportMessage>();

            var results = new List<SupportMessage>();

            // (Valgfrit filter) Kun denne partition + ekskl. evt. meta-docs
            var query = new QueryDefinition(@"
                SELECT * FROM c
                WHERE c.category = @cat
                  AND IS_DEFINED(c.description)
                  AND IS_DEFINED(c.user)
            ").WithParameter("@cat", category);

            var it = _container.GetItemQueryIterator<SupportMessage>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(category),
                    MaxItemCount = 100
                });

            while (it.HasMoreResults)
            {
                var page = await it.ReadNextAsync(ct);
                results.AddRange(page);
            }

            results.Sort((a, b) => DateTime.Compare(b.dateTime, a.dateTime)); // nyeste først
            return results;
        }

        public ValueTask DisposeAsync()
        {
            _client?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

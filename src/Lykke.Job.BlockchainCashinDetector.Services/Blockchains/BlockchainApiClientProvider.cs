using System;
using Autofac.Features.Indexed;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Service.BlockchainApi.Client;

namespace Lykke.Job.BlockchainCashinDetector.Services.Blockchains
{
    [UsedImplicitly]
    public class BlockchainApiClientProvider : IBlockchainApiClientProvider
    {
        private readonly IIndex<string, IBlockchainApiClient> _clients;

        public BlockchainApiClientProvider(IIndex<string, IBlockchainApiClient> clients)
        {
            _clients = clients;
        }

        public IBlockchainApiClient Get(string blockchainType)
        {
            if (!_clients.TryGetValue(blockchainType, out var client))
            {
                throw new InvalidOperationException($"Blockchain API client [{blockchainType}] is not found");
            }

            return client;
        }
    }
}
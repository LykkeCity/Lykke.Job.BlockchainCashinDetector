using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Projections
{
    // TODO: Handle transaction failure

    public class ClientOperationsProjection
    {
        private readonly ICashinRepository _cashinRepository;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly ICashOperationsRepositoryClient _clientOperationsRepositoryClient;
        private readonly IChaosKitty _chaosKitty;

        public ClientOperationsProjection(
            ICashinRepository cashinRepository,
            IBlockchainWalletsClient walletsClient,
            ICashOperationsRepositoryClient clientOperationsRepositoryClient,
            IChaosKitty chaosKitty)
        {
            _cashinRepository = cashinRepository;
            _walletsClient = walletsClient;
            _clientOperationsRepositoryClient = clientOperationsRepositoryClient;
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public async Task Handle(CashinEnrolledToMatchingEngineEvent evt)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);
                
            await _clientOperationsRepositoryClient.RegisterAsync(new CashInOutOperation(
                id: evt.OperationId.ToString(),
                amount: (double) evt.OperationAmount,
                clientId: evt.ClientId.ToString(),

                transactionId: aggregate.OperationId.ToString(),
                dateTime: aggregate.CreationMoment,
                assetId: aggregate.AssetId,
                addressFrom: aggregate.DepositWalletAddress,
                addressTo: aggregate.HotWalletAddress,

                type: CashOperationType.ForwardCashIn,
                state: TransactionStates.InProcessOnchain,
                isSettled: false,
                blockChainHash: "",

                // These fields are not used

                feeType: FeeType.Unknown,
                feeSize: 0,
                isRefund: false,
                multisig: "",
                isHidden: false
            ));

            _chaosKitty.Meow(evt.OperationId);
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt)
        {
            var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashin operation
                return;
            }

            var clientId = await GetClientIdAsync(aggregate);

            await _clientOperationsRepositoryClient.UpdateBlockchainHashAsync
            (
                clientId.ToString(),
                aggregate.OperationId.ToString(),
                evt.TransactionHash
            );

            _chaosKitty.Meow(evt.OperationId);
        }

        [UsedImplicitly]
        public async Task Handle(EnrolledBalanceSetEvent evt)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate.IsDustCashin)
            {
                var clientId = await GetClientIdAsync(aggregate);
                    
                await _clientOperationsRepositoryClient.UpdateBlockchainHashAsync
                (
                    clientId.ToString(),
                    aggregate.OperationId.ToString(),
                    "0x"
                );
            }

            _chaosKitty.Meow(evt.OperationId);
        }

        private async Task<Guid> GetClientIdAsync(CashinAggregate aggregate)
        {
            // Obtains clientId directly from the wallets, but not aggregate,
            // to make projection independent on the aggregate state, since
            // clientId in aggregate is initially not filled up.

            // TODO: Add client cache for the walletsClient

            var clientId = await _walletsClient.TryGetClientIdAsync
            (
                aggregate.BlockchainType,
                aggregate.BlockchainAssetId,
                aggregate.DepositWalletAddress
            );

            if (clientId == null)
            {
                throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
            }

            return clientId.Value;
        }
    }
}

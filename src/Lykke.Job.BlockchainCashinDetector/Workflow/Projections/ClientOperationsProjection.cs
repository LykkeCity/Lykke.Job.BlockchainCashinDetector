using System;
using System.Threading.Tasks;
using Common.Log;
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
        private readonly ILog _log;
        private readonly ICashinRepository _cashinRepository;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly ICashOperationsRepositoryClient _clientOperationsRepositoryClient;
        private readonly IChaosKitty _chaosKitty;

        public ClientOperationsProjection(
            ILog log,
            ICashinRepository cashinRepository,
            IBlockchainWalletsClient walletsClient,
            ICashOperationsRepositoryClient clientOperationsRepositoryClient,
            IChaosKitty chaosKitty)
        {
            _log = log.CreateComponentScope(nameof(ClientOperationsProjection));
            _cashinRepository = cashinRepository;
            _walletsClient = walletsClient;
            _clientOperationsRepositoryClient = clientOperationsRepositoryClient;
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public async Task Handle(CashinStartedEvent evt)
        {
            _log.WriteInfo(nameof(CashinStartedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                // Obtains clientId directly from the wallets, but not aggregate,
                // to make projection independent on the aggregate state, since
                // clientId in aggregate is initially not filled up.

                // TODO: Add client cache for the walletsClient

                var clientId = await _walletsClient.TryGetClientIdAsync(
                    aggregate.BlockchainType,
                    aggregate.BlockchainAssetId,
                    aggregate.DepositWalletAddress);

                if (clientId == null)
                {
                    throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
                }

                await _clientOperationsRepositoryClient.RegisterAsync(new CashInOutOperation(
                    id: aggregate.OperationId.ToString(),
                    transactionId: aggregate.OperationId.ToString(),
                    dateTime: aggregate.CreationMoment,
                    amount: (double) aggregate.OperationAmount,
                    assetId: aggregate.AssetId,
                    clientId: clientId.ToString(),
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
            catch (Exception ex)
            {
                _log.WriteError(nameof(CashinStartedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt)
        {
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, "");

            try
            {
                await UpdateBlockchainHashAsync(evt.OperationId, evt.TransactionHash);

                _chaosKitty.Meow(evt.OperationId);
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        public async Task Handle(EnrolledBalanceIncreasedEvent evt)
        {
            _log.WriteInfo(nameof(EnrolledBalanceIncreasedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.State == CashinState.OperationIsFinished)
                {
                    await UpdateBlockchainHashAsync(evt.OperationId, "n/a");
                }

                _chaosKitty.Meow(evt.OperationId);
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(EnrolledBalanceIncreasedEvent), evt, ex);
                throw;
            }
        }

        private async Task UpdateBlockchainHashAsync(Guid operationId, string transactionHash)
        {
            var aggregate = await _cashinRepository.TryGetAsync(operationId);

            if (aggregate == null)
            {
                // This is not a cashin operation
                return;
            }

            // Obtains clientId directly from the wallets, but not aggregate,
            // to make projection independent on the aggregate state, since
            // clientId in aggregate is initially not filled up.

            // TODO: Add client cache for the walletsClient

            var clientId = await _walletsClient.TryGetClientIdAsync(
                aggregate.BlockchainType,
                aggregate.BlockchainAssetId,
                aggregate.DepositWalletAddress);

            if (clientId == null)
            {
                throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
            }

            await _clientOperationsRepositoryClient.UpdateBlockchainHashAsync
            (
                clientId.ToString(),
                aggregate.OperationId.ToString(),
                transactionHash
            );
        }
    }
}

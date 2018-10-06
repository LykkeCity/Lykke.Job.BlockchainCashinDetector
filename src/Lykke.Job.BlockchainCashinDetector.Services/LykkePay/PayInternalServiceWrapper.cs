using Lykke.Job.BlockchainCashinDetector.Core.Services.LykkePay;
using Lykke.Service.PayInternal.Client;
using Lykke.Service.PayInternal.Client.Models.Validation;
using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Services.LykkePay
{
    public class PayInternalServiceWrapper : IPayInternalServiceWrapper
    {
        private readonly IPayInternalClient _client;

        public PayInternalServiceWrapper(Lykke.Service.PayInternal.Client.IPayInternalClient client)
        {
            _client = client;
        }

        public async Task<bool> ValidateDepoistTransferAsync(string integrationLayerId, string transferAddress, decimal transferAmount)
        {
            var request = new ValidateDepositTransferRequest()
            {
                Blockchain = integrationLayerId,
                TransferAmount = transferAmount,
                WalletAddress = transferAddress
            };

            var response = await _client.ValidateDepositTransferAsync(request);

            if (response == null)
                throw new Exception("Operation should be repeated");

            return response.IsSuccess;
        }
    }
}

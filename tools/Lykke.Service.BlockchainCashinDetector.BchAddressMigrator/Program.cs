using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Lykke.Job.BlockchainCashinDetector.AzureRepositories;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainCashinDetector.BchAddressMigrator.Address;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using NBitcoin;
using NBitcoin.Altcoins;

namespace Lykke.Service.BlockchainCashinDetector.BchAddressMigrator
{
    class Program
    {
        private const string SettingsUrl = "settingsUrl";
        private const string BlockchainType = "blockchainType";
        private const string BitcoinCashNetwork = "Bitcoin cash network";

        static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Creates in sign facade new entities with bitcoin cash address format"
            };

            var arguments = new Dictionary<string, CommandArgument>
            {
                {SettingsUrl, application.Argument(SettingsUrl, "Cashin detector settings url")},
                {BitcoinCashNetwork, application.Argument(BitcoinCashNetwork, "Bitcoin cash network mainnet/test")},
                {BlockchainType,application.Argument(BlockchainType, "Blockchain type (for bitcoin cash abc/bitcoin cash sv)")}

            };

            application.HelpOption("-? | -h | --help");
            application.OnExecute(async () =>
            {
                try
                {
                    if (arguments.Any(x => string.IsNullOrEmpty(x.Value.Value)))
                    {
                        application.ShowHelp();
                    }
                    else
                    {
                        await Execute(arguments[SettingsUrl].Value,
                            arguments[BlockchainType].Value,
                            arguments[BitcoinCashNetwork].Value);
                    }

                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);

                    return 1;
                }
            });

            application.Execute(args);
        }


        private static async Task Execute(string settingsUrl, string blockchainType, string bitcoinCashNetwork)
        {
            {
                if (!Uri.TryCreate(settingsUrl, UriKind.Absolute, out _))
                {
                    throw new ArgumentException("SettingsUrl should be a valid uri", nameof(settingsUrl));
                }

                BCash.Instance.EnsureRegistered();
                var network = Network.GetNetwork(bitcoinCashNetwork);
                var bcashNetwork = network == Network.Main ? BCash.Instance.Mainnet : BCash.Instance.Regtest;
                var addressValidator = new AddressValidator(network, bcashNetwork);

                var logFactory = LogFactory.Create().AddConsole();

                var appSettings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl, options => { });

                var enrolledBalanceStorage = AzureTableStorage<EnrolledBalanceEntity>.Create(
                    appSettings.Nested(p => p.BlockchainCashinDetectorJob.Db.DataConnString),
                    "EnrolledBalance",
                    logFactory);

                var enrolledBalanceRepository = EnrolledBalanceRepository.Create(
                    appSettings.Nested(p => p.BlockchainCashinDetectorJob.Db.DataConnString), logFactory);


                Console.WriteLine("Retrieving enrolled balances");
                var enrolledBalances = (await enrolledBalanceStorage.GetDataAsync())
                    .Where(p => p.BlockchainType == blockchainType)
                    .ToList();

                var counter = 0;
                foreach (var enrolledBalanceEntity in enrolledBalances)
                {
                    counter++;
                    Console.WriteLine("Processing " +
                                      $"{enrolledBalanceEntity} : {enrolledBalanceEntity.Balance} : {enrolledBalanceEntity.Block} " +
                                      $"--- {counter} of {enrolledBalances.Count}");

                    var addr = addressValidator.GetBitcoinAddress(enrolledBalanceEntity.DepositWalletAddress);

                    if (addr == null)
                    {
                        throw new ArgumentException(
                            $"Unable to recognize address {enrolledBalanceEntity.DepositWalletAddress}", 
                            nameof(enrolledBalanceEntity.DepositWalletAddress));
                    }

                    var bchCashAddr = addr.ScriptPubKey.GetDestinationAddress(bcashNetwork).ToString();

                    await enrolledBalanceRepository.SetBalanceAsync(new DepositWalletKey(enrolledBalanceEntity.BlockchainAssetId,
                        enrolledBalanceEntity.BlockchainType,
                        bchCashAddr),
                        enrolledBalanceEntity.Balance,
                        enrolledBalanceEntity.Block);

                    await enrolledBalanceRepository.ResetBalanceAsync(new DepositWalletKey(
                            enrolledBalanceEntity.BlockchainAssetId,
                            enrolledBalanceEntity.BlockchainType,
                            enrolledBalanceEntity.DepositWalletAddress), enrolledBalanceEntity.Block);
                }

                Console.WriteLine("All done");
            }
        }
    }
}

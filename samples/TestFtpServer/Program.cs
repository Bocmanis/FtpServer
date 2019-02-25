using FubarDev.FtpServer;
using FubarDev.FtpServer.AccountManagement;
using FubarDev.FtpServer.FileSystem.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestFtpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServiceCollection services = GetServices();
                var c = RunService(services).Result;

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Console.ReadLine();
            }
        }

        private static async Task<int> RunService(ServiceCollection services)
        {
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
                NLog.LogManager.LoadConfiguration("NLog.config");

                try
                {
                    // Start the FTP server
                    var ftpServerHost = serviceProvider.GetRequiredService<IFtpServerHost>();
                    await ftpServerHost.StartAsync(CancellationToken.None).ConfigureAwait(false);

                    Console.WriteLine("Press ENTER/RETURN to close the test application.");
                    Console.ReadLine();

                    // Stop the FTP server
                    await ftpServerHost.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                return 0;
            }
        }

        private static ServiceCollection GetServices()
        {
            var services = new ServiceCollection();
            services
                .AddLogging(cfg => cfg.SetMinimumLevel(LogLevel.Trace))
                .AddOptions()
                .Configure<AuthTlsOptions>(
                opt =>
                {
                    string certThumbPrint = "‎‎99719d489ddfef540f4da2567a3475a90fa77b61";
                    certThumbPrint = certThumbPrint.Replace("\u200e", string.Empty).Replace("\u200f", string.Empty).Replace(" ", string.Empty);
                    X509Store certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    // Try to open the store.

                    certStore.Open(OpenFlags.ReadOnly);
                    // Find the certificate that matches the thumbprint.
                    X509Certificate2Collection certCollection = certStore.Certificates.Find(
                        X509FindType.FindByThumbprint, certThumbPrint, false);
                    certStore.Close();
                    opt.ServerCertificate = certCollection[0];
                })
                .Configure<FtpConnectionOptions>(
                opt =>
                {
                    opt.DefaultEncoding = Encoding.ASCII;
                })
                .Configure<FtpServerOptions>(
                opt =>
                {
                    opt.ServerAddress = "127.0.0.1";
                    opt.Port = 21;
                    opt.MaxActiveConnectionCount = 3;
                })
                .Configure<InMemoryFileSystemOptions>(
                opt =>
                {
                    opt.KeepAuthenticatedUserFileSystem = true;
                    opt.KeepAnonymousFileSystem = false;
                })
                .AddFtpServer(sb => Configure(sb).UseInMemoryFileSystem());
            return services;
        }

        private static IFtpServerBuilder Configure(IFtpServerBuilder builder)
        {
            builder.Services.AddSingleton<IMembershipProvider, CustomMembershipProvider>();
            return builder;
        }
    }
}

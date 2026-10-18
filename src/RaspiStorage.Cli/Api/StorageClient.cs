using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using RaspiStorage.Cli.Config;

namespace RaspiStorage.Cli.Api;

public class StorageClient
{
    public static HttpClient CreateHttpClient(CliConfig config)
    {
        var clientCert = X509Certificate2.CreateFromPemFile(config.ClientCert, config.ClientKey);
        var ca = X509CertificateLoader.LoadCertificateFromFile(config.CaCert);

        var chainPolicy = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            RevocationMode = X509RevocationMode.NoCheck
        };
        chainPolicy.CustomTrustStore.Add(ca);

        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                ClientCertificates = new X509CertificateCollection { clientCert },
                CertificateChainPolicy = chainPolicy,
            },
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(config.ServerUrl),
        };

        return httpClient;
    }
}


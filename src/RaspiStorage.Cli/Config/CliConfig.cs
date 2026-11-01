using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Net.Security;

namespace RaspiStorage.Cli.Config;

public record CliConfig(string ServerUrl, string ClientCert, string ClientKey, string CaCert)
{

    public static CliConfig Load()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "raspi-storage", "config.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Config not found: {path}");
        }

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<CliConfig>(json, JsonSerializerOptions.Web) 
                    ?? throw new InvalidDataException($"Config is empty: {path}") ;

        if (string.IsNullOrWhiteSpace(config.ServerUrl))
            throw new InvalidDataException($"'serverUrl' is missing in {path}");
        if (string.IsNullOrWhiteSpace(config.ClientCert))
            throw new InvalidDataException($"'clientCert' is missing in {path}");
        if (string.IsNullOrWhiteSpace(config.ClientKey))
            throw new InvalidDataException($"'clientKey' is missing in {path}");
        if (string.IsNullOrWhiteSpace(config.CaCert))
            throw new InvalidDataException($"'caCert' is missing in {path}");

        return config;
    }

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
            Timeout = Timeout.InfiniteTimeSpan,
        };

        return httpClient;
    }
}
using System.Text.Json;

namespace RaspiStorage.Cli.Config;

public sealed record CliConfig(string ServerUrl, string ClientCert, string ClientKey, string CaCert)
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
}
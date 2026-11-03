using System.Security.Authentication;
using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Config;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

AnsiConsole.Write(new FigletText("Raspi Storage").Color(Color.Grey));

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    var http = CliConfig.CreateHttpClient();
    var filesApi = new FilesApi(http);

    var id = Guid.Parse("c39ee7ea-57bf-4e30-8cfe-a1912471e583");

    await filesApi.DeleteAsync(id, cts.Token);
} catch (ConfigException ex)
{
    Console.Error.WriteLine(ex.Message);
} 
catch (ApiException ex)
{
    Console.Error.WriteLine(ex.Code switch
    {
        "cert_missing"        => "No client certificate configured. Check your certificate path.",
        "cert_invalid"        => "Your client certificate is invalid or expired.",
        "backend_unavailable" => "The storage service is unavailable (restarting?). Try again shortly.",
        "backend_timeout"     => "The server took too long to respond.",
        "payload_too_large"   => "The file is larger than the 1 GB upload limit.",
        "quota_exceeded" or "disk_full" => $"Not enough storage space: {ex.Message}",
        _ => $"Error: {ex.Message}"
    });
} 
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Console.Error.WriteLine("Cancelled.");
}
catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.SecureConnectionError
                                        || ex.InnerException is AuthenticationException)
{
    Console.Error.WriteLine("Secure connection failed: the server certificate isn't trusted, " +
                            "or the server rejected the TLS handshake.");
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Could not reach the server: {ex.Message}");
}
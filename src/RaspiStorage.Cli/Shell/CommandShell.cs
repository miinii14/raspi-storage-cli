using System.Net;
using System.Reflection;
using System.Security.Authentication;
using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell;

public class CommandShell
{
    private readonly HttpClient _http;
    private readonly Context _context;
    private readonly FilesApi _filesApi;
    private readonly FoldersApi _foldersApi;
    private CancellationTokenSource? _currentCts;
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    public CommandShell(HttpClient http, Context context, FilesApi filesApi, FoldersApi foldersApi)
    {
        _http = http;
        _context = context;
        _filesApi = filesApi;
        _foldersApi = foldersApi;

        _commands["exit"] = new ExitCommand();
        _commands["help"] = new HelpCommand(_commands);
        _commands["ls"] =   new LsCommand(_filesApi, _foldersApi);

        Console.CancelKeyPress += (_, e) => { e.Cancel = true; _currentCts?.Cancel(); };
    }

    private static readonly IAnsiConsole Stderr = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(Console.Error),
    });

    private static void PrintError(string title, string message, IEnumerable<string>? details = null)
    {
        Stderr.MarkupLine($"[red bold]✗ {Markup.Escape(title)}:[/] {Markup.Escape(message)}");

        foreach (var detail in details ?? [])
            Stderr.MarkupLine($"    [grey]•[/] {Markup.Escape(detail)}");
    }

    private void PrintBanner()
    {
        var info = new Grid()
            .AddColumn(new GridColumn().PadRight(3))
            .AddColumn()
            .AddRow("[grey]server[/]", Markup.Escape(_http.BaseAddress!.Host))
            .AddRow("[grey]version[/]", Assembly.GetEntryAssembly()!.GetName().Version!.ToString(3));

        var content = new Rows(
            info,
            new Text(""),
            new Markup("Type [bold]help[/] to get started"));

        var panel = new Panel(content)
            .Header("[steelblue1 bold] Raspi Storage [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Padding(2, 0, 2, 0);

        AnsiConsole.Write(panel);
    }

    public async Task RunAsync()
    {
        PrintBanner();

        while (!_context.Exit)
        {
            AnsiConsole.Markup($"[steelblue1]rs[/][grey]:[/][bold]{Markup.Escape(_context.Path)}[/] [grey]❯[/] ");
            var line = Console.ReadLine();
            if (line is null)
                break;

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                continue;

            if (!_commands.TryGetValue(tokens[0], out var cmd))
            {
                PrintError("Unknown command", $"'{tokens[0]}'. Type 'help' to see available commands.");
                continue;
            }

            using var cts = new CancellationTokenSource();
            _currentCts = cts;

            try
            {
                await cmd.ExecuteAsync(tokens[1..], _context, cts.Token);
            }
            catch (ApiException ex)
            {
                var title = $"{ex.Status} {ex.Problem?.Title ?? ((HttpStatusCode)ex.Status).ToString()}";

                var message = ex.Code switch
                {
                    "cert_missing"        => "No client certificate configured. Check your certificate path.",
                    "cert_invalid"        => "Your client certificate is invalid or expired.",
                    "backend_unavailable" => "The storage service is unavailable (restarting?). Try again shortly.",
                    "backend_timeout"     => "The server took too long to respond.",
                    "payload_too_large"   => "The file is larger than the 1 GB upload limit.",
                    "quota_exceeded" or "disk_full" => $"Not enough storage space: {ex.Message}",
                    _ => ex.Message
                };

                var details = ex.Problem?.Errors?
                    .SelectMany(field => field.Value.Select(error => $"{field.Key}: {error}"));

                PrintError(title, message, details);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                Stderr.MarkupLine("[yellow]Cancelled.[/]");
            }
            catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.SecureConnectionError
                                                    || ex.InnerException is AuthenticationException)
            {
                PrintError("Secure connection failed",
                    "The server certificate isn't trusted, or the server rejected the TLS handshake.");
            }
            catch (HttpRequestException ex)
            {
                PrintError("Could not reach the server", ex.Message);
            }
            catch (Exception ex)
            {
                PrintError("Unexpected error", ex.Message);
            }
            finally
            {
                _currentCts = null;
            }
        }
    }
}

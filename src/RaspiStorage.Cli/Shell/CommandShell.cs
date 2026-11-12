using System.Net;
using System.Reflection;
using System.Security.Authentication;
using PrettyPrompt;
using PrettyPrompt.Highlighting;
using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;
using RaspiStorage.Cli.Shell.Commands;
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
    private readonly PromptConfiguration _promptConfig = new();
    private readonly Prompt _prompt;
    public CommandShell(HttpClient http, Context context, FilesApi filesApi, FoldersApi foldersApi)
    {
        _http = http;
        _filesApi = filesApi;
        _foldersApi = foldersApi;
        _context = context;
        _commands["ls"]       = new LsCommand(_filesApi, _foldersApi);
        _commands["exit"]     = new ExitCommand();
        _commands["cd"]       = new CdCommand(_foldersApi);
        _commands["help"]     = new HelpCommand(_commands);
        _commands["rm"]       = new RmCommand(_foldersApi, _filesApi);
        _commands["mkdir"]    = new MkDirCommand(_foldersApi);
        _commands["rename"]   = new RenameCommand(_foldersApi, _filesApi);
        _commands["mv"]       = new MvCommand(_foldersApi, _filesApi);
        _commands["upload"]   = new UploadCommand(_foldersApi, _filesApi);
        _commands["download"] = new DownloadCommand(_foldersApi, _filesApi);
        _commands["clear"]    = new ClearCommand();
        _commands["tree"]     = new TreeCommand(_foldersApi, _filesApi);

        Console.CancelKeyPress += (_, e) => { e.Cancel = true; _currentCts?.Cancel(); };
        _prompt = new Prompt(persistentHistoryFilepath: null, configuration: _promptConfig);
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
    
    private static FormattedString BuildPrompt(string path)
    {
        var text = $"rs:{path} ❯ ";
        return new FormattedString(text,
            new FormatSpan(0, 2, AnsiColor.Rgb(95, 175, 255)),
            new FormatSpan(2, 1, AnsiColor.BrightBlack),
            new FormatSpan(3, text.Length - 5, new ConsoleFormat(Bold: true)),
            new FormatSpan(text.Length - 2, 1, AnsiColor.BrightBlack));
    }

    private async Task<string?> ReadLineAsync()
    {
        if (Console.IsInputRedirected)
            return Console.ReadLine();

        _promptConfig.Prompt = BuildPrompt(_context.Path);
        var response = await _prompt.ReadLineAsync();
        return response.IsSuccess ? response.Text : "";
    }

    public async Task RunAsync()
    {
        PrintBanner();

        if (!Console.IsOutputRedirected)
            Console.Write("\e[?2004l");

        while (!_context.Exit)
        {
            var line = await ReadLineAsync();
            if (line is null)
                break;

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                continue;

            if (!_commands.TryGetValue(tokens[0], out var cmd))
            {
                Output.Error("Unknown command", $"'{tokens[0]}'. Type 'help' to see available commands.");
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

                Output.Error(title, message, details);
            }
            catch (CliException ex)
            {
                Output.Error(ex.Title, ex.Message);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                Output.Stderr.MarkupLine("[yellow]Cancelled.[/]");
            }
            catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.SecureConnectionError
                                                    || ex.InnerException is AuthenticationException)
            {
                Output.Error("Secure connection failed",
                    "The server certificate isn't trusted, or the server rejected the TLS handshake.");
            }
            catch (HttpRequestException ex)
            {
                Output.Error("Could not reach the server", ex.Message);
            }
            catch (Exception ex)
            {
                Output.Error("Unexpected error", ex.Message);
            }
            finally
            {
                _currentCts = null;
            }
        }
    }
}

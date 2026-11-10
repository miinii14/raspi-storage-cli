using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class UploadCommand : ICommand
{
    public string Description => "Upload local file to current or given folder";

    private readonly FoldersApi _foldersApi;
    private readonly FilesApi _filesApi;

    public UploadCommand(FoldersApi foldersApi, FilesApi filesApi)
    {
        _foldersApi = foldersApi;
        _filesApi = filesApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length is < 1 or > 2)
            throw new CliException("upload", "Usage: upload <local> [remote-dir]");

        var localPath = PathResolver.ResolveLocal(args[0]);

        if (!File.Exists(localPath))
            throw new CliException("upload", $"Local file '{localPath}' not found.");

        var folderId = args.Length == 2
            ? (await PathResolver.ResolveAsync(args[1], ctx, ct, _foldersApi, "upload")).LastOrDefault()?.Id
            : ctx.CurrentFolder?.Id;

        await using var file = File.OpenRead(localPath);
        var name = Path.GetFileName(localPath);

        var uploaded = await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new DownloadedColumn(),
                new TransferSpeedColumn(),
                new RemainingTimeColumn())
            .StartAsync(async progress =>
            {
                var task = progress.AddTask(Markup.Escape(name), maxValue: file.Length);
                var stream = new ProgressStream(file, bytes => task.Increment(bytes));
                return await _filesApi.UploadAsync(stream, name, folderId, ct);
            });

        AnsiConsole.MarkupLine($"[green]✔[/] Uploaded [bold]{Markup.Escape(uploaded.Name)}[/] [grey]({Format.Size(uploaded.SizeBytes)})[/]");
    }
}

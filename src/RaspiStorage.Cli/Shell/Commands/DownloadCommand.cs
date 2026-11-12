using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class DownloadCommand : ICommand
{
    public string Usage => "download <remote-file> [local]";
    public string Description => "Download file to current or given local path";

    private readonly FoldersApi _foldersApi;
    private readonly FilesApi _filesApi;

    public DownloadCommand(FoldersApi foldersApi, FilesApi filesApi)
    {
        _foldersApi = foldersApi;
        _filesApi = filesApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length is < 1 or > 2)
            throw new CliException("download", $"Usage: {Usage}");

        var (parentPath, name) = PathResolver.Split(args[0], "download");
        var parentId = (await PathResolver.ResolveAsync(parentPath, ctx, ct, _foldersApi, "download")).LastOrDefault()?.Id;

        var file = (await _filesApi.GetByFolderIdAsync(parentId, ct)).FirstOrDefault(f => f.Name == name)
            ?? throw new CliException("download", $"File '{name}' not found.");

        var target = args.Length == 2
            ? PathResolver.ResolveLocal(args[1])
            : Path.Combine(Environment.CurrentDirectory, file.Name);

        if (Directory.Exists(target))
            target = Path.Combine(target, file.Name);

        if (!Directory.Exists(Path.GetDirectoryName(target)))
            throw new CliException("download", $"Local directory '{Path.GetDirectoryName(target)}' not found.");

        if (File.Exists(target) && !AnsiConsole.Confirm($"Overwrite '{Markup.Escape(target)}'?", defaultValue: false))
            return;

        var partPath = target + ".part";

        try
        {
            await using (var output = File.Create(partPath))
            {
                await AnsiConsole.Progress()
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new DownloadedColumn(),
                        new TransferSpeedColumn(),
                        new RemainingTimeColumn())
                    .StartAsync(async progress =>
                    {
                        var task = progress.AddTask(Markup.Escape(file.Name), maxValue: file.SizeBytes);
                        var stream = new ProgressStream(output, bytes => task.Increment(bytes));
                        await _filesApi.DownloadAsync(file.Id, stream, ct);
                    });
            }

            File.Move(partPath, target, overwrite: true);
        }
        catch
        {
            File.Delete(partPath);
            throw;
        }

        Output.Success($"Saved to [bold]{Markup.Escape(target)}[/] [grey]({Format.Size(file.SizeBytes)})[/]");
    }
}

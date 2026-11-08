using RaspiStorage.Cli.Api;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class LsCommand : ICommand
{
    public string Description => "List files and folders in current folder";
    private readonly FilesApi _filesApi;
    private readonly FoldersApi _foldersApi;

    public LsCommand(FilesApi filesApi, FoldersApi foldersApi)
    {
        _filesApi = filesApi;
        _foldersApi = foldersApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct){
        var files = await _filesApi.GetByFolderIdAsync(ctx.CurrentFolder?.Id, ct);
        var folders = await _foldersApi.GetByParentIdAsync(ctx.CurrentFolder?.Id, ct);

        if (!files.Any() && !folders.Any())
        {
            AnsiConsole.MarkupLine("[grey]Folder is empty[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[grey]Name[/]")
            .AddColumn("[grey]Size[/]")
            .AddColumn("[grey]Modified[/]");

        foreach (var folder in folders.OrderBy(f => f.Name))
            table.AddRow(
                new Markup($"[bold blue]{Markup.Escape(folder.Name)}/[/]"),
                new Markup("[grey]—[/]").RightJustified(),
                new Markup($"[grey]{folder.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}[/]"));

        foreach (var file in files.OrderBy(f => f.Name))
            table.AddRow(
                new Markup(Markup.Escape(file.Name)),
                new Markup(FormatSize(file.SizeBytes)).RightJustified(),
                new Markup($"[grey]{file.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}[/]"));

        AnsiConsole.Write(table);
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{size:0.#} {units[unit]}";
    }
}

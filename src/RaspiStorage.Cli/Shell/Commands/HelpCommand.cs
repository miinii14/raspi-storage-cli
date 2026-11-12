using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class HelpCommand : ICommand
{
    private readonly IReadOnlyDictionary<string, ICommand> _commands;
    public string Usage => "help";
    public string Description => "Show available commands";

    public HelpCommand(IReadOnlyDictionary<string, ICommand> commands)
    {
        _commands = commands;
    }

    public Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().PadRight(4))
            .AddColumn();

        foreach (var command in _commands.Values.OrderBy(c => c.Usage))
            grid.AddRow($"[bold]{Markup.Escape(command.Usage)}[/]", $"[grey]{Markup.Escape(command.Description)}[/]");

        AnsiConsole.Write(grid);
        return Task.CompletedTask;
    }
}
using Spectre.Console;

namespace RaspiStorage.Cli.Shell;

public class HelpCommand : ICommand
{
    private readonly IReadOnlyDictionary<string, ICommand> _commands;
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

        foreach (var (name, command) in _commands.OrderBy(c => c.Key))
            grid.AddRow($"[bold]{name}[/]", Markup.Escape(command.Description));

        AnsiConsole.Write(grid);
        return Task.CompletedTask;
    }
}
using Spectre.Console;

namespace RaspiStorage.Cli.Shell;

public static class Output
{
    public static readonly IAnsiConsole Stderr = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(Console.Error),
    });

    public static void Error(string title, string message, IEnumerable<string>? details = null)
    {
        Stderr.MarkupLine($"[red bold]✗ {Markup.Escape(title)}:[/] {Markup.Escape(message)}");

        foreach (var detail in details ?? [])
            Stderr.MarkupLine($"    [grey]•[/] {Markup.Escape(detail)}");
    }

    public static void Success(string markup) => AnsiConsole.MarkupLine($"[green]✔[/] {markup}");
}

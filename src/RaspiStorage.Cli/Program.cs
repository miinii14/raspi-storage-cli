using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Config;
using Spectre.Console;

AnsiConsole.Write(new FigletText("Raspi Storage").Color(Color.Grey));
AnsiConsole.MarkupLine("[bold grey]type [white]help[/] for commands[/]");

var clientConfig = CliConfig.Load();
var client = StorageClient.CreateHttpClient(clientConfig);

var json = await client.GetStringAsync("api/folders");
AnsiConsole.WriteLine(json);

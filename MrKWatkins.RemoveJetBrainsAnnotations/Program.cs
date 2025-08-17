using MrKWatkins.RemoveJetBrainsAnnotations;
using Spectre.Console.Cli;

var app = new CommandApp();
app.SetDefaultCommand<RemoveCommand>();
return app.Run(args);
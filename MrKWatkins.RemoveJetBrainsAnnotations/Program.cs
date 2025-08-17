using MrKWatkins.RemoveJetBrainsAnnotations;
using Spectre.Console.Cli;

var app = new CommandApp();
app.SetDefaultCommand<RemoveCommand>();
app.Configure(config => config.SetExceptionHandler((thrown, _) =>
{
    IReadOnlyList<Exception> exceptions = thrown is AggregateException aggregateException ? aggregateException.InnerExceptions : new[] { thrown };
    foreach (var exception in exceptions)
    {
        Console.Error.WriteLine($"An error occurred: {exception.Message}");
    }
    return -1;
}));

return app.Run(args);
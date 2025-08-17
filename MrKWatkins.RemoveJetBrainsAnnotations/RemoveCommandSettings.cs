using Spectre.Console.Cli;

namespace MrKWatkins.RemoveJetBrainsAnnotations;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class RemoveCommandSettings : CommandSettings
{
    [CommandArgument(0, "[Assemblies]")]
    public string Assemblies { get; set; } = null!;
}
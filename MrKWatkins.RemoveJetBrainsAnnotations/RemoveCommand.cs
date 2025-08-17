using Microsoft.Extensions.FileSystemGlobbing;
using Mono.Cecil;
using Mono.Collections.Generic;
using Spectre.Console.Cli;

namespace MrKWatkins.RemoveJetBrainsAnnotations;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class RemoveCommand : Command<RemoveCommandSettings>
{
    private const string Namespace = "JetBrains.Annotations";

    public override int Execute(CommandContext context, RemoveCommandSettings settings)
    {
        var matcher = new Matcher();
        matcher.AddInclude(settings.Assemblies);

        var files = matcher.GetResultsInFullPath(Environment.CurrentDirectory).ToList();

        if (files.Count == 0)
        {
            throw new InvalidOperationException($"Pattern {settings.Assemblies} did not match any files in directory {Environment.CurrentDirectory}.");
        }

        Parallel.ForEach(files, RemoveAttributes);

        return 0;
    }

    private static void RemoveAttributes(string path)
    {
        var tempOutput = path + ".temp";
        try
        {
            var assembly = AssemblyDefinition.ReadAssembly(path);

            RemoveAttributes(assembly);

            assembly.Write(tempOutput);

            File.Move(tempOutput, path, true);

            Console.WriteLine($"Assembly {path} processed successfully.");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Exception processing {path}: {exception.Message}", exception);
        }
        finally
        {
            File.Delete(tempOutput);
        }
    }

    private static void RemoveAttributes(AssemblyDefinition assembly)
    {
        foreach (var module in assembly.Modules)
        {
            // Remove JetBrains.Annotations reference
            foreach (var reference in module.AssemblyReferences.Where(r => r.Name == Namespace).ToList())
            {
                module.AssemblyReferences.Remove(reference);
            }

            // Remove JetBrains.Annotations attributes from all types and members.
            foreach (var type in assembly.Modules.SelectMany(m => m.Types))
            {
                RemoveAttributes(type.CustomAttributes);

                foreach (var method in type.Methods)
                {
                    RemoveAttributes(method.CustomAttributes);
                    foreach (var parameter in method.Parameters)
                    {
                        RemoveAttributes(parameter.CustomAttributes);
                    }
                }

                foreach (var field in type.Fields)
                {
                    RemoveAttributes(field.CustomAttributes);
                }

                foreach (var property in type.Properties)
                {
                    RemoveAttributes(property.CustomAttributes);
                }

                foreach (var evt in type.Events)
                {
                    RemoveAttributes(evt.CustomAttributes);
                }
            }
        }
    }

    private static void RemoveAttributes(Collection<CustomAttribute> attributes)
    {
        for (var f = attributes.Count - 1; f >= 0; f--)
        {
            var attribute = attributes[f];
            if (attribute.AttributeType.Namespace.StartsWith(Namespace, StringComparison.Ordinal))
            {
                attributes.RemoveAt(f);
            }
        }
    }
}
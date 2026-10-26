using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MrKWatkins.RemoveJetBrainsAnnotations.Tests;

public sealed class RemoveCommandTests
{
    private string directory = null!;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "Remove annotations " + Guid.NewGuid());
        Directory.CreateDirectory(directory);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(directory, true);

    [Test]
    public async Task Execute_RemovesAnnotationsAndPreservesOtherMetadata()
    {
        var path = CreateAssembly("Annotated.dll", true);
        var result = await Run("Annotated.dll");

        result.ExitCode.Should().Equal(0);
        result.Output.Should().Contain("processed successfully.");
        using var assembly = AssemblyDefinition.ReadAssembly(path);
        assembly.MainModule.AssemblyReferences.Any(reference => reference.Name == "JetBrains.Annotations").Should().BeFalse();
        assembly.MainModule.AssemblyReferences.Any(reference => reference.Name == "Other.Annotations").Should().BeTrue();
        foreach (var provider in Providers(assembly.MainModule.Types.Single(type => type.Name == "Example")))
        {
            provider.CustomAttributes.Select(attribute => attribute.AttributeType.FullName).Should().SequenceEqual("Other.Annotations.KeepAttribute");
        }
        assembly.MainModule.Types.Single(type => type.Name == "Example").Methods.First().Body.Instructions.Last().OpCode.Should().Equal(OpCodes.Ret);
        File.Exists(path + ".temp").Should().BeFalse();
    }

    [Test]
    public async Task Execute_NoAnnotations()
    {
        var path = CreateAssembly("Plain.dll", false);
        string[] originalReferences;
        using (var original = AssemblyDefinition.ReadAssembly(path))
        {
            originalReferences = original.MainModule.AssemblyReferences.Select(reference => reference.FullName).ToArray();
        }
        var result = await Run("Plain.dll");

        result.ExitCode.Should().Equal(0);
        using var assembly = AssemblyDefinition.ReadAssembly(path);
        assembly.MainModule.AssemblyReferences.Select(reference => reference.FullName).Should().SequenceEqual(originalReferences);
        assembly.MainModule.Types.Single(type => type.Name == "Example").Fields.Single().Name.Should().Equal("Value");
    }

    [Test]
    public async Task Execute_MatchesMultipleFilesAndSubdirectories()
    {
        var first = CreateAssembly("First.dll", true);
        var second = CreateAssembly("subfolder/Second.dll", true);
        var untouched = CreateAssembly("Untouched.exe", true);
        var original = await File.ReadAllBytesAsync(untouched);

        var result = await Run("**/*.dll");

        result.ExitCode.Should().Equal(0);
        foreach (var path in new[] { first, second })
        {
            using var assembly = AssemblyDefinition.ReadAssembly(path);
            assembly.MainModule.AssemblyReferences.Any(reference => reference.Name == "JetBrains.Annotations").Should().BeFalse();
        }
        (await File.ReadAllBytesAsync(untouched)).Should().SequenceEqual(original);
    }

    [Test]
    public async Task Execute_IsIdempotent()
    {
        var path = CreateAssembly("Twice.dll", true);
        (await Run("Twice.dll")).ExitCode.Should().Equal(0);
        var once = await File.ReadAllBytesAsync(path);

        (await Run("Twice.dll")).ExitCode.Should().Equal(0);

        (await File.ReadAllBytesAsync(path)).Should().SequenceEqual(once);
    }

    [Test]
    public async Task Execute_NoMatches()
    {
        var result = await Run("missing*.dll");

        result.ExitCode.Should().NotEqual(0);
        result.Error.Should().Contain("did not match any files");
        Directory.GetFiles(directory).Should().BeEmpty();
    }

    [Test]
    public async Task Execute_InvalidAssemblyPreservesOriginalAndCleansTemporaryOutput()
    {
        var path = Path.Combine(directory, "Broken.dll");
        await File.WriteAllTextAsync(path, "Not an assembly.");

        var result = await Run("Broken.dll");

        result.ExitCode.Should().NotEqual(0);
        result.Error.Should().Contain("Exception processing");
        (await File.ReadAllTextAsync(path)).Should().Equal("Not an assembly.");
        File.Exists(path + ".temp").Should().BeFalse();
    }

    [Test]
    public async Task Execute_MissingPattern()
    {
        var result = await Run();

        result.ExitCode.Should().NotEqual(0);
        result.Error.Should().Contain("An error occurred:");
        Directory.GetFiles(directory).Should().BeEmpty();
    }

    [Test]
    public async Task Execute_Help()
    {
        var result = await Run("--help");

        result.ExitCode.Should().Equal(0);
        result.Output.Should().Contain("Assemblies");
    }

    private string CreateAssembly(string name, bool annotated)
    {
        var path = Path.Combine(directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Example", new Version(1, 0)), "Example", ModuleKind.Dll);
        var module = assembly.MainModule;
        var type = new TypeDefinition("Fixture", "Example", TypeAttributes.Public, module.TypeSystem.Object);
        module.Types.Add(type);
        type.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.TypeSystem.String));
        var method = new MethodDefinition("Run", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        method.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, module.TypeSystem.String));
        method.Body.GetILProcessor().Emit(OpCodes.Ret);
        type.Methods.Add(method);
        type.Properties.Add(new PropertyDefinition("Name", PropertyAttributes.None, module.TypeSystem.String));
        type.Events.Add(new EventDefinition("Changed", EventAttributes.None, module.TypeSystem.Object));
        if (annotated)
        {
            var annotations = new AssemblyNameReference("JetBrains.Annotations", new Version(1, 0));
            var other = new AssemblyNameReference("Other.Annotations", new Version(1, 0));
            module.AssemblyReferences.Add(annotations);
            module.AssemblyReferences.Add(other);
            foreach (var provider in Providers(type))
            {
                provider.CustomAttributes.Add(Attribute(module, annotations, "JetBrains.Annotations", "NotNullAttribute"));
                provider.CustomAttributes.Add(Attribute(module, other, "Other.Annotations", "KeepAttribute"));
            }
        }
        assembly.Write(path);
        return path;
    }

    private static CustomAttribute Attribute(ModuleDefinition module, AssemblyNameReference reference, string ns, string name)
    {
        var type = new TypeReference(ns, name, module, reference);
        return new CustomAttribute(new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true });
    }

    private static IEnumerable<ICustomAttributeProvider> Providers(TypeDefinition type)
    {
        yield return type;
        foreach (var method in type.Methods)
        {
            yield return method;
            foreach (var parameter in method.Parameters)
            {
                yield return parameter;
            }
        }
        foreach (var field in type.Fields)
        {
            yield return field;
        }
        foreach (var property in type.Properties)
        {
            yield return property;
        }
        foreach (var evt in type.Events)
        {
            yield return evt;
        }
    }

    private async Task<(int ExitCode, string Output, string Error)> Run(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(typeof(RemoveCommand).Assembly.Location);
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        return (process.ExitCode, await output, await error);
    }
}
# RemoveJetBrainsAnnotations

A .NET 10 command-line tool that rewrites assemblies to remove JetBrains.Annotations metadata.

- Production code lives under `src`; test code belongs under `tests`.
- Build `src/RemoveJetBrainsAnnotations.slnx`. Tests use NUnit on Microsoft Testing Platform and `MrKWatkins.Assertions` (`value.Should().Equal(...)`); do not use NUnit assertion APIs.
- Run tests from `src` with `dotnet test --solution RemoveJetBrainsAnnotations.slnx`.
- Keep package versions in the root `Directory.Packages.props` and tool metadata in its project file. Only the tool is packable.
- Format C# with `dotnet format src/RemoveJetBrainsAnnotations.slnx` and run the relevant tests before finishing.
- Build and Release use `MrKWatkins/build/dotnet/project` with `docs: 'false'`. Releases use trusted NuGet publishing; never add an API-token input.
- Keep usage and local development instructions in `ReadMe.md`; this tool has no separate documentation site.
- Tests should verify real rewritten assemblies, preserved unrelated metadata, CLI results and failure cleanup. Temporary test assemblies must remain outside source control.

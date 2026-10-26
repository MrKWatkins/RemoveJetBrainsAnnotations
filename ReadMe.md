# RemoveJetBrainsAnnotations

[![Build Status](https://github.com/MrKWatkins/RemoveJetBrainsAnnotations/actions/workflows/build.yml/badge.svg)](https://github.com/MrKWatkins/RemoveJetBrainsAnnotations/actions/workflows/build.yml)
[![NuGet Version](https://img.shields.io/nuget/v/MrKWatkins.RemoveJetBrainsAnnotations)](https://www.nuget.org/packages/MrKWatkins.RemoveJetBrainsAnnotations)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MrKWatkins.RemoveJetBrainsAnnotations)](https://www.nuget.org/packages/MrKWatkins.RemoveJetBrainsAnnotations)

> Removes all references to JetBrains.Annotations from an assembly.

## Installation

```
dotnet tool install --global MrKWatkins.RemoveJetBrainsAnnotations
```

## Usage

```
removejetbrainsannotations "SomeFolder/*.dll"
```

The pattern is relative to the current directory. Quote it so the tool receives the glob rather than a list expanded by your shell. Use `"**/*.dll"` to include subdirectories.

Assemblies are rewritten in place. Unrelated attributes are preserved. A failed rewrite returns a non-zero exit code and removes its temporary output file.

## Local development

Use the .NET 10 SDK:

```sh
dotnet build src/RemoveJetBrainsAnnotations.slnx --configuration Release
dotnet test --solution src/RemoveJetBrainsAnnotations.slnx --configuration Release
dotnet format src/RemoveJetBrainsAnnotations.slnx
```

Production code lives under `src`; NUnit tests under `tests` exercise the command-line tool against temporary assemblies.

## Licence

Licensed under GPL v3.0.
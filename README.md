# DnD 5e character builder
A homemade character builder for 5e.

This project now runs with Avalonia on .NET 8, so it is cross-platform (Windows/Linux/macOS).

For end users, you can distribute self-contained builds so they do not need to install .NET.

## Requirements
- .NET 8 SDK (for development)
- Linux users running a framework-dependent build need the .NET 8 runtime installed

## Run (dev)
1. Clone/download this repository
2. Open in VS Code
3. Run:

```bash
dotnet restore
dotnet run
```

## Publish (no .NET install required for users)
Linux self-contained build:

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/linux-x64
```

Windows self-contained build:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

Using Makefile shortcuts:

```bash
make publish-linux
make publish-win
make publish-all
```

Note: you still need .NET SDK on the developer machine to build. End users do not need .NET installed when using self-contained outputs.
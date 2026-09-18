# Development environment

## Verified baseline

The following tools were detected on the initial development machine on 2026-09-18:

| Tool | Verified version |
|---|---|
| .NET SDK | 10.0.401 |
| MSBuild | 18.9.11 |
| Windows SDK/WDK | 10.0.26100.0 |
| Visual Studio | Visual Studio 2022 17.14 and Visual Studio 2026 18.9 |
| Git | 2.53.0.windows.4 |

`global.json` pins the .NET SDK feature band used by the initial user-mode projects. Driver projects must explicitly document their Visual Studio, Windows SDK, WDK, KMDF, and Spectre-mitigation requirements when introduced.

## Repository rules

- Run commands from the repository root.
- Do not store signing certificates, private keys, raw captures, diagnostics, or crash dumps in Git.
- Keep generated output under ignored `bin`, `obj`, `artifacts`, or `TestResults` directories.
- Add a project only when its build and validation contract is understood.
- Driver scaffolding must remain non-binding until protocol and rollback gates pass.

## Build and test

The user-mode foundation builds from the repository root:

```powershell
dotnet restore MagicMouseWindows.slnx
dotnet build MagicMouseWindows.slnx --no-restore
dotnet test MagicMouseWindows.slnx --no-build
```

Any driver build command must identify the Visual Studio and WDK environment it requires and must remain separate from the user-mode build until a reproducible combined path exists.

## Debugging

- Prefer deterministic fixtures over live hardware when debugging parsers and gesture behavior.
- Never feed malformed or fuzzed data into physical system input.
- Record component versions, profile ID, Windows build, and device identity with hardware results.
- Treat base pointer/button interruption as a stop condition requiring rollback.

# Development Setup

This repo targets Unity and the Assembly-CSharp project targets .NET Framework 4.7.1. To build locally and run tests you need the following on Windows:

1. Visual Studio 2019/2022 or the .NET Framework 4.7.1 Developer Pack.
   - Download developer pack: https://aka.ms/msbuild/developerpacks (choose .NET Framework 4.7.1 Developer Pack)
   - Install the developer pack to provide reference assemblies for msbuild/dotnet targeting.

2. .NET SDK (used for some CLI tools): https://dotnet.microsoft.com/en-us/download

3. Unity Editor matching the project version. Open the project via Unity Hub to import packages and generate project files.

Build locally (PowerShell):

```powershell
# From the repository root
dotnet build .\Assembly-CSharp.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

If you see an error about missing .NET Framework reference assemblies, install the Developer Pack and re-run the build.

Running Unity tests
- Use the Unity Test Runner inside the Unity Editor for EditMode and PlayMode tests.
- Optionally add a CI runner that includes Unity; see Unity's GitHub Actions or third-party hosted runners.

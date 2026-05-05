# Rayo

Rayo is a declarative, retained-mode UI library for .NET 10 built on Silk.NET.

## NuGet packages

The repository is prepared to publish these packages together:

- `Rayo`
- `Rayo.Hosting.Abstractions`
- `Rayo.Hosting.Android`
- `Rayo.Hosting.Desktop`
- `Rayo.Rendering`
- `Rayo.Rendering.OpenGL`
- `Rayo.Rendering.SkiaSharp`
- `Rayo.Rendering.Vulkan`
- `Rayo.DevTool.Shared`

These packages have project-to-project dependencies between them, so releases should publish the full set from the same version tag.

## Local packaging

To build the main library locally:

```bash
dotnet restore Rayo/Rayo.csproj --configfile NuGet.Config
dotnet build Rayo/Rayo.csproj --no-restore
```

To create local NuGet packages with a specific version:

```bash
dotnet restore Rayo/Rayo.csproj --configfile NuGet.Config
dotnet pack Rayo.Rendering/Rayo.Rendering.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Rendering.OpenGL/Rayo.Rendering.OpenGL.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Rendering.SkiaSharp/Rayo.Rendering.SkiaSharp.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Rendering.Vulkan/Rayo.Rendering.Vulkan.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Hosting.Abstractions/Rayo.Hosting.Abstractions.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.DevTool.Shared/Rayo.DevTool.Shared.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo/Rayo.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Hosting.Desktop/Rayo.Hosting.Desktop.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Hosting.Android/Rayo.Hosting.Android.csproj -c Release --no-restore -p:PackageVersion=0.1.0
```

## Automated publishing

This repository includes two GitHub Actions workflows:

- `ci.yml`: validates build and package generation on pushes and pull requests.
- `publish-nuget.yml`: publishes the NuGet packages when a tag like `v0.1.0` is pushed.

The publish workflow is designed for NuGet Trusted Publishing with GitHub Actions OIDC.
The workflows also install the Android workload because `Rayo.Hosting.Android` is part of the release set.

### Required GitHub and NuGet setup

1. Create a `release` environment in GitHub if you want protected approvals before publishing.
2. In nuget.org, configure Trusted Publishing for this repository and the `publish-nuget.yml` workflow file.
3. Add the repository secret `NUGET_USER` with your nuget.org username or organization profile name.
4. Push a tag in the form `vX.Y.Z` to publish that version.

Example:

```bash
git tag v0.1.0
git push origin v0.1.0
```

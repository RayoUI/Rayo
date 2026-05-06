# NuGet Generation Guide

This document describes how NuGet package generation and publication works in this repository.

## Published packages

The automated release set is defined in [eng/nuget-pack-projects.txt](/C:/DEV/PROJECTS/RayoUI/Rayo/eng/nuget-pack-projects.txt).

At the time of writing, the release set is:

- `Rayo`
- `Rayo.FluentApiGenerator`
- `Rayo.Hosting.Abstractions`
- `Rayo.Hosting.Android`
- `Rayo.Hosting.Desktop`
- `Rayo.Rendering`
- `Rayo.Rendering.OpenGL`
- `Rayo.Rendering.SkiaSharp`
- `Rayo.Rendering.Vulkan`

These packages are versioned and published together from the same release tag.

## Packaging configuration

NuGet packaging is configured through these files:

- [Directory.Build.props](/C:/DEV/PROJECTS/RayoUI/Rayo/Directory.Build.props): shared package metadata and README packing
- [NuGet.Config](/C:/DEV/PROJECTS/RayoUI/Rayo/NuGet.Config): repository-local package source configuration
- [eng/nuget-pack-projects.txt](/C:/DEV/PROJECTS/RayoUI/Rayo/eng/nuget-pack-projects.txt): explicit release set
- [.github/workflows/ci.yml](/C:/DEV/PROJECTS/RayoUI/Rayo/.github/workflows/ci.yml): validation workflow
- [.github/workflows/publish-nuget.yml](/C:/DEV/PROJECTS/RayoUI/Rayo/.github/workflows/publish-nuget.yml): release workflow

Only projects with `<IsPackable>true</IsPackable>` are intended for publication.

## Local generation

To restore the main project:

```bash
dotnet restore Rayo/Rayo.csproj --configfile NuGet.Config
```

To build the main project:

```bash
dotnet build Rayo/Rayo.csproj --no-restore
```

To generate all release packages manually with a custom version:

```bash
dotnet pack Rayo.Reactivity/Rayo.FluentApiGenerator.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Hosting.Abstractions/Rayo.Hosting.Abstractions.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Rendering/Rayo.Rendering.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Rendering.OpenGL/Rayo.Rendering.OpenGL.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Rendering.SkiaSharp/Rayo.Rendering.SkiaSharp.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Rendering.Vulkan/Rayo.Rendering.Vulkan.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo/Rayo.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Hosting.Desktop/Rayo.Hosting.Desktop.csproj -c Release --no-restore -p:PackageVersion=0.1.0
dotnet pack Rayo.Hosting.Android/Rayo.Hosting.Android.csproj -c Release --no-restore -p:PackageVersion=0.1.0
```

## Automated publication

The repository uses two GitHub Actions workflows:

- `ci.yml`
  - runs on pushes and pull requests
  - restores, builds, and packs the release set
  - uploads generated package artifacts
- `publish-nuget.yml`
  - runs when a Git tag matching `v*` is pushed
  - restores, builds, packs, and publishes the release set to nuget.org

The release workflow uses:

- `windows-latest`
- `dotnet 10.0.x`
- `dotnet workload install android`
- NuGet Trusted Publishing with GitHub Actions OIDC

## How to publish a new version

Prerequisites:

1. The GitHub repository is already configured for NuGet Trusted Publishing.
2. The `NUGET_USER` secret exists in GitHub.
3. The branch content to publish is already merged.

Release steps:

1. Choose the new version using semantic versioning.
2. Create a tag in the format `vX.Y.Z`.
3. Push the tag to GitHub.

Example:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The workflow extracts `0.1.0` from `v0.1.0` and assigns that version to every package in the release set.

## Important rules

- Do not publish only part of the release set unless the dependency graph is intentionally redesigned.
- If you add or remove a public package, update:
  - `eng/nuget-pack-projects.txt`
  - `README.md`
  - this file
- If a package is already published on nuget.org, you cannot reuse the same version number.
- The root [LICENSE](/C:/DEV/PROJECTS/RayoUI/Rayo/LICENSE) is MIT and is referenced by the package metadata.

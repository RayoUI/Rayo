# Built-in icon assets

Place built-in SVG icons in this directory using lowercase kebab-case names, for
example `delete.svg`. They are embedded into the Rayo assembly at build time and
can be loaded with `IconAssets.FromName("delete")`.

To migrate an existing entry in `Icons.cs` without changing its callers, append:

```csharp
.UseImageSource(IconAssets.FromName("delete"));
```

Keep its draw commands during the transition if another control still uses them.

Use a `viewBox` and avoid a fixed `fill` colour when tinting is required by a
control theme. `ButtonIcon` applies its `IconColor` through `Image.Tint`.

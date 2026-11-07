using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot scripts (linked into the test output by the test project) and the blueprint and checks the
/// Godot glue of #90: the cached crop mesh builder <c>CropModels</c>, <c>TileView</c> drawing it with the shared
/// material, the prototype models moved to <c>PrototypeCropModels</c>, and the sandbox's <c>Crops</c> row.
/// </summary>
public class CropModelsSourceTests
{
    private static readonly Regex CropModelsUse = new(@"(?<![A-Za-z_])CropModels\.", RegexOptions.CultureInvariant);

    // ---- AC8: one merged, cached mesh per (crop, stage), drawn with the shared material ----

    [Fact]
    public void AC8_crop_models_builds_one_merged_mesh_per_crop_and_stage()
    {
        var source = ReadGameFile("art", "crops", "CropModels.cs");

        Assert.Contains("CropArt.Parts", source, StringComparison.Ordinal);
        Assert.Contains("ArtMeshes.Merge", source, StringComparison.Ordinal);
        Assert.Matches(@"static\s+readonly\s+Dictionary<|static\s+Dictionary<", source);
        Assert.Matches(@"RadialSegments\s*=\s*8\b", source);
        Assert.Matches(@"Rings\s*=\s*4\b", source);
        Assert.Matches(@"RadialSegments\s*=\s*7\b", source);
        Assert.Matches(
            @"public\s+static\s+ArrayMesh\?\s+MeshFor\s*\(\s*CropTypeId\s+crop\s*,\s*GrowthStage\s+stage\s*\)",
            source);
    }

    [Fact]
    public void AC8_tile_view_draws_crop_meshes_with_the_shared_material()
    {
        var source = ReadGameFile("scripts", "Farm", "TileView.cs");

        Assert.Contains("CropModels.MeshFor(", source, StringComparison.Ordinal);
        Assert.Contains("ArtMaterials.Shared", source, StringComparison.Ordinal);
        Assert.Contains("ShadowCastingSetting.On", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StandardMaterial3D", source, StringComparison.Ordinal);
        Assert.DoesNotContain("class CropModels", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Flat(", source, StringComparison.Ordinal);
    }

    // ---- AC9: moonmelon, sunbell and starcorn keep their prototype models ----

    [Fact]
    public void AC9_prototype_models_keep_moonmelon_sunbell_and_starcorn()
    {
        var prototype = ReadGameFile("scripts", "Farm", "PrototypeCropModels.cs");
        var tileView = ReadGameFile("scripts", "Farm", "TileView.cs");

        Assert.Contains("internal static class PrototypeCropModels", prototype, StringComparison.Ordinal);
        Assert.Contains("Moonmelon()", prototype, StringComparison.Ordinal);
        Assert.Contains("Sunbell()", prototype, StringComparison.Ordinal);
        Assert.Contains("Starcorn()", prototype, StringComparison.Ordinal);
        Assert.Contains("1.6f", prototype, StringComparison.Ordinal);
        Assert.DoesNotContain("Emberroot()", prototype, StringComparison.Ordinal);
        Assert.DoesNotContain("Dusktuber()", prototype, StringComparison.Ordinal);
        Assert.Contains("PrototypeCropModels.", tileView, StringComparison.Ordinal);
    }

    // ---- AC10: the sandbox's Crops row ----

    [Fact]
    public void AC10_sandbox_builds_a_crops_row_from_crop_models()
    {
        var source = ReadGameFile("art", "sandbox", "ArtSandbox.cs");

        Assert.Contains("BuildCrops()", source, StringComparison.Ordinal);
        Assert.Contains("\"Crops\"", source, StringComparison.Ordinal);
        Assert.Contains("CropModels.MeshFor(", source, StringComparison.Ordinal);
        Assert.Contains("\"soil-tilled\"", source, StringComparison.Ordinal);
        Assert.Contains("0.96f", source, StringComparison.Ordinal);
    }

    // ---- Layering (ART_DIRECTION §8): only TileView and the sandbox use the crop builder ----

    [Fact]
    public void Layering_only_tile_view_and_the_sandbox_use_crop_models()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "game");
        var allowed = new[] { "TileView.cs", "ArtSandbox.cs", "CropModels.cs" };

        var users = new[] { "scripts", "art" }
            .Select(folder => Path.Combine(root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            .Where(path => !allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            .Where(path => CropModelsUse.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();

        Assert.Empty(users);
    }

    // ---- Blueprint ----

    [Fact]
    public void Docs_blueprint_names_the_crop_builder()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "docs", "requirements", "technical-blueprint.md");
        Assert.True(File.Exists(path), "docs/requirements/technical-blueprint.md not found");
        var blueprint = File.ReadAllText(path);

        Assert.Contains("crops/CropModels.cs", blueprint, StringComparison.Ordinal);
        Assert.Contains("the crop builder `CropModels`", blueprint, StringComparison.Ordinal);
    }

    // ---- helpers ----

    private static string ReadGameFile(params string[] parts)
    {
        var path = Path.Combine(new[] { AppContext.BaseDirectory, "game" }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"game/{string.Join('/', parts)} not found");
        return File.ReadAllText(path);
    }
}

using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot scripts (linked into the test output by the test project) and the blueprint and checks the
/// Godot glue of #90: the cached crop mesh builder <c>CropModels</c>, <c>TileView</c> drawing it with the shared
/// material, and the sandbox's <c>Crops</c> row; and #92's removal of the prototype fallback: with moonmelon drawn,
/// <c>PrototypeCropModels</c> is deleted and <c>CropModels.MeshFor</c> always returns a mesh.
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
            @"public\s+static\s+ArrayMesh\s+MeshFor\s*\(\s*CropTypeId\s+crop\s*,\s*GrowthStage\s+stage\s*\)",
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

    // ---- AC9: the prototype fallback is gone (#92 drew moonmelon, the last crop) ----

    /// <summary>
    /// Read from the repository, not the test output: the output keeps a stale copy of a deleted script, since the
    /// test project copies game scripts with <c>PreserveNewest</c>.
    /// </summary>
    [Fact]
    public void AC9_prototype_crop_models_are_gone()
    {
        var root = RepoRoot();
        var prototype = Path.Combine(root, "game", "scripts", "Farm", "PrototypeCropModels.cs");
        var tileViewPath = Path.Combine(root, "game", "scripts", "Farm", "TileView.cs");
        Assert.True(File.Exists(tileViewPath), $"{tileViewPath} is missing");
        var tileView = File.ReadAllText(tileViewPath);

        Assert.False(File.Exists(prototype), "game/scripts/Farm/PrototypeCropModels.cs still exists");
        Assert.False(File.Exists(prototype + ".uid"), "game/scripts/Farm/PrototypeCropModels.cs.uid still exists");
        Assert.DoesNotContain("PrototypeCropModels", tileView, StringComparison.Ordinal);
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

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LongWayHome.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"no folder holding LongWayHome.sln above {AppContext.BaseDirectory}");
        return dir!.FullName;
    }

    private static string ReadGameFile(params string[] parts)
    {
        var path = Path.Combine(new[] { AppContext.BaseDirectory, "game" }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"game/{string.Join('/', parts)} not found");
        return File.ReadAllText(path);
    }
}

#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The town's meshes (ART_DIRECTION §7.3 and §7.5): the general store at each <see cref="StoreStage"/> from
/// <see cref="GeneralStoreArt"/>, the carpenter at each <see cref="CarpenterStage"/> from <see cref="CarpenterArt"/> and the
/// blacksmith at each <see cref="BlacksmithStage"/> from <see cref="BlacksmithArt"/>,
/// with their night-only window lights, and the rebuild sign from <see cref="RebuildSignArt"/>
/// with its night-only amber outline, each one merged mesh under the shared material by <see cref="PartMeshes"/>. Each
/// mesh is built once and shared. Shown only in the art sandbox for now.
/// </summary>
public static class TownModels
{
    private static readonly Dictionary<string, ArrayMesh> Meshes = new();

    /// <summary>The general store at <paramref name="stage"/>.</summary>
    public static ArrayMesh Store(StoreStage stage) => Cached($"store-{stage}", () => GeneralStoreArt.For(stage));

    /// <summary>The store's lit window panes at <paramref name="stage"/>, to show only at night.</summary>
    public static ArrayMesh StoreNightLights(StoreStage stage) => Cached($"store-night-{stage}", () => GeneralStoreArt.NightLights(stage));

    /// <summary>The carpenter at <paramref name="stage"/>.</summary>
    public static ArrayMesh Carpenter(CarpenterStage stage) => Cached($"carpenter-{stage}", () => CarpenterArt.For(stage));

    /// <summary>The carpenter's lit window panes at <paramref name="stage"/>, to show only at night.</summary>
    public static ArrayMesh CarpenterNightLights(CarpenterStage stage) => Cached($"carpenter-night-{stage}", () => CarpenterArt.NightLights(stage));

    /// <summary>The blacksmith at <paramref name="stage"/>.</summary>
    public static ArrayMesh Blacksmith(BlacksmithStage stage) => Cached($"blacksmith-{stage}", () => BlacksmithArt.For(stage));

    /// <summary>The blacksmith's lit window pane at <paramref name="stage"/>, to show only at night.</summary>
    public static ArrayMesh BlacksmithNightLights(BlacksmithStage stage) => Cached($"blacksmith-night-{stage}", () => BlacksmithArt.NightLights(stage));

    /// <summary>A rebuild sign with the given materials delivered.</summary>
    public static ArrayMesh RebuildSign(bool wood, bool stone, bool gold) =>
        Cached($"sign-{wood}-{stone}-{gold}", () => RebuildSignArt.Parts(wood, stone, gold));

    /// <summary>The rebuild sign's amber outline, to show only at night.</summary>
    public static ArrayMesh RebuildSignOutline() => Cached("sign-outline", () => RebuildSignArt.NightOutline);

    private static ArrayMesh Cached(string key, System.Func<IReadOnlyList<CropPart>> parts)
    {
        if (!Meshes.TryGetValue(key, out var mesh))
        {
            mesh = PartMeshes.Merge(parts());
            Meshes[key] = mesh;
        }

        return mesh;
    }
}

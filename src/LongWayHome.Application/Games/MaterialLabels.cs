using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The canonical display labels of materials (canonical-values.md MaterialId).</summary>
public static class MaterialLabels
{
    /// <summary>"Wood", "Sap", "Stone", "Rosk ore", "Groth ore", "Glimmercap", "Plank", "Sealed plank" or "Rosk pump".</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined material.</exception>
    public static string For(MaterialId material) => material switch
    {
        MaterialId.Wood => "Wood",
        MaterialId.Sap => "Sap",
        MaterialId.Stone => "Stone",
        MaterialId.RoskOre => "Rosk ore",
        MaterialId.GrothOre => "Groth ore",
        MaterialId.Glimmercap => "Glimmercap",
        MaterialId.Plank => "Plank",
        MaterialId.SealedPlank => "Sealed plank",
        MaterialId.RoskPump => "Rosk pump",
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown material."),
    };
}

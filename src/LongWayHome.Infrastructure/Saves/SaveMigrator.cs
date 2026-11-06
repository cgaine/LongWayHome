using System.Text.Json.Nodes;

namespace LongWayHome.Infrastructure.Saves;

/// <summary>
/// Upgrades a save document step by step from its <c>saveVersion</c> to the target version. <c>Steps[i]</c> upgrades a
/// document from version <c>i + 1</c> to <c>i + 2</c>. A missing or non-integer version, a version below 1, or one
/// above the target ("made by a newer version of the game") is refused.
/// </summary>
public sealed class SaveMigrator
{
    /// <summary>The save format version this build writes.</summary>
    public const int CurrentVersion = 1;

    /// <summary>A migrator up to <see cref="CurrentVersion"/> with the given steps.</summary>
    public SaveMigrator(IReadOnlyList<Func<JsonObject, JsonObject>> steps)
        : this(CurrentVersion, steps)
    {
    }

    /// <summary>A migrator up to the given target version, with one step per version below it.</summary>
    public SaveMigrator(int targetVersion, IReadOnlyList<Func<JsonObject, JsonObject>> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        TargetVersion = targetVersion;
        Steps = steps;
    }

    /// <summary>The migrator with no steps, for the current version.</summary>
    public static SaveMigrator Default { get; } = new([]);

    /// <summary>The version documents are upgraded to; <see cref="CurrentVersion"/> unless a test says otherwise.</summary>
    public int TargetVersion { get; }

    public IReadOnlyList<Func<JsonObject, JsonObject>> Steps { get; }

    /// <summary>Applies the steps in order from the document's saveVersion up to <see cref="TargetVersion"/>.</summary>
    public JsonObject Migrate(JsonObject document) => throw new NotImplementedException();
}

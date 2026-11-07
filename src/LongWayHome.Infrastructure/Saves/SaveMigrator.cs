using System.Text.Json.Nodes;
using LongWayHome.Infrastructure.Saves.Migrations;

namespace LongWayHome.Infrastructure.Saves;

/// <summary>
/// Upgrades a save document step by step from its <c>saveVersion</c> to the target version. <c>Steps[i]</c> upgrades a
/// document from version <c>i + 1</c> to <c>i + 2</c>. A missing or non-integer version, a version below 1, or one
/// above the target ("made by a newer version of the game") is refused.
/// </summary>
public sealed class SaveMigrator
{
    /// <summary>The save format version this build writes.</summary>
    public const int CurrentVersion = 2;

    /// <summary>The name of the version property in a save document.</summary>
    public const string VersionProperty = "saveVersion";

    /// <summary>A migrator up to <see cref="CurrentVersion"/> with the given steps.</summary>
    public SaveMigrator(IReadOnlyList<Func<JsonObject, JsonObject>> steps)
        : this(CurrentVersion, steps)
    {
    }

    /// <summary>
    /// A migrator up to the given target version, with one step per version below it: exactly
    /// <c>targetVersion - 1</c> steps.
    /// </summary>
    public SaveMigrator(int targetVersion, IReadOnlyList<Func<JsonObject, JsonObject>> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetVersion, 1);
        if (steps.Count != targetVersion - 1)
        {
            throw new ArgumentException(
                $"A migrator to version {targetVersion} needs {targetVersion - 1} steps, got {steps.Count}.",
                nameof(steps));
        }

        if (steps.Any(step => step is null))
        {
            throw new ArgumentException("A migration step must not be null.", nameof(steps));
        }

        TargetVersion = targetVersion;
        Steps = steps;
    }

    /// <summary>The migrator for the current version: the 1 → 2 step (<see cref="SaveV1ToV2"/>).</summary>
    public static SaveMigrator Default { get; } = new([SaveV1ToV2.Migrate]);

    /// <summary>The version documents are upgraded to; <see cref="CurrentVersion"/> unless a test says otherwise.</summary>
    public int TargetVersion { get; }

    public IReadOnlyList<Func<JsonObject, JsonObject>> Steps { get; }

    /// <summary>
    /// Applies the steps in order from the document's saveVersion up to <see cref="TargetVersion"/>, setting the
    /// version after each step. A document whose version is refused throws <see cref="InvalidDataException"/>.
    /// </summary>
    public JsonObject Migrate(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var version = ReadVersion(document);
        if (version < 1)
        {
            throw new InvalidDataException($"{VersionProperty} must be at least 1, was {version}.");
        }

        if (version > TargetVersion)
        {
            throw new InvalidDataException(
                $"{VersionProperty} {version} was made by a newer version of the game (this one reads up to {TargetVersion}).");
        }

        for (; version < TargetVersion; version++)
        {
            document = Steps[version - 1](document)
                ?? throw new InvalidDataException($"The migration from {VersionProperty} {version} returned nothing.");
            document[VersionProperty] = version + 1;
        }

        return document;
    }

    private static int ReadVersion(JsonObject document)
    {
        if (!document.TryGetPropertyValue(VersionProperty, out var node) || node is null)
        {
            throw new InvalidDataException($"{VersionProperty} is missing.");
        }

        if (node is not JsonValue value || !value.TryGetValue<int>(out var version))
        {
            throw new InvalidDataException($"{VersionProperty} must be an integer, was {node.ToJsonString()}.");
        }

        return version;
    }
}

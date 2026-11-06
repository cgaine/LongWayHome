using System.Text.Json.Nodes;
using LongWayHome.Infrastructure.Saves;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// <see cref="SaveMigrator"/> upgrades a save document one version at a time. Version 1 is the only real version, so
/// these tests build migrators with an explicit target version to exercise steps.
/// </summary>
public class SaveMigratorTests
{
    private static JsonObject Document(int version) => new() { ["saveVersion"] = version, ["payload"] = "kept" };

    [Fact]
    public void AC11_migrator_applies_a_step_from_version_1_to_2()
    {
        var migrator = new SaveMigrator(2, [doc =>
        {
            doc["saveVersion"] = 2;
            doc["migratedTo2"] = true;
            return doc;
        }]);

        var result = migrator.Migrate(Document(1));

        Assert.Equal(2, result["saveVersion"]!.GetValue<int>());
        Assert.True(result["migratedTo2"]!.GetValue<bool>());
        Assert.Equal("kept", result["payload"]!.GetValue<string>());
    }

    [Fact]
    public void AC11_migrator_applies_steps_in_order_from_the_files_version()
    {
        var applied = new List<string>();
        Func<JsonObject, JsonObject> Step(int to) => doc =>
        {
            applied.Add($"{doc["saveVersion"]!.GetValue<int>()}->{to}");
            doc["saveVersion"] = to;
            return doc;
        };
        var migrator = new SaveMigrator(4, [Step(2), Step(3), Step(4)]);

        var result = migrator.Migrate(Document(2));

        Assert.Equal(["2->3", "3->4"], applied);
        Assert.Equal(4, result["saveVersion"]!.GetValue<int>());
    }

    [Fact]
    public void AC11_default_migrator_leaves_a_version_1_document_as_it_is()
    {
        var result = SaveMigrator.Default.Migrate(Document(1));

        Assert.Equal(Document(1).ToJsonString(), result.ToJsonString());
    }
}

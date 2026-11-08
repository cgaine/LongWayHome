using System.Text.Json.Nodes;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Infrastructure.Saves.Migrations;

/// <summary>
/// Moves a Phase 1 12 x 9 <c>farmPlot</c> into the grown grid (#150, BR-T1): each saved Tile moves from (x, y) to
/// <see cref="FarmPlot.Phase1Block"/>'s origin + (x, y), the plot becomes <see cref="FarmPlot.Width"/> x
/// <see cref="FarmPlot.Height"/>, and every other position is added as a grass, unwatered Tile with no Planting. Used by
/// <see cref="SaveV1ToV2"/> and for a version 2 file written before the grid grew. Any other size is left alone, so
/// loading refuses it as before.
/// </summary>
public static class Phase1PlotPlacement
{
    /// <summary>Places a 12 x 9 <c>farmPlot</c> in the grown grid; any other document is returned unchanged.</summary>
    public static JsonObject Apply(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var block = FarmPlot.Phase1Block;
        if (document["farmPlot"] is not JsonObject plot
            || !IsInt(plot["width"], block.Width)
            || !IsInt(plot["height"], block.Height)
            || plot["tiles"] is not JsonArray tiles)
        {
            return document;
        }

        foreach (var tile in tiles.OfType<JsonObject>())
        {
            // A position that isn't on the old plot isn't moved, so loading refuses it as before.
            if (tile["position"] is JsonObject position
                && TryInt(position["x"], out var x)
                && TryInt(position["y"], out var y)
                && x >= 0 && x < block.Width && y >= 0 && y < block.Height)
            {
                position["x"] = x + block.OriginX;
                position["y"] = y + block.OriginY;
            }
        }

        // BR-T1: the rest of the grid is fresh grass. A Tile missing from the old plot stays missing, so loading
        // refuses it as before.
        for (var y = 0; y < FarmPlot.Height; y++)
        {
            for (var x = 0; x < FarmPlot.Width; x++)
            {
                if (!block.Contains(new GridPosition(x, y)))
                {
                    tiles.Add(new JsonObject
                    {
                        ["position"] = new JsonObject { ["x"] = x, ["y"] = y },
                        ["state"] = "grass",
                        ["watered"] = false,
                        ["planting"] = null,
                    });
                }
            }
        }

        plot["width"] = FarmPlot.Width;
        plot["height"] = FarmPlot.Height;
        return document;
    }

    private static bool IsInt(JsonNode? node, int expected) => TryInt(node, out var value) && value == expected;

    private static bool TryInt(JsonNode? node, out int value)
    {
        value = 0;
        return node is JsonValue json && json.TryGetValue(out value);
    }
}

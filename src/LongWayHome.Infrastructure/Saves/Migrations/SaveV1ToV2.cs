using System.Text.Json.Nodes;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Saves.Migrations;

/// <summary>
/// The save 1 → 2 step, on the JSON document. It packs a Phase 1 Player's <c>tools</c> and <c>inventory</c> into
/// <c>player.backpack</c> (REQ-47). Later Phase 2 features extend this same step with their own fields.
/// </summary>
public static class SaveV1ToV2
{
    private static readonly int[] Capacities = [12, 24, 36];

    /// <summary>
    /// Builds <c>player.backpack</c>: the hoe in Slot 0 and the watering can in Slot 1 at <c>basic</c> (the can at
    /// <c>rosk</c> when <c>player.wateringCan.upgraded</c> is true), then each inventory stack in saved order, split into
    /// Slots of up to 99 (BR-K2). Capacity is 12, raised to 24 then 36 only when the Slots don't fit (BR-K1); what
    /// doesn't fit in 36 is discarded. <c>hotbarRow</c> is 0. Removes <c>player.tools</c> and
    /// <c>player.inventory</c>, and writes the default <c>residents</c> (#138) and
    /// <c>openingDone: true</c> (#139, BR-I1) and an empty <c>purchasedListings</c> (#145, BR-L5). A document whose player, tools or inventory (or one of its stacks) isn't the expected shape is returned
    /// unchanged, so loading it refuses it as before.
    /// </summary>
    public static JsonObject Migrate(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document["player"] is not JsonObject player
            || player["tools"] is not JsonArray
            || player["inventory"] is not JsonArray inventory
            || inventory.Any(node => node is not JsonObject))
        {
            return document;
        }

        // BR-K6, BR-P6: a Phase 1 Player always has both tools; the Can upgrade made the can a Rosk can (BR-W1).
        var upgraded = player["wateringCan"] is JsonObject can
            && can["upgraded"] is JsonValue flag
            && flag.TryGetValue<bool>(out var isUpgraded)
            && isUpgraded;
        var filled = new List<(JsonNode? Item, JsonNode? Quantity, string? ToolTier)>
        {
            (Item("tool", "hoe"), 1, "basic"),
            (Item("tool", "watering_can"), 1, upgraded ? "rosk" : "basic"),
        };

        foreach (var stack in inventory.Cast<JsonObject>())
        {
            JsonObject Copy() => new() { ["kind"] = stack["kind"]?.DeepClone(), ["id"] = stack["cropTypeId"]?.DeepClone() };
            if (stack["quantity"] is not JsonValue value || !value.TryGetValue<int>(out var quantity) || quantity < 1)
            {
                // A quantity a v1 save couldn't hold stays as one Slot, so loading still refuses it (BR-K2).
                filled.Add((Copy(), stack["quantity"]?.DeepClone(), null));
                continue;
            }

            // BR-K2: at most 99 of one item per Slot; the same item may fill several Slots.
            for (var left = quantity; left > 0; left -= Backpack.MaxStack)
            {
                filled.Add((Copy(), Math.Min(left, Backpack.MaxStack), null));
            }
        }

        // BR-K1: the smallest capacity the Slots fit in; past 36 the excess is discarded.
        var capacity = Capacities.FirstOrDefault(size => size >= filled.Count, Capacities[^1]);
        var slots = new JsonArray();
        for (var index = 0; index < capacity; index++)
        {
            var (item, quantity, tier) = index < filled.Count ? filled[index] : (null, 0, null);
            slots.Add(new JsonObject
            {
                ["index"] = index,
                ["item"] = item,
                ["quantity"] = quantity,
                ["toolTier"] = tier,
            });
        }

        player.Remove("tools");
        player.Remove("inventory");
        player["backpack"] = new JsonObject { ["capacity"] = capacity, ["hotbarRow"] = 0, ["slots"] = slots };

        // BR-R1 (#138): a Phase 1 Game has the new-Game Residents, Maggie and Clyde present.
        document["residents"] = new JsonArray(Game.DefaultResidents()
            .Select(resident => (JsonNode)new JsonObject
            {
                ["id"] = ResidentCodes.Code(resident.Id),
                ["present"] = resident.Present,
            })
            .ToArray());

        // BR-I1 (#139): a Phase 1 Game is underway, so it never plays the Opening.
        document["openingDone"] = true;

        // BR-L5 (#145): a Phase 1 Game has bought no once-per-game listing.
        document["purchasedListings"] = new JsonArray();
        return document;
    }

    private static JsonObject Item(string kind, string id) => new() { ["kind"] = kind, ["id"] = id };
}

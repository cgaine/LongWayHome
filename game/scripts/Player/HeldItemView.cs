#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Art;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Player;

/// <summary>
/// Keeps the held item in the farmer's right hand (#102): shows <see cref="HeldItemSelection.Held"/> (what the held
/// hotbar position holds; nothing for an empty or crop slot) on the <see cref="FarmerModel"/> under the Player's
/// <c>Model</c> on start, when the selection changes and on <see cref="GameSession.InventoryChanged"/>, which covers
/// planting the last seed of the held slot and cycling the hotbar row (neither raises
/// <see cref="HeldItemSelection.SelectionChanged"/>). A child of the Player, after <c>HeldItemSelection</c>. Holds no
/// rules.
/// </summary>
public partial class HeldItemView : Node
{
    private const string GameRootPath = "/root/GameRoot";

    private GameSession? _session;

    private HeldItemSelection _selection = null!;

    private FarmerModel? _farmer;

    public override void _Ready()
    {
        var player = GetParent();
        _selection = player.GetNode<HeldItemSelection>("HeldItemSelection");
        _farmer = FindFarmer(player.GetNode("Model"));
        if (_farmer is null)
        {
            GD.PushError("HeldItemView: the Player's Model has no FarmerModel.");
            return;
        }

        _selection.SelectionChanged += Refresh;
        // GameRoot.Session is null when the data failed to load and GameRoot is already quitting.
        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is not null)
        {
            _session.InventoryChanged += Refresh;
        }

        Refresh();
    }

    public override void _ExitTree()
    {
        if (_farmer is not null)
        {
            _selection.SelectionChanged -= Refresh;
        }

        if (_session is not null)
        {
            _session.InventoryChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (_selection.Held is { } held)
        {
            _farmer?.ShowHeld(held);
        }
        else
        {
            _farmer?.ShowEmptyHand();
        }
    }

    private static FarmerModel? FindFarmer(Node model)
    {
        foreach (var child in model.GetChildren())
        {
            if (child is FarmerModel farmer)
            {
                return farmer;
            }
        }

        return null;
    }
}

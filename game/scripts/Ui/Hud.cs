#nullable enable

using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Players;
using PlayerState = LongWayHome.Domain.Players.Player;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The heads-up display, top-left: the energy bar with "Energy &lt;n&gt; / 150" and, below it, the held item's label.
/// Redraws when the session raises <see cref="GameSession.PlayerChanged"/> and when the Player's
/// <see cref="HeldItemSelection"/> changes. Holds no rules.
/// </summary>
public partial class Hud : CanvasLayer
{
    private GameSession? _session;

    private HeldItemSelection? _selection;

    private ProgressBar _energyBar = null!;

    private Label _energyLabel = null!;

    private Label _heldLabel = null!;

    public override void _Ready()
    {
        _energyBar = GetNode<ProgressBar>("Panel/Rows/EnergyBar");
        _energyLabel = GetNode<Label>("Panel/Rows/EnergyLabel");
        _heldLabel = GetNode<Label>("Panel/Rows/HeldLabel");
        _energyBar.MaxValue = PlayerState.MaxEnergy;

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            return;
        }

        _selection = GetTree().GetFirstNodeInGroup(PlayerController.Group)?
            .GetChildren()
            .OfType<HeldItemSelection>()
            .FirstOrDefault();

        _session.PlayerChanged += Refresh;
        if (_selection is not null)
        {
            _selection.SelectionChanged += Refresh;
        }

        Refresh();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.PlayerChanged -= Refresh;
        }

        if (_selection is not null)
        {
            _selection.SelectionChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        var energy = _session!.Game.Player.Energy;
        _energyBar.Value = energy;
        _energyLabel.Text = $"Energy {energy} / {PlayerState.MaxEnergy}";
        _heldLabel.Text = (_selection?.Held ?? HeldItem.Of(ToolId.Hoe)).DisplayLabel;
    }
}

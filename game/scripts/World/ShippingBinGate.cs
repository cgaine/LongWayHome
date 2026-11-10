#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.World;

/// <summary>
/// Puts the farm Shipping Bin on the farm once the general store is at stage 4 (BR-S3, #166): instances
/// <c>FarmShippingBin.tscn</c> (the bin's body and its <c>bin</c> point with <see cref="BinShip"/>) as its child on
/// <c>_Ready</c> when <c>Game.HasShippingBin</c>, else on the first <see cref="GameSession.NightCompleted"/> after which
/// it is true, so the bin stands the morning after the stage 4 rebuild without the farm scene reloading. A node of
/// <c>Farm.tscn</c> at the origin, ahead of <c>Common</c>, so on a Night it adds the bin before the Hud looks for the
/// <c>bin</c> point again. Holds no rules: whether the bin exists is the Game's.
/// </summary>
public partial class ShippingBinGate : Node3D
{
    private const string BinScenePath = "res://scenes/FarmShippingBin.tscn";

    private GameSession? _session;

    private bool _placed;

    public override void _Ready()
    {
        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting.
            return;
        }

        if (_session.Game.HasShippingBin)
        {
            PlaceBin();
            return;
        }

        _session.NightCompleted += OnNightCompleted;
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.NightCompleted -= OnNightCompleted;
        }
    }

    private void OnNightCompleted()
    {
        if (!_session!.Game.HasShippingBin)
        {
            return;
        }

        // A Building's stage only rises (BR-B1, BR-B3), so once the bin exists it stays and the gate stops listening.
        _session.NightCompleted -= OnNightCompleted;
        PlaceBin();
    }

    private void PlaceBin()
    {
        if (_placed)
        {
            return;
        }

        var scene = GD.Load<PackedScene>(BinScenePath);
        if (scene is null)
        {
            GD.PushError($"Could not load {BinScenePath}");
            return;
        }

        AddChild(scene.Instantiate());
        _placed = true;
    }
}

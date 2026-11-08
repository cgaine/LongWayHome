#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Art;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;
using ResidentKind = LongWayHome.Domain.Residents.ResidentId;

namespace LongWayHome.Game.World;

/// <summary>
/// One Resident standing in an area (#138), the root of <c>Resident.tscn</c>: <see cref="ResidentId"/> is the
/// Resident's code (<see cref="ResidentCodes"/>). Only when the Game says the Resident is present
/// (<c>Game.IsPresent</c>, BR-R1) does it show the Resident's <see cref="ResidentModels"/> mesh and a
/// <see cref="StaticBody3D"/> capsule the Player walks round, and name its <see cref="InteractionPoint"/> child after the
/// code, so the Hud's Talk hint and <c>ResidentTalk</c> find it; otherwise the point keeps no id and nothing shows. The
/// scene's placement sets the rest facing; while the Player is in the point's range the model turns to face him, and
/// turns back once he leaves. An unknown code is reported once and nothing shows. Holds no rules.
/// </summary>
public partial class ResidentView : Node3D
{
    /// <summary>The capsule round a Resident: about the farmer's (0.35 m radius) and as tall as the tallest Resident.</summary>
    private const float ColliderRadius = 0.3f;

    private const float ColliderHeight = 1.5f;

    /// <summary>How quickly the model turns, per second (exponential ease towards the target yaw).</summary>
    private const float TurnRate = 8f;

    /// <summary>The Resident's code, e.g. <c>maggie</c> or <c>clyde</c>.</summary>
    [Export] public string ResidentId { get; set; } = "";

    private InteractionPoint _point = null!;

    private Node3D? _model;

    private Node3D? _player;

    public override void _Ready()
    {
        _point = GetNode<InteractionPoint>("Point");
        SetProcess(false);

        if (!ResidentCodes.TryParse(ResidentId, out var id))
        {
            GD.PushError($"{GetPath()}: ResidentId \"{ResidentId}\" is not a resident (maggie, clyde, tom, june, jesse, mira).");
            return;
        }

        var session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (session is null || !session.Game.IsPresent(id))
        {
            // GameRoot has already reported the data errors and is quitting, or the Resident hasn't arrived (BR-R1).
            return;
        }

        _point.PointId = ResidentCodes.Code(id);
        _model = new MeshInstance3D
        {
            Name = "Model",
            Mesh = ResidentModels.MeshFor(id),
            MaterialOverride = ResidentModels.Material,
        };
        AddChild(_model);

        var body = new StaticBody3D { Name = "Body" };
        body.AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = ColliderRadius, Height = ColliderHeight },
            Position = new Vector3(0f, ColliderHeight / 2f, 0f),
        });
        AddChild(body);
        _point.PlayerEntered += OnPlayerEntered;
        SetProcess(_point.IsPlayerInRange);
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_point))
        {
            _point.PlayerEntered -= OnPlayerEntered;
        }
    }

    private void OnPlayerEntered() => SetProcess(true);

    public override void _Process(double delta)
    {
        // The rest facing is the scene's (local yaw 0); in range, the yaw that points the model's +Z at the Player.
        var target = 0f;
        if (_point.IsPlayerInRange && FindPlayer() is { } player)
        {
            var offset = player.GlobalPosition - GlobalPosition;
            if (offset.X * offset.X + offset.Z * offset.Z > 1e-4f)
            {
                target = Mathf.Atan2(offset.X, offset.Z) - GlobalRotation.Y;
            }
        }

        var yaw = _model!.Rotation.Y;
        if (!_point.IsPlayerInRange && Mathf.Abs(Mathf.AngleDifference(yaw, target)) < 1e-3f)
        {
            // Back at rest with nobody near: stop turning until the Player comes in range again.
            _model.Rotation = Vector3.Zero;
            SetProcess(false);
            return;
        }

        var step = 1f - Mathf.Exp(-TurnRate * (float)delta);
        _model.Rotation = new Vector3(0f, Mathf.LerpAngle(yaw, target, step), 0f);
    }

    private Node3D? FindPlayer()
    {
        if (_player is null || !IsInstanceValid(_player) || !_player.IsInsideTree())
        {
            _player = GetTree().GetFirstNodeInGroup(PlayerController.Group) as Node3D;
        }

        return _player;
    }
}

#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.World;

/// <summary>
/// Takes the Player home after a Night that ended away from the farm (#136). Part of <c>AreaCommon.tscn</c>, so every
/// area scene has one; in the farm's own scene it does nothing. On <see cref="GameSession.NightCompleted"/> in any
/// other area it keeps the night message on <see cref="GameRoot.PendingNight"/> (the Day's end reason, whether the save
/// failed, and the Buildings the Night rebuilt, #165) for the farm's <c>NightTransition</c>, clears any pending arrival
/// so the Player wakes at the door, and changes to the farm scene (deferred, so the session's event finishes first).
/// Holds no rules: the session has already put the Player on the farm.
/// </summary>
public partial class NightReturn : Node
{
    private GameRoot? _root;

    private GameSession? _session;

    private DayEndReason _reason;

    private bool _saveFailed;

    private IReadOnlyList<BuildingId> _rebuilt = [];

    public override void _Ready()
    {
        _root = GetNode<GameRoot>("/root/GameRoot");
        _session = _root.Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting.
            return;
        }

        _session.DayEnded += OnDayEnded;
        _session.SaveFailed += OnSaveFailed;
        _session.BuildingsRebuilt += OnBuildingsRebuilt;
        _session.NightCompleted += OnNightCompleted;
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.DayEnded -= OnDayEnded;
            _session.SaveFailed -= OnSaveFailed;
            _session.BuildingsRebuilt -= OnBuildingsRebuilt;
            _session.NightCompleted -= OnNightCompleted;
        }
    }

    /// <summary>Whether this node is in the farm's scene: the scene that owns the instanced AreaCommon.</summary>
    private bool IsOnFarmScene() => AreaScenes.RootOf(this).SceneFilePath == AreaScenes.FarmScene;

    private void OnDayEnded(DayEndReason reason)
    {
        _reason = reason;
        _saveFailed = false;
        _rebuilt = [];
    }

    private void OnSaveFailed(string reason) => _saveFailed = true;

    private void OnBuildingsRebuilt(IReadOnlyList<BuildingId> ids) => _rebuilt = ids;

    private void OnNightCompleted()
    {
        if (IsOnFarmScene())
        {
            return;
        }

        _root!.PendingNight = new PendingNightMessage(_reason, _saveFailed, _rebuilt);
        _root.PendingArrivalId = null;
        CallDeferred(MethodName.GoHome);
    }

    private void GoHome()
    {
        var error = GetTree().ChangeSceneToFile(AreaScenes.FarmScene);
        if (error != Error.Ok)
        {
            GD.PushError($"Could not open {AreaScenes.FarmScene}: {error}");
            _root!.PendingNight = null;
        }
    }
}

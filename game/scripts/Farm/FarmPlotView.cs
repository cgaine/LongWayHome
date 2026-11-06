#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Draws the Farm plot of the current Game: one <see cref="TileView"/> per Tile, placed at
/// <see cref="FarmGrid.ToWorld"/>, and redraws a Tile when the session raises <see cref="GameSession.TileChanged"/>.
/// Reads state from the <see cref="GameRoot"/> session. Holds no rules.
/// </summary>
public partial class FarmPlotView : Node3D
{
    private const string GameRootPath = "/root/GameRoot";

    private readonly Dictionary<GridPosition, TileView> _views = new();

    private GameSession? _session;

    public override void _Ready()
    {
        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting; there is nothing to draw.
            return;
        }

        foreach (var tile in _session.Game.FarmPlot.Tiles)
        {
            var view = new TileView
            {
                Name = $"Tile_{tile.Position.X}_{tile.Position.Y}",
                Position = FarmGrid.ToWorld(tile.Position),
            };
            AddChild(view);
            view.Refresh(tile);
            _views[tile.Position] = view;
        }

        _session.TileChanged += OnTileChanged;
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.TileChanged -= OnTileChanged;
        }
    }

    private void OnTileChanged(GridPosition position)
    {
        if (_views.TryGetValue(position, out var view))
        {
            view.Refresh(_session!.Game.FarmPlot[position]);
        }
    }
}

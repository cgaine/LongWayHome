#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Game.Art;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Draws the Farm plot of the current Game: one <see cref="TileView"/> per Tile, placed at
/// <see cref="FarmGrid.ToWorld"/>, and redraws a Tile when the session raises <see cref="GameSession.TileChanged"/>
/// and every Tile on <see cref="GameSession.NightCompleted"/>. Under the Tiles lies the <c>PlotBase</c>, a dark
/// 12 x 9 m soil plane (#78) that shows through the gaps between the Tiles' slabs as grooves, so the grid reads without
/// drawn lines. Reads state from the <see cref="GameRoot"/> session. Holds no rules.
/// </summary>
public partial class FarmPlotView : Node3D
{
    private const string GameRootPath = "/root/GameRoot";

    /// <summary>Just above the meadow's ground (y 0), which is the only layer inside the plot, and below the slabs.</summary>
    private const float PlotBaseHeight = 0.002f;

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

        AddPlotBase();

        foreach (var tile in _session.Game.FarmPlot.Tiles)
        {
            var view = new TileView
            {
                Name = $"Tile_{tile.Position.X}_{tile.Position.Y}",
                Position = FarmGrid.ToWorld(tile.Position),
            };
            AddChild(view);
            view.Refresh(tile, _session.GrowthStageAt(tile.Position));
            _views[tile.Position] = view;
        }

        _session.TileChanged += OnTileChanged;
        _session.NightCompleted += OnNightCompleted;
    }

    /// <summary>The 12 x 9 m base plane, centred under the plot's corner tiles.</summary>
    private void AddPlotBase()
    {
        var first = FarmGrid.ToWorld(new GridPosition(0, 0));
        var last = FarmGrid.ToWorld(new GridPosition(FarmPlot.Width - 1, FarmPlot.Height - 1));
        var plotBase = new MeshInstance3D
        {
            Name = "PlotBase",
            Mesh = new PlaneMesh { Size = new Vector2(12f, 9f) },
            Position = ((first + last) / 2f) with { Y = PlotBaseHeight },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        ArtMaterials.Apply(plotBase, "soil-watered");
        AddChild(plotBase);
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.TileChanged -= OnTileChanged;
            _session.NightCompleted -= OnNightCompleted;
        }
    }

    private void OnNightCompleted()
    {
        foreach (var position in _views.Keys)
        {
            OnTileChanged(position);
        }
    }

    private void OnTileChanged(GridPosition position)
    {
        if (_views.TryGetValue(position, out var view))
        {
            view.Refresh(_session!.Game.FarmPlot[position], _session.GrowthStageAt(position));
        }
    }
}

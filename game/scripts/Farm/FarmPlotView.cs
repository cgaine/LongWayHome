#nullable enable

using Godot;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Draws the Farm plot of the current Game: one <see cref="TileView"/> per Tile, placed at
/// <see cref="FarmGrid.ToWorld"/>. Reads state from the <see cref="GameRoot"/> session. Holds no rules.
/// </summary>
public partial class FarmPlotView : Node3D
{
    private const string GameRootPath = "/root/GameRoot";

    public override void _Ready()
    {
        var session = GetNode<GameRoot>(GameRootPath).Session;
        if (session is null)
        {
            // The data failed to load and GameRoot is already quitting; there is nothing to draw.
            return;
        }

        foreach (var tile in session.Game.FarmPlot.Tiles)
        {
            var view = new TileView
            {
                Name = $"Tile_{tile.Position.X}_{tile.Position.Y}",
                Position = FarmGrid.ToWorld(tile.Position),
            };
            AddChild(view);
            view.Refresh(tile);
        }
    }
}

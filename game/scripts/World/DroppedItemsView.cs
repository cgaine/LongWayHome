#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Game.Core;
using LongWayHome.Game.Farm;
using LongWayHome.Game.Ui;

namespace LongWayHome.Game.World;

/// <summary>
/// Draws the DroppedItems of the current Game (#130, BR-K3, BR-K4): one small billboard <see cref="Sprite3D"/> per
/// DroppedItem, showing its seed or crop icon (<see cref="ItemIcons.For(Domain.Players.ItemRef)"/>), on its tile at
/// <see cref="FarmGrid.ToWorld"/> just above the slab tops, with several on one tile spread by
/// <see cref="DroppedItemLayout.Offset"/> so each shows. Built from state when ready (so a loaded Game, which has none,
/// shows none) and rebuilt on <see cref="GameSession.DroppedItemsChanged"/> and <see cref="GameSession.NightCompleted"/>
/// (the Night removes them all). No collision, no lights and no shadows (REQ-14). Reads state from the
/// <see cref="GameRoot"/> session and holds no rules.
/// </summary>
public partial class DroppedItemsView : Node3D
{
    private const string GameRootPath = "/root/GameRoot";

    /// <summary>The icon's height on screen, in metres: a small marker, well inside one tile.</summary>
    private const float IconSize = 0.35f;

    /// <summary>The icons' texture width in pixels (#95).</summary>
    private const float IconPixels = 64f;

    /// <summary>The sprite's centre: half an icon above the slab tops (y 0.06), so it never sinks into a tile.</summary>
    private const float Height = 0.06f + (IconSize / 2f) + 0.01f;

    private GameSession? _session;

    public override void _Ready()
    {
        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting; there is nothing to draw.
            return;
        }

        Rebuild();
        _session.DroppedItemsChanged += Rebuild;
        _session.NightCompleted += Rebuild;
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.DroppedItemsChanged -= Rebuild;
            _session.NightCompleted -= Rebuild;
        }
    }

    private void Rebuild()
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        // Counts the items already drawn on each tile, so the next one there takes the next offset.
        var onTile = new Dictionary<GridPosition, int>();
        foreach (var dropped in _session!.Game.DroppedItems)
        {
            if (ItemIcons.For(dropped.Item) is not { } icon)
            {
                continue;
            }

            var index = onTile.GetValueOrDefault(dropped.Position);
            onTile[dropped.Position] = index + 1;
            var offset = DroppedItemLayout.Offset(index);
            var tile = FarmGrid.ToWorld(dropped.Position);
            AddChild(new Sprite3D
            {
                Name = $"Dropped_{dropped.Position.X}_{dropped.Position.Y}_{index}",
                Texture = IconTextures.Get(icon),
                PixelSize = IconSize / IconPixels,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Position = new Vector3(tile.X + offset.X, Height, tile.Z + offset.Z),
            });
        }
    }
}

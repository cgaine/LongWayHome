#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Game.Art;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Draws the 72 x 36 Farm plot of the current Game (#150, REQ-14). Untouched grass is drawn in batches, so the plot
/// holds 30 fps on the Steam Deck: under a <c>GrassBatches</c> node, one <see cref="MultiMeshInstance3D"/> of every
/// grass Tile's slab (<c>GrassSlabs</c>, in the Season's ground role), one per <see cref="GrassTileArt"/> layout
/// (<c>Grass0</c> to <c>Grass7</c>, each drawing <see cref="GrassTileModels.MeshFor"/> for the Season, every Tile with
/// its quarter turn) and one per native plant kind (<c>NativeFrond</c>, <c>NativePuffball</c>), planned by the pure
/// <see cref="GrassBatches"/> and <see cref="TileDecoration"/>, so grass looks exactly as a grass <see cref="TileView"/>
/// draws it. A Tile that isn't grass gets its own <see cref="TileView"/>, placed at <see cref="FarmGrid.ToWorld"/>.
/// <para>
/// On <see cref="GameSession.TileChanged"/> a Tile's view is redrawn; a Tile that has just left grass (tilled) gets its
/// view at once and its batch instances are hidden. It never goes back (BR-T8). On
/// <see cref="GameSession.NightCompleted"/>, the only time the Season, whose grass colours the grass (#79), can change
/// (BR-G1), every view is redrawn and the batches are rebuilt for the Season. Under the Tiles lies the <c>PlotBase</c>,
/// a dark 72 x 36 m soil plane (#78) that shows through the gaps between the slabs as grooves, so the grid reads without
/// drawn lines. Reads state from the <see cref="GameRoot"/> session. Holds no rules.
/// </para>
/// </summary>
public partial class FarmPlotView : Node3D
{
    private const string GameRootPath = "/root/GameRoot";

    /// <summary>Just above the meadow's ground (y 0), which is the only layer inside the plot, and below the slabs.</summary>
    private const float PlotBaseHeight = 0.002f;

    private readonly Dictionary<GridPosition, TileView> _views = new();

    private GameSession? _session;

    private Node3D? _batches;

    /// <summary>Every batch instance drawing a grass Tile, so a Tile that leaves grass is hidden without a rebuild.</summary>
    private readonly Dictionary<GridPosition, List<(MultiMesh Batch, int Index)>> _grassInstances = new();

    /// <summary>A transform scaled to nothing, which hides one batch instance.</summary>
    private static readonly Transform3D Hidden = new(Basis.Identity.Scaled(Vector3.Zero), Vector3.Zero);

    public override void _Ready()
    {
        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting; there is nothing to draw.
            return;
        }

        AddPlotBase();

        foreach (var tile in _session.Game.FarmPlot.Tiles.Where(tile => tile.State != TileState.Grass))
        {
            AddView(tile.Position);
        }

        RebuildGrassBatches();

        _session.TileChanged += OnTileChanged;
        _session.NightCompleted += OnNightCompleted;
    }

    /// <summary>The 72 x 36 m base plane, centred under the plot's corner tiles.</summary>
    private void AddPlotBase()
    {
        var first = FarmGrid.ToWorld(new GridPosition(0, 0));
        var last = FarmGrid.ToWorld(new GridPosition(FarmPlot.Width - 1, FarmPlot.Height - 1));
        var plotBase = new MeshInstance3D
        {
            Name = "PlotBase",
            Mesh = new PlaneMesh { Size = new Vector2(FarmPlot.Width, FarmPlot.Height) },
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
            RefreshView(position);
        }

        RebuildGrassBatches();
    }

    private void OnTileChanged(GridPosition position)
    {
        if (_views.ContainsKey(position))
        {
            RefreshView(position);
            return;
        }

        if (_session!.Game.FarmPlot[position].State == TileState.Grass)
        {
            return;
        }

        // The Tile has just left grass: it gets its own view now and its batch instances are hidden, so a till costs
        // no rebuild of the whole plot's batches; the next Night's rebuild drops them.
        AddView(position);
        HideGrassInstances(position);
    }

    private void HideGrassInstances(GridPosition position)
    {
        if (!_grassInstances.Remove(position, out var instances))
        {
            return;
        }

        foreach (var (batch, index) in instances)
        {
            batch.SetInstanceTransform(index, Hidden);
        }
    }

    /// <summary>Makes the <see cref="TileView"/> of the (non-grass) Tile at <paramref name="position"/> and draws it.</summary>
    private TileView AddView(GridPosition position)
    {
        var view = new TileView
        {
            Name = $"Tile_{position.X}_{position.Y}",
            Position = FarmGrid.ToWorld(position),
        };
        AddChild(view);
        _views[position] = view;
        RefreshView(position);
        return view;
    }

    private void RefreshView(GridPosition position) =>
        _views[position].Refresh(_session!.Game.FarmPlot[position], _session.GrowthStageAt(position), _session.Game.Date.Season);

    /// <summary>
    /// Replaces the grass batches with ones built from the plot's grass Tiles now, in the current Season: the slabs,
    /// each layout's detail and each native plant kind, each one <see cref="MultiMeshInstance3D"/>.
    /// </summary>
    private void RebuildGrassBatches()
    {
        if (_batches is not null)
        {
            RemoveChild(_batches);
            _batches.QueueFree();
        }

        _batches = new Node3D { Name = "GrassBatches" };
        AddChild(_batches);
        _grassInstances.Clear();

        var plot = _session!.Game.FarmPlot;
        var grass = SeasonGrass.For(_session.Game.Date.Season);
        var positions = GrassBatches.GrassPositions(plot);

        var slabLift = new Vector3(0f, TileView.SurfaceHeight / 2f, 0f);
        var slabs = AddBatch("GrassSlabs", TileView.SlabMesh, positions.Select(p => (p, new Transform3D(Basis.Identity, FarmGrid.ToWorld(p) + slabLift))));
        if (slabs is not null)
        {
            // A grass slab's role in the Season, as TileLook gives a grass Tile.
            ArtMaterials.Apply(slabs, grass.Ground);
            slabs.SetInstanceShaderParameter("wet", 0f);
        }

        var topLift = new Vector3(0f, TileView.SurfaceHeight, 0f);
        foreach (var (variant, batch) in GrassBatches.ByLayout(plot))
        {
            var tufts = AddBatch(
                $"Grass{variant}",
                GrassTileModels.MeshFor(variant, grass),
                batch.Select(entry => (entry.Position, new Transform3D(
                    new Basis(Vector3.Up, Mathf.DegToRad(90f * entry.QuarterTurns)),
                    FarmGrid.ToWorld(entry.Position) + topLift))));
            if (tufts is not null)
            {
                tufts.MaterialOverride = ArtMaterials.Shared;
            }
        }

        foreach (var (kind, batch) in GrassBatches.ByNativePlant(plot))
        {
            var plants = AddBatch(
                $"Native{kind}",
                TileView.NativePlantMesh(kind),
                batch.Select(p => (p, new Transform3D(
                        new Basis(Vector3.Up, Mathf.DegToRad(TileView.NativePlantYawDegrees(p))),
                        FarmGrid.ToWorld(p)))));
            if (plants is not null)
            {
                plants.MaterialOverride = ArtMaterials.Shared;
            }
        }
    }

    /// <summary>
    /// Adds one batch of <paramref name="mesh"/> at each Tile's transform, recording each instance against its Tile;
    /// none when there are no transforms.
    /// </summary>
    private MultiMeshInstance3D? AddBatch(string name, Mesh mesh, IEnumerable<(GridPosition Position, Transform3D Transform)> transforms)
    {
        var placed = transforms.ToList();
        if (placed.Count == 0)
        {
            return null;
        }

        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = placed.Count,
        };
        for (var i = 0; i < placed.Count; i++)
        {
            var (position, transform) = placed[i];
            multimesh.SetInstanceTransform(i, transform);
            if (!_grassInstances.TryGetValue(position, out var instances))
            {
                instances = [];
                _grassInstances[position] = instances;
            }

            instances.Add((multimesh, i));
        }

        var node = new MultiMeshInstance3D
        {
            Name = name,
            Multimesh = multimesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _batches!.AddChild(node);
        return node;
    }
}

#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Game.Ui;

namespace LongWayHome.Game.Art;

/// <summary>
/// The art sandbox (#75), the script of <c>res://art/sandbox/ArtSandbox.tscn</c>, opened from the title screen with
/// the <c>--art-sandbox</c> launch flag. Builds, on a 52 x 72 m ground slab (x ±26, z −22..50) (the <c>wall-grey</c> swatch, the
/// palette's closest neutral):
/// <list type="bullet">
/// <item>the palette rows at the back: one 0.8 m cube per <see cref="ArtPalette"/> swatch, in texture order (11 to a
/// row, 1.2 m apart), each with a billboard label giving its role and hex;</item>
/// <item>the three 1 x 1 m tile slabs at the front: <c>grass-lit</c>, <c>soil-tilled</c> and
/// <c>soil-watered</c>;</item>
/// <item>the <c>Crops</c> rows in front of the tile slabs (#90): one column per <see cref="CropTypeId"/> and one row per
/// <see cref="GrowthStage"/>, Seed nearest the camera, each model from <see cref="CropModels.MeshFor"/> on a
/// <c>soil-tilled</c> slab, for every (crop, stage);</item>
/// <item>the <c>Farmer</c> row beside the crops (#102): four farmers from <c>res://art/farmer/Farmer.tscn</c> on a
/// <c>grass-lit</c> slab, turned to yaw 0, 90, 180 and 270 and holding the hoe, the watering can, emberroot seeds and
/// the hoe, so the cap brim and the held item can be judged from every side;</item>
/// <item>the <c>Animations</c> row between the sample shapes and the tile slabs (#103): eleven farmers, one per
/// <see cref="FarmerClip"/>, each looping its clip with a billboard label naming it;</item>
/// <item>the four sample shapes (low-poly sphere, 6-sided trunk, house, cone tree), once on the left and once on the
/// right, with a label over each set naming the style it shows;</item>
/// <item>the <c>Farmhouses</c> at the front corners: the round farmhouse of the concept art
/// (<see cref="RoundFarmhouseArt"/>, built by <see cref="RoundFarmhouseModels"/>), run-down on the left and restored on
/// the right, each with a label. The restored house's warm window panes show only under the Night preset;</item>
/// <item>the <c>Trees</c> behind the palette (<see cref="TreeArt"/>, built by <see cref="NatureModels"/>): a row of
/// Twinbough stages in summer (sapling, full tree, old giant, stump) and behind it a row of full Twinboughs, one per
/// season, each with a label. The sap seams and the stump's rings glow under the Night preset;</item>
/// <item>the <c>Rocks</c> down the left edge (<see cref="RockArt"/>): a rock, a boulder, Rosk ore and Groth ore, each
/// with a label;</item>
/// <item>the <c>GatherTools</c> row at the front: four farmers holding the Phase 2 axe and pickaxe
/// (<see cref="HeldItemArt.PartsFor(GatherTool)"/>), one standing and one looping the hoe swing with each, a display model of
/// each tool at 2.5 times its size, side-on, at the row's ends (axe left, pickaxe right), and in front
/// of them the <c>Icons</c> row: the axe, pickaxe and material icons (<see cref="ItemIcons"/>) as labelled billboards;</item>
/// <item>the <c>Town</c> row furthest to the front: the general store (<see cref="GeneralStoreArt"/>, built by
/// <see cref="TownModels"/>) at each <see cref="StoreStage"/>, left to right, each labelled, with its rebuild sign
/// (<see cref="RebuildSignArt"/>) in front for the next stage: wood and stone delivered at Maggie's corner, nothing yet
/// once reopened, wood at the storeroom, and none once fully expanded. The store's lit windows and the signs' amber
/// outlines show only under the Night preset;</item>
/// <item>in front of it the carpenter (<see cref="CarpenterArt"/>) at each <see cref="CarpenterStage"/>, left to right,
/// each labelled, with a rebuild sign in front of the first three (wood in while empty, nothing once reopened, wood and
/// stone at the workshop). Its lit windows show only at night; the sap barrels glow too.</item>
/// </list>
/// Every mesh takes its colour from the shared palette material through <see cref="ArtMaterials.Apply"/> (#76).
/// Flat only or Stepped only sets the global shader uniform <c>art_stepped_shading</c>, so every mesh shows that
/// style just as the project-wide switch would. Both keeps the global at its project value and gives the sample
/// meshes a <c>style_override</c> instance parameter: the left set flat and the right set stepped unless
/// <see cref="SandboxLook.SidesSwapped"/>. The palette cubes, tile slabs and ground never get an override, so they
/// always follow the global. Leaving the sandbox restores the global to the project value.
/// Controls, all Input Map actions (REQ-13): <c>tool_next</c>/<c>tool_prev</c> step through the light presets,
/// <c>use_tool</c> swaps the sides, <c>interact</c> steps the style mode, <c>pause</c> returns to the title screen,
/// and <see cref="SandboxCamera"/> turns and zooms on the <c>camera_*</c> actions and slides left, right, forward and back
/// on the <c>move_*</c> actions (the left stick, the D-pad, WASD and the arrow keys). The top-left label shows
/// <see cref="SandboxLook.Label"/>; with <c>--show-fps</c> the <see cref="FpsCounter"/> shows top right. Reads and
/// changes no game state.
/// </summary>
public partial class ArtSandbox : Node3D
{
    private const string SteppedGlobal = "art_stepped_shading";

    private const string NightGlobal = "art_night";

    private const string StyleOverride = "style_override";

    private const string TitleScene = "res://scenes/Main.tscn";

    private const float GroundWidth = 52f;

    private const float GroundDepth = 72f;

    /// <summary>The ground's centre z: it runs from z −22 at the back to z 50 at the front.</summary>
    private const float GroundCentreZ = 14f;

    /// <summary>The z of the Carpenter row, in front of the store row.</summary>
    private const float CarpenterZ = 42.5f;

    /// <summary>The z of the Town row, furthest to the front.</summary>
    private const float TownZ = 29f;

    private const float CubeSize = 0.8f;

    private const float CubeSpacing = 1.2f;

    private const int CubesPerRow = 11;

    /// <summary>The z of the back palette row; each further row is <see cref="PaletteRowSpacing"/> in front.</summary>
    private const float PaletteBackRowZ = -7.4f;

    private const float PaletteRowSpacing = 1.9f;

    /// <summary>The z of the two sample-shape sets, clear of the front palette row's labels.</summary>
    private const float ShapesZ = 1.2f;

    /// <summary>The z of the tile slabs, in front of the sample shapes.</summary>
    private const float TilesZ = 4.5f;

    /// <summary>The z of the Ripe crop row, in front of the tile slabs; each earlier stage is a row nearer the camera.</summary>
    private const float CropsRipeZ = TilesZ + 1.2f;

    /// <summary>The spacing of the crop rows and columns.</summary>
    private const float CropSpacing = 1.1f;

    /// <summary>The tilled slab each crop stands on, the size of a farm Tile's slab.</summary>
    private const float CropSlabSize = 0.96f;

    private const float CropSlabHeight = 0.06f;

    private const string FarmerScene = "res://art/farmer/Farmer.tscn";

    /// <summary>The x of the Farmer row, just right of the crop columns.</summary>
    private const float FarmersX = 3.5f;

    /// <summary>The farmers' spacing along the row, from the Ripe crop row's z towards the camera.</summary>
    private const float FarmerSpacing = 1.1f;

    /// <summary>The z of the Animations row: between the sample shapes (z 1.2) and the tile slabs (z 4.5).</summary>
    private const float AnimationsZ = 2.9f;

    /// <summary>The x of the two farmhouses, run-down at −x and restored at +x, clear of the crops and the Farmer row.</summary>
    private const float FarmhousesX = 8.2f;

    /// <summary>The z of the two farmhouses, level with the crop rows and in front of the Animations row.</summary>
    private const float FarmhousesZ = 8f;

    /// <summary>The z of the Twinbough stages row, behind the palette.</summary>
    private const float TreeStagesZ = -12f;

    /// <summary>The z of the Twinbough seasons row, behind the stages row.</summary>
    private const float TreeSeasonsZ = -18.5f;

    /// <summary>The x of the rocks, down the left edge clear of the run-down farmhouse.</summary>
    private const float RocksX = -16f;

    /// <summary>The z of the gathering-tool farmers, in front of the crops and the farmhouses.</summary>
    private const float GatherToolsZ = 15.5f;

    /// <summary>How much bigger than in the hand the gathering-tool display models are.</summary>
    private const float ToolDisplayScale = 2.5f;

    /// <summary>The z of the icon row, in front of the gathering-tool farmers.</summary>
    private const float IconsZ = 18f;

    /// <summary>How long a one-shot clip rests on its last pose before the sandbox plays it again, in seconds.</summary>
    private const double OneShotRestartSeconds = 0.5;

    /// <summary>The point the camera orbits by default: in front of the origin, so the crop rows are in view.</summary>
    private static readonly Vector3 CameraCentre = new(0f, 0f, 2.5f);

    /// <summary>
    /// The light presets, ART_DIRECTION §3 and the issue's table. A reviewer may tune these values; nothing else
    /// depends on them. Sun elevation and azimuth are in degrees (azimuth 0 = light travelling north, clockwise from
    /// above). Morning uses the warm end of "cool white warming to #FFE2B8", with a cool ambient for the start.
    /// </summary>
    private static readonly Dictionary<LightPreset, LightSettings> Presets = new()
    {
        [LightPreset.Morning] = new(
            SunElevation: 15f, SunAzimuth: 300f, SunColour: new Color("#FFE2B8"), SunEnergy: 1.0f,
            SkyTop: new Color("#8EC5E8"), SkyHorizon: new Color("#CDE9DA"),
            Ambient: new Color("#BFD3E6"), AmbientEnergy: 0.45f, Night: false),
        [LightPreset.Day] = new(
            SunElevation: 55f, SunAzimuth: 330f, SunColour: Colors.White, SunEnergy: 1.1f,
            SkyTop: new Color("#8EC5E8"), SkyHorizon: new Color("#CDE9DA"),
            Ambient: new Color("#CDE9DA"), AmbientEnergy: 0.5f, Night: false),
        [LightPreset.Dusk] = new(
            SunElevation: 8f, SunAzimuth: 60f, SunColour: new Color("#F0A060"), SunEnergy: 1.0f,
            SkyTop: new Color("#F0A060"), SkyHorizon: new Color("#B85C86"),
            Ambient: new Color("#C88AA6"), AmbientEnergy: 0.6f, Night: false),
        [LightPreset.Night] = new(
            SunElevation: 0f, SunAzimuth: 0f, SunColour: Colors.Black, SunEnergy: 0f,
            SkyTop: new Color("#1B2142"), SkyHorizon: new Color("#2A3160"),
            Ambient: new Color("#6A78C0"), AmbientEnergy: 0.55f, Night: true),
    };

    /// <summary>Hearth, the big gold moon: from the west-north-west, fairly high, so its shadow falls east.</summary>
    private static readonly MoonSettings Hearth = new("Hearth", new Color("#F2D38A"), 0.35f, Elevation: 40f, Azimuth: 110f);

    /// <summary>Wisp, the small blue-white moon: from the east-south-east, lower, so its shadow falls west.</summary>
    private static readonly MoonSettings Wisp = new("Wisp", new Color("#CFE2FF"), 0.2f, Elevation: 30f, Azimuth: 290f);

    /// <summary>The sample-shape meshes, which get a <c>style_override</c> in Both mode.</summary>
    private readonly List<(GeometryInstance3D Node, Side Side)> _samples = new();

    private SandboxLook _look = SandboxLook.Default;

    private DirectionalLight3D _sun = null!;

    private DirectionalLight3D _hearth = null!;

    private DirectionalLight3D _wisp = null!;

    private Environment _environment = null!;

    private ProceduralSkyMaterial _sky = null!;

    private Label _label = null!;

    private Label3D _leftStyleLabel = null!;

    private Label3D _rightStyleLabel = null!;

    /// <summary>The meshes shown only under the Night preset: lit windows and the rebuild signs' outlines.</summary>
    private readonly List<GeometryInstance3D> _nightOnly = new();

    private bool _leaving;

    /// <summary>The project's <c>art_stepped_shading</c>, read once in <see cref="_Ready"/>.</summary>
    private bool _projectStepped;

    /// <summary>Which sample set a mesh belongs to.</summary>
    private enum Side
    {
        Left,
        Right,
    }

    /// <summary>The camera, public so a probe can read its view.</summary>
    public SandboxCamera Camera { get; private set; } = null!;

    /// <summary>What the sandbox is showing now.</summary>
    public SandboxLook Look => _look;

    public override void _Ready()
    {
        _projectStepped = ProjectStepped();
        BuildEnvironment();
        BuildGround();
        BuildPalette();
        BuildTiles();
        BuildCrops();
        BuildFarmers();
        BuildAnimations();
        BuildShapes();
        BuildFarmhouses();
        BuildTrees();
        BuildRocks();
        BuildGatherTools();
        BuildIcons();
        BuildTown();
        BuildLabel();

        Camera = new SandboxCamera { Name = "SandboxCamera", Current = true, Centre = CameraCentre };
        AddChild(Camera);

        Apply();
    }

    public override void _ExitTree()
    {
        // Flat only and Stepped only override the project-wide style; don't let that leak into the next scene.
        RenderingServer.GlobalShaderParameterSet(SteppedGlobal, _projectStepped);
        // The Night preset lights the crop glows (#94); the next scene starts without them.
        RenderingServer.GlobalShaderParameterSet(NightGlobal, 0f);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_leaving)
        {
            return;
        }

        if (Pressed(@event, "pause"))
        {
            // Handled first: changing scene takes this node out of the tree at once, so it has no viewport after.
            GetViewport().SetInputAsHandled();
            _leaving = true;
            var error = GetTree().ChangeSceneToFile(TitleScene);
            if (error != Error.Ok)
            {
                _leaving = false;
                GD.PushError($"Could not open {TitleScene}: {error}");
            }

            return;
        }

        if (Pressed(@event, "tool_next"))
        {
            Show(_look.NextPreset());
        }
        else if (Pressed(@event, "tool_prev"))
        {
            Show(_look.PreviousPreset());
        }
        else if (Pressed(@event, "use_tool"))
        {
            Show(_look.Swapped());
        }
        else if (Pressed(@event, "interact"))
        {
            Show(_look.NextMode());
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    private static bool Pressed(InputEvent @event, string action) => @event.IsActionPressed(action, allowEcho: false);

    private void Show(SandboxLook look)
    {
        _look = look;
        Apply();
    }

    private void Apply()
    {
        ApplyLight(Presets[_look.Preset]);
        RenderingServer.GlobalShaderParameterSet(NightGlobal, _look.ArtNight);
        foreach (var node in _nightOnly)
        {
            node.Visible = _look.Preset == LightPreset.Night;
        }

        var leftStepped = SteppedIn(_look.Mode, inBoth: _look.SidesSwapped);
        var rightStepped = SteppedIn(_look.Mode, inBoth: !_look.SidesSwapped);

        if (_look.Mode == StyleMode.Both)
        {
            // The left set flat and the right stepped unless swapped, through per-mesh overrides. The global stays at
            // the project value, which the palette cubes, tiles and ground follow.
            RenderingServer.GlobalShaderParameterSet(SteppedGlobal, _projectStepped);
            foreach (var (node, side) in _samples)
            {
                var stepped = side == Side.Left ? leftStepped : rightStepped;
                node.SetInstanceShaderParameter(StyleOverride, stepped ? 1 : 0);
            }
        }
        else
        {
            // Both sides show the same style here: set it project-wide and let every mesh follow it.
            RenderingServer.GlobalShaderParameterSet(SteppedGlobal, leftStepped);
            foreach (var (node, _) in _samples)
            {
                node.SetInstanceShaderParameter(StyleOverride, -1);
            }
        }

        _leftStyleLabel.Text = StyleName(leftStepped);
        _rightStyleLabel.Text = StyleName(rightStepped);
        _label.Text = _look.Label;
    }

    private static bool SteppedIn(StyleMode mode, bool inBoth) => mode switch
    {
        StyleMode.FlatOnly => false,
        StyleMode.SteppedOnly => true,
        _ => inBoth,
    };

    private static string StyleName(bool stepped) => stepped ? "Stepped" : "Flat";

    /// <summary>
    /// The project's <c>art_stepped_shading</c> value. Godot stores a shader global as a dictionary with
    /// <c>"type"</c> and <c>"value"</c>; anything else counts as flat.
    /// </summary>
    private static bool ProjectStepped()
    {
        var setting = ProjectSettings.GetSetting($"shader_globals/{SteppedGlobal}");
        if (setting.VariantType == Variant.Type.Dictionary
            && setting.AsGodotDictionary().TryGetValue("value", out var value)
            && value.VariantType == Variant.Type.Bool)
        {
            return value.AsBool();
        }

        return setting.VariantType == Variant.Type.Bool && setting.AsBool();
    }

    private void ApplyLight(LightSettings settings)
    {
        _sun.Visible = !settings.Night;
        _sun.LightColor = settings.SunColour;
        _sun.LightEnergy = settings.SunEnergy;
        _sun.Rotation = Direction(settings.SunElevation, settings.SunAzimuth);

        _hearth.Visible = settings.Night;
        _wisp.Visible = settings.Night;

        _sky.SkyTopColor = settings.SkyTop;
        _sky.SkyHorizonColor = settings.SkyHorizon;
        _sky.GroundHorizonColor = settings.SkyHorizon;
        // The camera looks down, so much of its background is below the horizon: keep that the horizon colour.
        _sky.GroundBottomColor = settings.SkyHorizon;

        _environment.AmbientLightColor = settings.Ambient;
        _environment.AmbientLightEnergy = settings.AmbientEnergy;
    }

    /// <summary>The rotation that points a directional light (which shines along its -Z) down at the elevation.</summary>
    private static Vector3 Direction(float elevationDegrees, float azimuthDegrees) =>
        new(Mathf.DegToRad(-elevationDegrees), Mathf.DegToRad(-azimuthDegrees), 0f);

    private void BuildEnvironment()
    {
        _sky = new ProceduralSkyMaterial();
        _environment = new Environment
        {
            BackgroundMode = Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = _sky },
            AmbientLightSource = Environment.AmbientSource.Color,
        };
        AddChild(new WorldEnvironment { Name = "WorldEnvironment", Environment = _environment });

        _sun = new DirectionalLight3D { Name = "Sun", ShadowEnabled = true, DirectionalShadowMaxDistance = 40f };
        AddChild(_sun);

        _hearth = Moon(Hearth);
        _wisp = Moon(Wisp);
    }

    private DirectionalLight3D Moon(MoonSettings moon)
    {
        var light = new DirectionalLight3D
        {
            Name = moon.Name,
            LightColor = moon.Colour,
            LightEnergy = moon.Energy,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 40f,
            Rotation = Direction(moon.Elevation, moon.Azimuth),
            Visible = false,
        };
        AddChild(light);
        return light;
    }

    private void BuildGround()
    {
        // wall-grey, the palette's closest neutral, so the tiles and samples are judged against no strong colour.
        AddMesh(
            "Ground",
            new BoxMesh { Size = new Vector3(GroundWidth, 0.1f, GroundDepth) },
            new Vector3(0f, -0.05f, GroundCentreZ),
            "wall-grey");
    }

    private void BuildPalette()
    {
        var palette = new Node3D { Name = "Palette" };
        AddChild(palette);

        var swatches = ArtPalette.Swatches;
        for (var i = 0; i < swatches.Count; i++)
        {
            var row = i / CubesPerRow;
            var column = i % CubesPerRow;
            var inRow = Mathf.Min(CubesPerRow, swatches.Count - row * CubesPerRow);
            var x = (column - (inRow - 1) / 2f) * CubeSpacing;
            // The first row at the back, each next one in front of it. Each label lies just in front of its cube, so
            // from the camera's raised view it shows under the cube rather than hidden behind it.
            var z = PaletteBackRowZ + row * PaletteRowSpacing;
            var swatch = swatches[i];

            AddMesh(
                $"Swatch{i + 1}",
                new BoxMesh { Size = new Vector3(CubeSize, CubeSize, CubeSize) },
                new Vector3(x, CubeSize / 2f, z),
                swatch.Role,
                parent: palette);

            palette.AddChild(new Label3D
            {
                Name = $"Swatch{i + 1}Label",
                Text = $"{swatch.Role}\n{swatch.Hex}",
                Position = new Vector3(x, 0.15f, z + 0.75f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 32,
                OutlineSize = 8,
                PixelSize = 0.0035f,
                Width = 300f,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
        }
    }

    private void BuildTiles()
    {
        var tiles = new Node3D { Name = "Tiles" };
        AddChild(tiles);
        (string Name, string Role)[] slabs = [("Grass", "grass-lit"), ("Tilled", "soil-tilled"), ("Watered", "soil-watered")];
        for (var i = 0; i < slabs.Length; i++)
        {
            AddMesh(
                $"{slabs[i].Name}Tile",
                new BoxMesh { Size = new Vector3(1f, 0.05f, 1f) },
                new Vector3((i - 1) * 1.1f, 0.025f, TilesZ),
                slabs[i].Role,
                parent: tiles);
        }
    }

    /// <summary>
    /// The crop models in front of the tile slabs: one column per Crop type in <see cref="CropTypeId"/> order and one
    /// row per <see cref="GrowthStage"/>, Seed nearest the camera and Ripe farthest, 1.1 m apart. Each stands on a
    /// 0.96 x 0.06 x 0.96 tilled slab and is drawn as on the farm: one mesh under the shared material.
    /// </summary>
    private void BuildCrops()
    {
        var crops = new Node3D { Name = "Crops" };
        AddChild(crops);
        var cropTypes = System.Enum.GetValues<CropTypeId>();
        var stages = System.Enum.GetValues<GrowthStage>();
        for (var column = 0; column < cropTypes.Length; column++)
        {
            var x = (column - ((cropTypes.Length - 1) / 2f)) * CropSpacing;
            for (var row = 0; row < stages.Length; row++)
            {
                // Stages run Seed to Ripe: Ripe (the last) farthest from the camera, Seed (the first) nearest.
                var z = CropsRipeZ + ((stages.Length - 1 - row) * CropSpacing);
                var name = $"{stages[row]}_{cropTypes[column]}";
                AddMesh(
                    $"{name}Slab",
                    new BoxMesh { Size = new Vector3(CropSlabSize, CropSlabHeight, CropSlabSize) },
                    new Vector3(x, CropSlabHeight / 2f, z),
                    "soil-tilled",
                    parent: crops);
                crops.AddChild(new MeshInstance3D
                {
                    Name = name,
                    Mesh = CropModels.MeshFor(cropTypes[column], stages[row]),
                    MaterialOverride = ArtMaterials.Shared,
                    Position = new Vector3(x, CropSlabHeight, z),
                });
            }
        }
    }

    /// <summary>
    /// The <c>Farmer</c> row: four farmers on one <c>grass-lit</c> slab beside the crops, one per crop row, at yaw 0,
    /// 90, 180 and 270 (facing the camera, to its right, away and to its left), holding the hoe, the watering can,
    /// emberroot seeds and the hoe. They are lit like everything else, so each light preset shows them too.
    /// </summary>
    private void BuildFarmers()
    {
        var farmers = new Node3D { Name = "Farmer" };
        AddChild(farmers);
        float[] yaws = [0f, 90f, 180f, 270f];
        HeldItem[] held =
        [
            HeldItem.Of(ToolId.Hoe),
            HeldItem.Of(ToolId.WateringCan),
            HeldItem.SeedOf(CropTypeId.Emberroot),
            HeldItem.Of(ToolId.Hoe),
        ];
        var length = yaws.Length * FarmerSpacing;
        AddMesh(
            "FarmerSlab",
            new BoxMesh { Size = new Vector3(1.2f, 0.05f, length) },
            new Vector3(FarmersX, 0.025f, CropsRipeZ + (length - FarmerSpacing) / 2f),
            "grass-lit",
            parent: farmers);

        var scene = GD.Load<PackedScene>(FarmerScene);
        for (var i = 0; i < yaws.Length; i++)
        {
            var farmer = scene.Instantiate<FarmerModel>();
            farmer.Name = $"Farmer{yaws[i]:0}";
            farmer.Position = new Vector3(FarmersX, 0.05f, CropsRipeZ + i * FarmerSpacing);
            farmer.RotationDegrees = new Vector3(0f, yaws[i], 0f);
            farmer.ShowHeld(held[i]);
            farmers.AddChild(farmer);
        }
    }

    /// <summary>
    /// The <c>Animations</c> row (#103): eleven farmers on one <c>grass-lit</c> slab between the sample shapes and the
    /// tile slabs, one per <see cref="FarmerClip"/> in order from the left, facing the camera. Each loops its clip (a
    /// one-shot plays again <see cref="OneShotRestartSeconds"/> after it ends), holds the item the clip uses (the hoe
    /// for the swing, the can for watering and the refill, emberroot seeds for planting, the hoe otherwise) and has a
    /// billboard label naming the clip.
    /// </summary>
    private void BuildAnimations()
    {
        var row = new Node3D { Name = "Animations" };
        AddChild(row);
        var clips = System.Enum.GetValues<FarmerClip>();
        var width = clips.Length * FarmerSpacing;
        AddMesh(
            "AnimationsSlab",
            new BoxMesh { Size = new Vector3(width, 0.05f, 1.2f) },
            new Vector3(0f, 0.025f, AnimationsZ),
            "grass-lit",
            parent: row);

        var scene = GD.Load<PackedScene>(FarmerScene);
        for (var i = 0; i < clips.Length; i++)
        {
            var clip = clips[i];
            var name = FarmerAnimations.ClipName(clip);
            var x = (i - (clips.Length - 1) / 2f) * FarmerSpacing;
            var farmer = scene.Instantiate<FarmerModel>();
            farmer.Name = $"Farmer_{name}";
            farmer.Position = new Vector3(x, 0.05f, AnimationsZ);
            farmer.ShowHeld(HeldFor(clip));
            row.AddChild(farmer);

            row.AddChild(new Label3D
            {
                Name = $"Label_{name}",
                Text = name,
                Position = new Vector3(x, 1.95f, AnimationsZ),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 32,
                OutlineSize = 8,
                PixelSize = 0.004f,
            });

            var player = farmer.GetNode<AnimationPlayer>("Animations");
            player.Play(name);
            if (!FarmerMoves.For(clip).Loops)
            {
                player.AnimationFinished += _ => GetTree().CreateTimer(OneShotRestartSeconds).Timeout += () =>
                {
                    if (IsInstanceValid(player) && player.IsInsideTree())
                    {
                        player.Play(name);
                        player.Seek(0, update: true);
                    }
                };
            }
        }
    }

    /// <summary>The item a clip's farmer holds in the Animations row.</summary>
    private static HeldItem HeldFor(FarmerClip clip) => clip switch
    {
        FarmerClip.Water or FarmerClip.Refill => HeldItem.Of(ToolId.WateringCan),
        FarmerClip.Plant => HeldItem.SeedOf(CropTypeId.Emberroot),
        _ => HeldItem.Of(ToolId.Hoe),
    };

    private void BuildShapes()
    {
        _leftStyleLabel = BuildShapeSet("LeftShapes", -1f, Side.Left);
        _rightStyleLabel = BuildShapeSet("RightShapes", 1f, Side.Right);
    }

    /// <summary>One set of the four shapes, on the left (<paramref name="sign"/> -1) or right (+1) of the centre.</summary>
    private Label3D BuildShapeSet(string name, float sign, Side side)
    {
        var set = new Node3D { Name = name, Position = new Vector3(sign * 4.2f, 0f, ShapesZ) };
        AddChild(set);

        AddMesh("Sphere", new SphereMesh { Radius = 0.5f, Height = 1f, RadialSegments = 8, Rings = 4 },
            new Vector3(-2.25f, 0.5f, 0f), "stone", side, set);

        AddMesh("Trunk", new CylinderMesh { TopRadius = 0.25f, BottomRadius = 0.3f, Height = 1.4f, RadialSegments = 6, Rings = 1 },
            new Vector3(-0.75f, 0.7f, 0f), "trunk", side, set);

        AddMesh("HouseWalls", new BoxMesh { Size = new Vector3(1f, 0.8f, 1f) },
            new Vector3(0.75f, 0.4f, 0f), "timber", side, set);
        AddMesh("HouseRoof", new PrismMesh { Size = new Vector3(1.2f, 0.55f, 1.1f) },
            new Vector3(0.75f, 0.8f + 0.275f, 0f), "ember", side, set);

        AddMesh("TreeTrunk", new CylinderMesh { TopRadius = 0.1f, BottomRadius = 0.12f, Height = 0.5f, RadialSegments = 6, Rings = 1 },
            new Vector3(2.25f, 0.25f, 0f), "trunk", side, set);
        AddMesh("TreeFoliage", new CylinderMesh { TopRadius = 0f, BottomRadius = 0.55f, Height = 1.4f, RadialSegments = 8, Rings = 1 },
            new Vector3(2.25f, 0.5f + 0.7f, 0f), "foliage", side, set);

        var label = new Label3D
        {
            Name = "StyleLabel",
            Position = new Vector3(0f, 2.4f, 0f),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 48,
            OutlineSize = 10,
            PixelSize = 0.006f,
        };
        set.AddChild(label);
        return label;
    }

    /// <summary>
    /// The round farmhouse of the concept art, run-down on the left and restored on the right, each facing the camera
    /// with a label over it, one merged mesh under the shared material that casts shadows. The restored house's night
    /// window panes are a second mesh that <see cref="Apply"/> shows only under the Night preset.
    /// </summary>
    private void BuildFarmhouses()
    {
        var farmhouses = new Node3D { Name = "Farmhouses" };
        AddChild(farmhouses);
        foreach (var (restored, sign) in new[] { (false, -1f), (true, 1f) })
        {
            var state = restored ? "Restored" : "RunDown";
            var position = new Vector3(sign * FarmhousesX, 0f, FarmhousesZ);
            farmhouses.AddChild(new MeshInstance3D
            {
                Name = $"RoundFarmhouse{state}",
                Mesh = RoundFarmhouseModels.MeshFor(restored),
                MaterialOverride = ArtMaterials.Shared,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
                Position = position,
            });
            farmhouses.AddChild(new Label3D
            {
                Name = $"RoundFarmhouse{state}Label",
                Text = restored ? "Round farmhouse · Restored" : "Round farmhouse · Run-down",
                Position = position + new Vector3(0f, 7.9f, 0f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 48,
                OutlineSize = 10,
                PixelSize = 0.006f,
            });
            if (restored)
            {
                var nightLights = new MeshInstance3D
                {
                    Name = "RoundFarmhouseRestoredNightLights",
                    Mesh = RoundFarmhouseModels.NightLights(),
                    MaterialOverride = ArtMaterials.Shared,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                    Position = position,
                    Visible = false,
                };
                farmhouses.AddChild(nightLights);
                _nightOnly.Add(nightLights);
            }
        }
    }

    /// <summary>
    /// The Twinbough rows behind the palette: the four stages in summer, then a full tree in each season, each one merged
    /// mesh under the shared material that casts shadows, with a label over it.
    /// </summary>
    private void BuildTrees()
    {
        var trees = new Node3D { Name = "Trees" };
        AddChild(trees);
        (TreeStage Stage, float X, string Label)[] stages =
        [
            (TreeStage.Sapling, -9f, "Twinbough · Sapling"),
            (TreeStage.Full, -4f, "Twinbough · Full tree"),
            (TreeStage.Giant, 2.5f, "Twinbough · Old giant"),
            (TreeStage.Stump, 8.5f, "Twinbough · Stump"),
        ];
        foreach (var (stage, x, label) in stages)
        {
            AddNature(trees, $"Twinbough{stage}", NatureModels.Twinbough(stage, Season.Summer), new Vector3(x, 0f, TreeStagesZ), label, LabelHeight(stage));
        }

        var seasons = System.Enum.GetValues<Season>();
        for (var i = 0; i < seasons.Length; i++)
        {
            var x = (i - (seasons.Length - 1) / 2f) * 7f;
            AddNature(trees, $"Twinbough{seasons[i]}", NatureModels.Twinbough(TreeStage.Full, seasons[i]), new Vector3(x, 0f, TreeSeasonsZ),
                $"Twinbough · {seasons[i]}", LabelHeight(TreeStage.Full));
        }
    }

    /// <summary>How high a tree's label floats: just over its top.</summary>
    private static float LabelHeight(TreeStage stage) => stage switch
    {
        TreeStage.Sapling => 2.2f,
        TreeStage.Giant => 6.9f,
        TreeStage.Stump => 1.6f,
        _ => 5.6f,
    };

    /// <summary>The rocks the pickaxe breaks, down the left edge, each with a label.</summary>
    private void BuildRocks()
    {
        var rocks = new Node3D { Name = "Rocks" };
        AddChild(rocks);
        (RockKind Kind, float Z, string Label, float LabelY)[] kinds =
        [
            (RockKind.Rock, -4f, "Rock", 1.1f),
            (RockKind.Boulder, -0.5f, "Boulder (iron pickaxe)", 1.8f),
            (RockKind.RoskOre, 3f, "Rosk ore", 1.1f),
            (RockKind.GrothOre, 6f, "Groth ore", 1.3f),
        ];
        foreach (var (kind, z, label, labelY) in kinds)
        {
            AddNature(rocks, $"Rock{kind}", NatureModels.Rock(kind), new Vector3(RocksX, 0f, z), label, labelY);
        }
    }

    /// <summary>
    /// Four farmers on a <c>grass-lit</c> slab at the front, facing the camera: standing with the axe, swinging the axe,
    /// standing with the pickaxe and swinging the pickaxe (the hoe swing, the nearest clip the game has), each labelled.
    /// </summary>
    private void BuildGatherTools()
    {
        var row = new Node3D { Name = "GatherTools" };
        AddChild(row);
        (GatherTool Tool, bool Swing, string Label)[] farmers =
        [
            (GatherTool.Axe, false, "Axe"),
            (GatherTool.Axe, true, "Axe · swing"),
            (GatherTool.Pickaxe, false, "Pickaxe"),
            (GatherTool.Pickaxe, true, "Pickaxe · swing"),
        ];
        AddMesh("GatherToolsSlab", new BoxMesh { Size = new Vector3(farmers.Length * 1.6f, 0.05f, 1.4f) },
            new Vector3(0f, 0.025f, GatherToolsZ), "grass-lit", parent: row);

        AddToolDisplay(row, GatherTool.Axe, -(farmers.Length * 0.8f + 1.0f));
        AddToolDisplay(row, GatherTool.Pickaxe, farmers.Length * 0.8f + 1.0f);

        var scene = GD.Load<PackedScene>(FarmerScene);
        var swing = FarmerAnimations.ClipName(FarmerClip.HoeSwing);
        for (var i = 0; i < farmers.Length; i++)
        {
            var (tool, swings, label) = farmers[i];
            var x = (i - (farmers.Length - 1) / 2f) * 1.6f;
            var farmer = scene.Instantiate<FarmerModel>();
            farmer.Name = $"Farmer_{tool}{(swings ? "Swing" : "")}";
            farmer.Position = new Vector3(x, 0.05f, GatherToolsZ);
            row.AddChild(farmer);
            farmer.ShowGatherTool(tool);

            row.AddChild(new Label3D
            {
                Name = $"Label_{farmer.Name}",
                Text = label,
                Position = new Vector3(x, 2.1f, GatherToolsZ),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 32,
                OutlineSize = 8,
                PixelSize = 0.004f,
            });

            if (!swings) continue;
            var player = farmer.GetNode<AnimationPlayer>("Animations");
            player.Play(swing);
            player.AnimationFinished += _ => GetTree().CreateTimer(OneShotRestartSeconds).Timeout += () =>
            {
                if (IsInstanceValid(player) && player.IsInsideTree())
                {
                    player.Play(swing);
                    player.Seek(0, update: true);
                }
            };
        }
    }

    /// <summary>
    /// A tool at <see cref="ToolDisplayScale"/> times its size, standing on its grip at the end of the gathering-tool row,
    /// turned side-on (its head pointing along +X) so the head's profile faces the camera.
    /// </summary>
    private static void AddToolDisplay(Node3D parent, GatherTool tool, float x)
    {
        // The grip (the hand's origin) sits 0.22 m above the handle's butt, so lift it that far, scaled.
        var position = new Vector3(x, 0.05f + 0.24f * ToolDisplayScale, GatherToolsZ);
        parent.AddChild(new MeshInstance3D
        {
            Name = $"Display_{tool}",
            Mesh = PartMeshes.Merge(HeldItemArt.PartsFor(tool)),
            MaterialOverride = ArtMaterials.Shared,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
            Position = position,
            RotationDegrees = new Vector3(0f, 90f, 0f),
            Scale = Vector3.One * ToolDisplayScale,
        });
        parent.AddChild(new Label3D
        {
            Name = $"DisplayLabel_{tool}",
            Text = $"{tool} ×{ToolDisplayScale:0.#}",
            Position = new Vector3(x, 2.4f, GatherToolsZ),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 32,
            OutlineSize = 8,
            PixelSize = 0.004f,
        });
    }

    /// <summary>
    /// The general store at each stage in a row at the front, left to right, each labelled and with the rebuild sign
    /// for its next stage out front. Lit windows and sign outlines join the night-only meshes.
    /// </summary>
    private void BuildTown()
    {
        var town = new Node3D { Name = "Town" };
        AddChild(town);
        (StoreStage Stage, float X, string Label, (bool Wood, bool Stone, bool Gold)? Sign)[] stores =
        [
            (StoreStage.MaggiesCorner, -20f, "General store · 1. Maggie's corner", (true, true, false)),
            (StoreStage.Reopened, -7f, "General store · 2. Reopened", (false, false, false)),
            (StoreStage.Storeroom, 6f, "General store · 3. Storeroom", (true, false, false)),
            (StoreStage.ExpandedShop, 18.5f, "General store · 4. Expanded shop", null),
        ];
        foreach (var (stage, x, label, sign) in stores)
        {
            var position = new Vector3(x, 0f, TownZ);
            town.AddChild(new MeshInstance3D
            {
                Name = $"Store{stage}",
                Mesh = TownModels.Store(stage),
                MaterialOverride = ArtMaterials.Shared,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
                Position = position,
            });
            AddNightOnly(town, $"Store{stage}NightLights", TownModels.StoreNightLights(stage), position);
            town.AddChild(new Label3D
            {
                Name = $"Store{stage}Label",
                Text = label,
                Position = position + new Vector3(0f, 8.2f, 0f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 48,
                OutlineSize = 10,
                PixelSize = 0.006f,
            });

            if (sign is not { } pips) continue;
            var signAt = position + new Vector3(2.6f, 0f, 5.4f);
            town.AddChild(new MeshInstance3D
            {
                Name = $"Store{stage}RebuildSign",
                Mesh = TownModels.RebuildSign(pips.Wood, pips.Stone, pips.Gold),
                MaterialOverride = ArtMaterials.Shared,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
                Position = signAt,
            });
            AddNightOnly(town, $"Store{stage}RebuildSignOutline", TownModels.RebuildSignOutline(), signAt);
        }

        (CarpenterStage Stage, float X, string Label, (bool Wood, bool Stone, bool Gold)? Sign)[] carpenters =
        [
            (CarpenterStage.Empty, -19.5f, "Carpenter · 0. Empty", (true, false, false)),
            (CarpenterStage.Reopened, -6.5f, "Carpenter · 1. Reopened", (false, false, false)),
            (CarpenterStage.Workshop, 6.5f, "Carpenter · 2. Workshop", (true, true, false)),
            (CarpenterStage.Builder, 19.5f, "Carpenter · 3. Builder", null),
        ];
        foreach (var (stage, x, label, sign) in carpenters)
        {
            var position = new Vector3(x, 0f, CarpenterZ);
            town.AddChild(new MeshInstance3D
            {
                Name = $"Carpenter{stage}",
                Mesh = TownModels.Carpenter(stage),
                MaterialOverride = ArtMaterials.Shared,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
                Position = position,
            });
            AddNightOnly(town, $"Carpenter{stage}NightLights", TownModels.CarpenterNightLights(stage), position);
            town.AddChild(new Label3D
            {
                Name = $"Carpenter{stage}Label",
                Text = label,
                Position = position + new Vector3(0f, 8.8f, 0f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 48,
                OutlineSize = 10,
                PixelSize = 0.006f,
            });

            if (sign is not { } pips) continue;
            var signAt = position + new Vector3(-1.9f, 0f, 4.6f);
            town.AddChild(new MeshInstance3D
            {
                Name = $"Carpenter{stage}RebuildSign",
                Mesh = TownModels.RebuildSign(pips.Wood, pips.Stone, pips.Gold),
                MaterialOverride = ArtMaterials.Shared,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
                Position = signAt,
            });
            AddNightOnly(town, $"Carpenter{stage}RebuildSignOutline", TownModels.RebuildSignOutline(), signAt);
        }
    }

    /// <summary>A mesh under the shared material that shows only under the Night preset, casting no shadow.</summary>
    private void AddNightOnly(Node3D parent, string name, Mesh mesh, Vector3 position)
    {
        var node = new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            MaterialOverride = ArtMaterials.Shared,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Position = position,
            Visible = false,
        };
        parent.AddChild(node);
        _nightOnly.Add(node);
    }

    /// <summary>The axe, pickaxe and material icons in a row of billboards at the front, each with its name under it.</summary>
    private void BuildIcons()
    {
        var row = new Node3D { Name = "Icons" };
        AddChild(row);
        var icons = new List<(string Path, string Label)>
        {
            (ItemIcons.ToolIcon(GatherTool.Axe), "Axe"),
            (ItemIcons.ToolIcon(GatherTool.Pickaxe), "Pickaxe"),
        };
        foreach (var material in System.Enum.GetValues<MaterialKind>())
        {
            icons.Add((ItemIcons.MaterialIcon(material), MaterialLabel(material)));
        }

        for (var i = 0; i < icons.Count; i++)
        {
            var x = (i - (icons.Count - 1) / 2f) * 1.5f;
            var (path, label) = icons[i];
            row.AddChild(new Sprite3D
            {
                Name = $"Icon_{label.Replace(' ', '_')}",
                Texture = GD.Load<Texture2D>(path),
                Position = new Vector3(x, 1.0f, IconsZ),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                PixelSize = 0.016f,
                Shaded = false,
            });
            row.AddChild(new Label3D
            {
                Name = $"IconLabel_{label.Replace(' ', '_')}",
                Text = label,
                Position = new Vector3(x, 0.25f, IconsZ),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 32,
                OutlineSize = 8,
                PixelSize = 0.004f,
            });
        }
    }

    private static string MaterialLabel(MaterialKind material) => material switch
    {
        MaterialKind.RoskOre => "Rosk ore",
        MaterialKind.GrothOre => "Groth ore",
        MaterialKind.SealedPlank => "Sealed planks",
        MaterialKind.Plank => "Planks",
        _ => material.ToString(),
    };

    /// <summary>One merged tree or rock mesh under the shared material, casting shadows, with a billboard label over it.</summary>
    private static void AddNature(Node3D parent, string name, Mesh mesh, Vector3 position, string label, float labelHeight)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            MaterialOverride = ArtMaterials.Shared,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
            Position = position,
        });
        parent.AddChild(new Label3D
        {
            Name = $"{name}Label",
            Text = label,
            Position = position + new Vector3(0f, labelHeight, 0f),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 40,
            OutlineSize = 8,
            PixelSize = 0.006f,
        });
    }

    private void BuildLabel()
    {
        var layer = new CanvasLayer { Name = "Ui" };
        AddChild(layer);

        _label = new Label
        {
            Name = "LookLabel",
            Position = new Vector2(16f, 12f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _label.AddThemeColorOverride("font_outline_color", Colors.Black);
        _label.AddThemeConstantOverride("outline_size", 4);
        _label.AddThemeFontSizeOverride("font_size", 22);
        layer.AddChild(_label);

        if (DisplayProfile.ShowFps(OS.GetCmdlineUserArgs()))
        {
            var fps = new FpsCounter
            {
                AnchorLeft = 1f,
                AnchorRight = 1f,
                OffsetLeft = -300f,
                OffsetRight = -16f,
                OffsetTop = 12f,
                OffsetBottom = 44f,
            };
            layer.AddChild(fps);
        }
    }

    /// <summary>
    /// Adds a mesh coloured with <paramref name="role"/>'s swatch. With a <paramref name="side"/> it is a sample mesh,
    /// which gets a style override in Both mode; without one it always follows the global style.
    /// </summary>
    private MeshInstance3D AddMesh(string name, Mesh mesh, Vector3 position, string role, Side? side = null, Node? parent = null)
    {
        var instance = new MeshInstance3D { Name = name, Mesh = mesh, Position = position };
        ArtMaterials.Apply(instance, role);
        if (side is { } sampleSide)
        {
            _samples.Add((instance, sampleSide));
        }

        (parent ?? this).AddChild(instance);
        return instance;
    }

    private readonly record struct LightSettings(
        float SunElevation,
        float SunAzimuth,
        Color SunColour,
        float SunEnergy,
        Color SkyTop,
        Color SkyHorizon,
        Color Ambient,
        float AmbientEnergy,
        bool Night);

    private readonly record struct MoonSettings(string Name, Color Colour, float Energy, float Elevation, float Azimuth);
}

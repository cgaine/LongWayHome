#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Game.Ui;

namespace LongWayHome.Game.Art;

/// <summary>
/// The art sandbox (#75), the script of <c>res://art/sandbox/ArtSandbox.tscn</c>, opened from the title screen with
/// the <c>--art-sandbox</c> launch flag. Builds, on a 16 x 16 m ground slab (the <c>wall-grey</c> swatch, the
/// palette's closest neutral):
/// <list type="bullet">
/// <item>the palette rows at the back: one 0.8 m cube per <see cref="ArtPalette"/> swatch, in texture order (11 to a
/// row, 1.2 m apart), each with a billboard label giving its role and hex;</item>
/// <item>the three 1 x 1 m tile slabs at the front: <c>grass-lit</c>, <c>soil-tilled</c> and
/// <c>soil-watered</c>;</item>
/// <item>the four sample shapes (low-poly sphere, 6-sided trunk, house, cone tree), once on the left and once on the
/// right, with a label over each set naming the style it shows.</item>
/// </list>
/// Every mesh takes its colour from the shared palette material through <see cref="ArtMaterials.Apply"/> (#76).
/// Flat only or Stepped only sets the global shader uniform <c>art_stepped_shading</c>, so every mesh shows that
/// style just as the project-wide switch would. Both keeps the global at its project value and gives the sample
/// meshes a <c>style_override</c> instance parameter: the left set flat and the right set stepped unless
/// <see cref="SandboxLook.SidesSwapped"/>. The palette cubes, tile slabs and ground never get an override, so they
/// always follow the global. Leaving the sandbox restores the global to the project value.
/// Controls, all Input Map actions (REQ-13): <c>tool_next</c>/<c>tool_prev</c> step through the light presets,
/// <c>use_tool</c> swaps the sides, <c>interact</c> steps the style mode, <c>pause</c> returns to the title screen,
/// and <see cref="SandboxCamera"/> turns and zooms on the <c>camera_*</c> actions. The top-left label shows
/// <see cref="SandboxLook.Label"/>; with <c>--show-fps</c> the <see cref="FpsCounter"/> shows top right. Reads and
/// changes no game state.
/// </summary>
public partial class ArtSandbox : Node3D
{
    private const string SteppedGlobal = "art_stepped_shading";

    private const string StyleOverride = "style_override";

    private const string TitleScene = "res://scenes/Main.tscn";

    private const float GroundSize = 16f;

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
        BuildShapes();
        BuildLabel();

        Camera = new SandboxCamera { Name = "SandboxCamera", Current = true };
        AddChild(Camera);

        Apply();
    }

    public override void _ExitTree()
    {
        // Flat only and Stepped only override the project-wide style; don't let that leak into the next scene.
        RenderingServer.GlobalShaderParameterSet(SteppedGlobal, _projectStepped);
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
            new BoxMesh { Size = new Vector3(GroundSize, 0.1f, GroundSize) },
            new Vector3(0f, -0.05f, 0f),
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

#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.World;

/// <summary>
/// Shows the Day's weather on the farm. While <see cref="Domain.Games.Game.Weather"/> is <see cref="Weather.Rain"/>
/// its <see cref="GPUParticles3D"/> child emits rain over the farm, the <see cref="Light"/> is dimmed to
/// <see cref="RainLightFactor"/> of its scene energy and the sky of the <see cref="Environment"/> is greyed; on a
/// clear Day the scene's own values are restored. Re-checked on start and on <see cref="GameSession.NightCompleted"/>,
/// the only time the weather changes (BR-G4). Reads the weather and holds no rules.
/// </summary>
public partial class RainView : Node3D
{
    private const string GameRootPath = "/root/GameRoot";

    /// <summary>How far the sky colours move towards <see cref="RainSkyGrey"/> on a rain Day.</summary>
    private const float SkyGreyAmount = 0.65f;

    private static readonly Color RainSkyGrey = new(0.5f, 0.52f, 0.55f);

    private GameSession? _session;

    private GpuParticles3D? _particles;

    private float _clearLightEnergy;

    private ProceduralSkyMaterial? _sky;

    private Color _clearSkyTop;

    private Color _clearSkyHorizon;

    private Color _clearGroundHorizon;

    /// <summary>The sun, dimmed on rain Days.</summary>
    [Export]
    public DirectionalLight3D? Light { get; set; }

    /// <summary>The scene's environment, whose procedural sky is greyed on rain Days.</summary>
    [Export]
    public WorldEnvironment? Environment { get; set; }

    /// <summary>The share of the light's scene energy it keeps on a rain Day.</summary>
    [Export]
    public float RainLightFactor { get; set; } = 0.6f;

    public override void _Ready()
    {
        _particles = GetNodeOrNull<GpuParticles3D>("Rain");
        if (Light is not null)
        {
            _clearLightEnergy = Light.LightEnergy;
        }

        _sky = Environment?.Environment?.Sky?.SkyMaterial as ProceduralSkyMaterial;
        if (_sky is not null)
        {
            _clearSkyTop = _sky.SkyTopColor;
            _clearSkyHorizon = _sky.SkyHorizonColor;
            _clearGroundHorizon = _sky.GroundHorizonColor;
        }

        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting.
            return;
        }

        _session.NightCompleted += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.NightCompleted -= Refresh;
        }
    }

    private void Refresh()
    {
        var raining = _session!.Game.Weather == Weather.Rain;

        if (_particles is not null)
        {
            _particles.Emitting = raining;
            _particles.Visible = raining;
        }

        if (Light is not null)
        {
            Light.LightEnergy = raining ? _clearLightEnergy * RainLightFactor : _clearLightEnergy;
        }

        if (_sky is not null)
        {
            _sky.SkyTopColor = raining ? _clearSkyTop.Lerp(RainSkyGrey, SkyGreyAmount) : _clearSkyTop;
            _sky.SkyHorizonColor = raining ? _clearSkyHorizon.Lerp(RainSkyGrey, SkyGreyAmount) : _clearSkyHorizon;
            _sky.GroundHorizonColor =
                raining ? _clearGroundHorizon.Lerp(RainSkyGrey, SkyGreyAmount) : _clearGroundHorizon;
        }
    }
}

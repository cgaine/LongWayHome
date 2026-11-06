#nullable enable

using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The Use prompt, bottom centre of the HUD: each frame asks <see cref="GameSession.PreviewUse"/> what Use would do on
/// the targeted tile with the held item and shows <see cref="UsePromptText"/>'s words for it. An action shows the
/// <c>use_tool</c> glyph of the last-used device then the text, in full white ("[Space] Till"); a rejection shows only
/// the reason, dimmed ("Already watered"); nothing to say hides it (REQ-12). Holds no rules: the decision is
/// <see cref="GameSession.PreviewUse"/>'s, so the prompt and the action can't disagree.
/// </summary>
public partial class UsePrompt : Label
{
    /// <summary>Opacity of a rejection reason, so it reads as "can't" next to a full-white action.</summary>
    public const float RejectionAlpha = 0.6f;

    private GameSession? _session;

    private LastInputDevice? _device;

    private ToolTargeting? _targeting;

    private HeldItemSelection? _selection;

    public override void _Ready()
    {
        Visible = false;
        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetProcess(false);
            return;
        }

        _device = GetNodeOrNull<LastInputDevice>(LastInputDevice.Path);
    }

    public override void _Process(double delta)
    {
        if (_targeting is null || _selection is null)
        {
            var player = GetTree().GetFirstNodeInGroup(PlayerController.Group);
            _targeting = player?.GetChildren().OfType<ToolTargeting>().FirstOrDefault();
            _selection = player?.GetChildren().OfType<HeldItemSelection>().FirstOrDefault();
            if (_targeting is null || _selection is null)
            {
                Visible = false;
                return;
            }
        }

        var target = _targeting.Target;
        var held = _selection.Held;
        var preview = _session!.PreviewUse(target, held);
        var planting = target is { } position ? _session.Game.FarmPlot[position].Planting : null;

        if (UsePromptText.For(preview, held, planting, _session.Data.CropTypes) is not { } prompt)
        {
            Visible = false;
            return;
        }

        var text = prompt.IsAction ? $"{Glyph()} {prompt.Text}" : prompt.Text;
        if (Text != text)
        {
            // Only on change, so an unchanged prompt doesn't re-shape its text every frame.
            Text = text;
        }

        Modulate = new Color(1f, 1f, 1f, prompt.IsAction ? 1f : RejectionAlpha);
        Visible = true;
    }

    private string Glyph() =>
        _device?.GlyphFor("use_tool") ?? LastInputDevice.GlyphFor("use_tool", InputDevice.KeyboardMouse);
}

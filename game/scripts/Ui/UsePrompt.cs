#nullable enable

using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The Use prompt, bottom centre of the HUD: each frame asks <see cref="GameSession.PreviewUse"/> what Use would do on
/// the targeted tile from the held hotbar position and shows <see cref="UsePromptText"/>'s words for it. An action shows the
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

    /// <summary>What the prompt was last drawn from, or null to force a redraw.</summary>
    private (UsePreview Preview, HeldItem? Held, CropTypeId? Crop, InputDeviceKind Device)? _last;

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
        if (_targeting is null || _selection is null || !IsInstanceValid(_targeting) || !IsInstanceValid(_selection))
        {
            _last = null;
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
        var held = _session!.HeldItemAt(_selection.Position);
        var preview = _session.PreviewUse(target, _selection.Position);
        var planting = _session.PlantingAt(target);

        // Only redraw when what the prompt is drawn from changes, so an unchanged prompt costs no allocation per frame.
        var device = LastInputDevice.DeviceOf(_device);
        var inputs = (preview, held, planting?.CropTypeId, device);
        if (_last == inputs)
        {
            return;
        }

        _last = inputs;
        if (UsePromptText.For(preview, held, planting, _session.Data.CropTypes) is not { } prompt)
        {
            Visible = false;
            return;
        }

        Text = prompt.IsAction ? $"{LastInputDevice.GlyphFor("use_tool", device)} {prompt.Text}" : prompt.Text;
        Modulate = new Color(1f, 1f, 1f, prompt.IsAction ? 1f : RejectionAlpha);
        Visible = true;
    }
}

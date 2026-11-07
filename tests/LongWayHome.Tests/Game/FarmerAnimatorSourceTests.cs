using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files and the blueprint from the repository (not the test output, which may hold stale
/// copies) and checks the Godot glue of #103 that xUnit can't run: <c>FarmerAnimator</c> plays the locomotion and the
/// one-shots, the hooks play a clip only on a successful result, the day-end clips never move the end of the Day, the
/// scenes carry the <c>AnimationPlayer</c> and the animator, and the sandbox shows an <c>Animations</c> row.
/// </summary>
public class FarmerAnimatorSourceTests
{
    private static readonly string[] AnimatorPath = ["game", "scripts", "Player", "FarmerAnimator.cs"];

    private static readonly string[] SnakeCaseClipNames =
    [
        "idle", "walk", "run", "hoe_swing", "water", "plant", "harvest", "refill", "ship", "sleep", "pass_out",
    ];

    // ---- AC5: locomotion ----

    [Fact]
    public void AC5_farmer_animator_exists_as_a_class()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Matches(@"class\s+FarmerAnimator\b", source);
    }

    [Fact]
    public void AC5_farmer_animator_plays_locomotion_from_the_velocity_and_the_run_action()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Contains("FarmerAnimationState.Locomotion(", source, StringComparison.Ordinal);
        Assert.Matches(@"Input\.IsActionPressed\(\s*""run""\s*\)", source);
        Assert.Matches(@"override\s+void\s+_PhysicsProcess\s*\(", source);
    }

    [Fact]
    public void AC5_farmer_animator_scales_walk_playback_and_blends_0_15()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Contains("FarmerAnimationState.WalkPlaybackScale(", source, StringComparison.Ordinal);
        Assert.Matches(@"(?<![\d.])0\.15f?\b", source);
    }

    [Fact]
    public void AC5_farmer_animator_finds_the_farmers_animation_player()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Contains("\"../Model/Farmer/Animations\"", source, StringComparison.Ordinal);
        Assert.Contains("AnimationPlayer", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_farmer_animator_exposes_busy_locks_movement_and_play_one_shot()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Matches(@"public\s+bool\s+Busy\b", source);
        Assert.Matches(@"public\s+bool\s+LocksMovement\b", source);
        Assert.Matches(@"public\s+void\s+PlayOneShot\s*\(\s*FarmerClip\s+\w+\s*,\s*Action\?\s+\w+\s*=\s*null\s*\)", source);
    }

    [Fact]
    public void AC5_farmer_animator_blends_one_shots_in_over_0_08()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Matches(@"(?<![\d.])0\.08f?\b", source);
    }

    // ---- AC6: Use plays its clip only on success ----

    [Fact]
    public void AC6_tool_use_previews_the_action_before_using()
    {
        var source = ReadRepoFile("game", "scripts", "Player", "ToolUse.cs");

        var preview = source.IndexOf("PreviewUse(", StringComparison.Ordinal);
        var use = Regex.Match(source, @"\.Use\s*\(");
        Assert.True(preview >= 0, "ToolUse.cs never calls PreviewUse(");
        Assert.True(use.Success, "ToolUse.cs never calls .Use(");
        Assert.True(preview < use.Index, "ToolUse.cs calls .Use( before PreviewUse(");
    }

    [Fact]
    public void AC6_tool_use_plays_the_use_clip_only_when_the_result_is_ok()
    {
        var source = ReadRepoFile("game", "scripts", "Player", "ToolUse.cs");

        AssertPlaysAfterIsOk(source, "FarmerAnimationState.ForUse(", "ToolUse.cs");
        Assert.Contains("PlayOneShot(", source, StringComparison.Ordinal);
    }

    // ---- AC7: refill and ship ----

    [Fact]
    public void AC7_pond_refill_plays_refill_on_success()
    {
        var source = ReadRepoFile("game", "scripts", "World", "PondRefill.cs");

        AssertPlaysAfterIsOk(source, "FarmerClip.Refill", "PondRefill.cs");
        Assert.Matches(@"PlayOneShot\s*\(\s*FarmerClip\.Refill\b", source);
    }

    [Fact]
    public void AC7_pond_refill_finds_the_animator_through_the_player_group()
    {
        var source = ReadRepoFile("game", "scripts", "World", "PondRefill.cs");

        Assert.Contains("PlayerController.Group", source, StringComparison.Ordinal);
        Assert.Contains("FarmerAnimator", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC7_bin_ship_plays_ship_on_success()
    {
        var source = ReadRepoFile("game", "scripts", "World", "BinShip.cs");

        AssertPlaysAfterIsOk(source, "FarmerClip.Ship", "BinShip.cs");
        Assert.Matches(@"PlayOneShot\s*\(\s*FarmerClip\.Ship\b", source);
    }

    [Fact]
    public void AC7_bin_ship_finds_the_animator_through_the_player_group()
    {
        var source = ReadRepoFile("game", "scripts", "World", "BinShip.cs");

        Assert.Contains("PlayerController.Group", source, StringComparison.Ordinal);
        Assert.Contains("FarmerAnimator", source, StringComparison.Ordinal);
    }

    // ---- AC8 and AC10: sleep at the door ----

    [Fact]
    public void AC8_door_sleep_plays_the_clip_only_when_it_ends_before_0200()
    {
        var source = ReadRepoFile("game", "scripts", "World", "DoorSleep.cs");

        Assert.Contains("FarmerAnimationState.SleepPlaysClip(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC8_door_sleep_plays_sleep_then_sleeps_when_it_finishes()
    {
        var source = ReadRepoFile("game", "scripts", "World", "DoorSleep.cs");

        var call = Regex.Match(source, @"PlayOneShot\s*\(\s*FarmerClip\.Sleep\s*,(?<done>[\s\S]*?)\)\s*;");
        Assert.True(call.Success, "DoorSleep.cs never calls PlayOneShot(FarmerClip.Sleep, <done>)");
        Assert.Contains("Sleep", call.Groups["done"].Value, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_door_sleep_keeps_a_direct_sleep_without_a_clip()
    {
        var source = ReadRepoFile("game", "scripts", "World", "DoorSleep.cs");

        Assert.Matches(@"_session!?\s*\.Sleep\s*\(\s*\)", source);
        Assert.Contains("FarmerAnimationState.SleepPlaysClip(", source, StringComparison.Ordinal);
    }

    // ---- AC9: pass out at 02:00 ----

    [Fact]
    public void AC9_farmer_animator_plays_pass_out_when_it_is_due()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Contains("FarmerAnimationState.PassOutDue(", source, StringComparison.Ordinal);
        Assert.Contains("FarmerClip.PassOut", source, StringComparison.Ordinal);
    }

    // ---- AC11 and BR-G2: the clips pause with the clock and never drive it ----

    [Fact]
    public void AC11_farmer_animator_is_pausable()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Contains("ProcessModeEnum.Pausable", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BR_G2_farmer_animator_is_pausable_and_never_advances_the_clock()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Contains("ProcessModeEnum.Pausable", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AdvanceClock", source, StringComparison.Ordinal);
    }

    // ---- AC12: wake at the door, free to move and use ----

    [Fact]
    public void AC12_farmer_animator_subscribes_to_night_completed_and_plays_idle()
    {
        var source = ReadRepoFile(AnimatorPath);

        Assert.Matches(@"\.NightCompleted\s*\+=", source);
        Assert.Contains("FarmerClip.Idle", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC12_farmer_animator_unsubscribes_on_exit_tree()
    {
        var source = ReadRepoFile(AnimatorPath);

        var exit = Regex.Match(source, @"override\s+void\s+_ExitTree\s*\(\s*\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(exit.Success, "FarmerAnimator has no _ExitTree");
        Assert.Matches(@"\.NightCompleted\s*-=", exit.Groups["body"].Value);
    }

    [Fact]
    public void AC12_player_controller_stops_while_the_animator_locks_movement()
    {
        var source = ReadRepoFile("game", "scripts", "Player", "PlayerController.cs");

        Assert.Contains("LocksMovement", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC12_tool_use_checks_the_movement_lock_before_using()
    {
        var source = ReadRepoFile("game", "scripts", "Player", "ToolUse.cs");

        var locks = source.IndexOf("LocksMovement", StringComparison.Ordinal);
        var use = Regex.Match(source, @"\.Use\s*\(");
        Assert.True(locks >= 0, "ToolUse.cs never reads LocksMovement");
        Assert.True(use.Success, "ToolUse.cs never calls .Use(");
        Assert.True(locks < use.Index, "ToolUse.cs reads LocksMovement only after calling .Use(");
    }

    // ---- Scenes and the builder ----

    [Fact]
    public void Scenes_farmer_scene_has_an_animation_player_named_animations()
    {
        var scene = ReadRepoFile("game", "art", "farmer", "Farmer.tscn");

        Assert.Matches(@"\[node\s+name=""Animations""\s+type=""AnimationPlayer""", scene);
    }

    [Fact]
    public void Scenes_player_scene_has_a_farmer_animator_node()
    {
        var scene = ReadRepoFile("game", "scenes", "Player.tscn");

        var resource = Regex.Match(
            scene,
            @"\[ext_resource[^\]]*type=""Script""[^\]]*path=""res://scripts/Player/FarmerAnimator\.cs""[^\]]*id=""([^""]+)""");
        Assert.True(resource.Success, "Player.tscn has no Script ext_resource for res://scripts/Player/FarmerAnimator.cs");
        var id = Regex.Escape(resource.Groups[1].Value);

        Assert.Matches(
            @"\[node\s+name=""FarmerAnimator""[^\]]*\][^\[]*script\s*=\s*ExtResource\(\s*""" + id + @"""\s*\)",
            scene);
    }

    [Fact]
    public void Scenes_farmer_animations_builds_a_library_with_looping_clips()
    {
        var source = ReadRepoFile("game", "art", "farmer", "FarmerAnimations.cs");

        Assert.Matches(@"public\s+static\s+AnimationLibrary\s+Build\s*\(\s*\)", source);
        Assert.Matches(@"LoopModeEnum\.Linear|LoopMode\.Linear", source);
        Assert.Contains("FarmerMoves.For(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Scenes_farmer_animations_names_all_eleven_clips_in_snake_case()
    {
        var source = ReadRepoFile("game", "art", "farmer", "FarmerAnimations.cs");

        foreach (var name in SnakeCaseClipNames)
        {
            Assert.True(source.Contains($"\"{name}\"", StringComparison.Ordinal), $"FarmerAnimations.cs never names the clip \"{name}\"");
        }
    }

    [Fact]
    public void Scenes_farmer_installs_the_built_library()
    {
        var text = ReadRepoFile("game", "art", "farmer", "Farmer.tscn") + "\n" + ReadRepoFile("game", "art", "farmer", "FarmerModel.cs");

        Assert.Contains("FarmerAnimations.Build()", text, StringComparison.Ordinal);
    }

    // ---- AC13: the sandbox's Animations row ----

    [Fact]
    public void AC13_sandbox_builds_an_animations_row_from_every_clip()
    {
        var source = ReadRepoFile("game", "art", "sandbox", "ArtSandbox.cs");

        Assert.Contains("\"Animations\"", source, StringComparison.Ordinal);
        Assert.Matches(@"Enum\.GetValues\s*(?:<\s*FarmerClip\s*>|\(\s*typeof\s*\(\s*FarmerClip\s*\)\s*\))", source);
    }

    [Fact]
    public void AC13_sandbox_labels_each_clip_with_a_billboard()
    {
        var source = ReadRepoFile("game", "art", "sandbox", "ArtSandbox.cs");

        Assert.Contains("Label3D", source, StringComparison.Ordinal);
        Assert.Contains("Billboard", source, StringComparison.Ordinal);
        Assert.Contains("FarmerClip", source, StringComparison.Ordinal);
    }

    // ---- Blueprint ----

    [Fact]
    public void Docs_blueprint_names_the_farmer_animations()
    {
        var blueprint = ReadRepoFile("docs", "requirements", "technical-blueprint.md");

        Assert.Contains("FarmerAnimations.cs", blueprint, StringComparison.Ordinal);
    }

    // ---- helpers ----

    /// <summary><paramref name="play"/> appears, and some <c>IsOk</c> check comes before it.</summary>
    private static void AssertPlaysAfterIsOk(string source, string play, string file)
    {
        var playAt = source.IndexOf(play, StringComparison.Ordinal);
        var okAt = source.IndexOf("IsOk", StringComparison.Ordinal);
        Assert.True(playAt >= 0, $"{file} never mentions {play}");
        Assert.True(okAt >= 0, $"{file} never checks IsOk");
        Assert.True(okAt < playAt, $"{file} mentions {play} before any IsOk check");
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"{string.Join('/', parts)} not found");
        return File.ReadAllText(path);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LongWayHome.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"no folder holding LongWayHome.sln above {AppContext.BaseDirectory}");
        return dir!.FullName;
    }
}

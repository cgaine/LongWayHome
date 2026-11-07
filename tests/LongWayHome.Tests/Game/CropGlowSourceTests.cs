using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files from the repository (the shader and the new <c>NightGlowView</c> aren't all in the test
/// output) and checks the Godot glue of #94's crop night glow: <c>MeshPart</c> and <c>ArtMeshes.Merge</c> carry a
/// part's glow into UV2 only when some part glows, <c>CropModels</c> copies it, the shared shader turns it into
/// <c>EMISSION</c> scaled by the <c>art_night</c> global, <c>NightGlowView</c> drives that global from the clock in the
/// farm, and the art sandbox sets it per light preset. Each check looks for the essential tokens, not exact formatting.
/// </summary>
public class CropGlowSourceTests
{
    // ---- AC3: glow travels into the merged mesh as UV2, only when a part glows ----

    [Fact]
    public void AC3_mesh_part_declares_trailing_glow_and_twinkle()
    {
        var source = ReadRepoFile("game", "art", "ArtMeshes.cs");

        Assert.Matches(
            @"record\s+struct\s+MeshPart\s*\([^)]*,\s*float\s+Glow\s*=\s*0(\.0)?f\s*,\s*bool\s+Twinkle\s*=\s*false\s*\)",
            source);
    }

    [Fact]
    public void AC3_merge_writes_uv2_glow_and_twinkle_only_when_a_part_glows()
    {
        var merge = MethodBody(ReadRepoFile("game", "art", "ArtMeshes.cs"), "Merge");

        Assert.Contains("SetUV2(", merge, StringComparison.Ordinal);
        Assert.Matches(@"Glow\s*>\s*0", merge);
        Assert.Matches(@"[Tt]winkle\s*\?\s*1(\.0)?f?\s*:\s*0", merge);
        // The UV2 write sits under a condition (e.g. `if (anyGlow)`), so meshes with no glow get no UV2 array.
        Assert.Matches(@"if\s*\([^)]*\)\s*\{?[^}]*SetUV2\(", merge);
    }

    [Fact]
    public void AC3_crop_models_copies_glow_and_twinkle_into_mesh_part()
    {
        var source = ReadRepoFile("game", "art", "crops", "CropModels.cs");

        Assert.Matches(@"new\s+MeshPart\s*\([^;]*\bpart\.Glow\b[^;]*\bpart\.Twinkle\b[^;]*\)\s*;", source);
    }

    // ---- AC4: zero by default, so every mesh at art_night 0 looks exactly as before ----

    [Fact]
    public void AC4_shader_declares_the_art_night_global_and_scales_uv2_glow_by_it()
    {
        var shader = ReadShader();

        Assert.Matches(@"global\s+uniform\s+float\s+art_night\s*;", shader);
        Assert.Matches(@"varying\s+(flat\s+)?float\s+v_glow\s*;", shader);
        Assert.Matches(@"UV2\s*\.\s*x\s*\*\s*art_night", shader);
    }

    [Fact]
    public void AC4_shader_emission_is_albedo_times_glow_times_glow_energy()
    {
        var shader = ReadShader();

        Assert.Matches(@"const\s+float\s+GLOW_ENERGY\s*=\s*1\.6\s*;", shader);
        Assert.Matches(@"EMISSION\s*=\s*ALBEDO\s*\*\s*v_glow\s*\*\s*GLOW_ENERGY", shader);
    }

    [Fact]
    public void AC4_shader_header_describes_art_night_uv2_glow_and_emission()
    {
        var header = string.Join(
            '\n',
            ReadShader().Split('\n').TakeWhile(line => !line.StartsWith("shader_type", StringComparison.Ordinal))
                .Where(line => line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

        Assert.Contains("art_night", header, StringComparison.Ordinal);
        Assert.Contains("UV2", header, StringComparison.Ordinal);
        Assert.Contains("EMISSION", header, StringComparison.Ordinal);
    }

    [Fact]
    public void AC4_project_declares_art_night_as_a_float_shader_global_defaulting_to_zero()
    {
        var project = ReadRepoFile("game", "project.godot");
        var start = project.IndexOf("[shader_globals]", StringComparison.Ordinal);
        Assert.True(start >= 0, "project.godot has no [shader_globals] section");
        var next = project.IndexOf("\n[", start + 1, StringComparison.Ordinal);
        var section = next < 0 ? project[start..] : project[start..next];

        Assert.Matches(
            @"art_night\s*=\s*\{\s*""type""\s*:\s*""float""\s*,\s*""value""\s*:\s*0(\.0)?\s*\}",
            section);
    }

    // ---- AC5: steady breathing pulse and twinkle, no bloom ----

    [Fact]
    public void AC5_shader_steady_glow_breathes_out_of_step_between_crops()
    {
        var shader = ReadShader();

        Assert.Matches(@"UV2\s*\.\s*y", shader);
        Assert.Matches(@"0\.85\s*\+\s*0\.15\s*\*\s*sin\s*\(\s*TIME\s*\*\s*1\.5", shader);
        Assert.Matches(@"NODE_POSITION_WORLD\s*\.\s*x\s*\+\s*NODE_POSITION_WORLD\s*\.\s*z", shader);
    }

    [Fact]
    public void AC5_shader_twinkle_flashes_each_speck_on_its_own()
    {
        var shader = ReadShader();

        Assert.Matches(@"0\.35\s*\+\s*0\.65\s*\*\s*pow\s*\(", shader);
        Assert.Matches(@"TIME\s*\*\s*5\.0\s*\+\s*dot\s*\(\s*VERTEX\s*,\s*vec3\s*\(\s*91\.0\s*,\s*37\.0\s*,\s*53\.0\s*\)", shader);
    }

    /// <summary>Constraint guard (REQ-14): no bloom in the farm or the sandbox. Passes before and after #94.</summary>
    [Theory]
    [InlineData("game/scenes/Farm.tscn")]
    [InlineData("game/art/sandbox/ArtSandbox.tscn")]
    [InlineData("game/art/sandbox/ArtSandbox.cs")]
    [InlineData("game/scripts/World/RainView.cs")]
    public void AC5_no_world_environment_glow_is_turned_on(string path)
    {
        var source = ReadRepoFile(path.Split('/'));

        Assert.DoesNotMatch(@"glow_enabled\s*=\s*true", source);
        Assert.DoesNotMatch(@"GlowEnabled\s*=\s*true", source);
    }

    // ---- AC6: NightGlowView drives art_night from the clock in the farm ----

    [Fact]
    public void AC6_night_glow_view_is_a_node_in_the_art_namespace()
    {
        var source = ReadNightGlowView();

        Assert.Matches(@"namespace\s+LongWayHome\.Game\.Art\s*;", source);
        Assert.Matches(@"partial\s+class\s+NightGlowView\s*:\s*Node\b", source);
    }

    [Fact]
    public void AC6_night_glow_view_reads_the_session_and_subscribes_to_the_clock_and_the_night()
    {
        var ready = MethodBody(ReadNightGlowView(), "_Ready");

        Assert.Contains("GameRoot.Session", ready, StringComparison.Ordinal);
        Assert.Matches(@"\bClockChanged\s*\+=", ready);
        Assert.Matches(@"\bNightCompleted\s*\+=", ready);
    }

    [Fact]
    public void AC6_night_glow_view_sets_art_night_from_the_clock_through_night_glow()
    {
        var source = ReadNightGlowView();

        Assert.Contains("\"art_night\"", source, StringComparison.Ordinal);
        Assert.Contains("RenderingServer.GlobalShaderParameterSet(", source, StringComparison.Ordinal);
        Assert.Matches(@"NightGlow\s*\.\s*At\s*\([^)]*ClockMinutes", source);
    }

    [Fact]
    public void AC6_night_glow_view_unsubscribes_in_exit_tree()
    {
        var exit = MethodBody(ReadNightGlowView(), "_ExitTree");

        Assert.Matches(@"\bClockChanged\s*-=", exit);
        Assert.Matches(@"\bNightCompleted\s*-=", exit);
    }

    [Fact]
    public void AC6_farm_scene_has_a_night_glow_view_node_with_the_script()
    {
        var farm = ReadRepoFile("game", "scenes", "Farm.tscn");

        var resource = Regex.Match(
            farm,
            @"\[ext_resource\s+type=""Script""[^\]]*path=""res://art/crops/NightGlowView\.cs""[^\]]*id=""(?<id>[^""]+)""\]");
        Assert.True(resource.Success, "Farm.tscn has no ext_resource for res://art/crops/NightGlowView.cs");
        var id = Regex.Escape(resource.Groups["id"].Value);

        Assert.Matches(
            @"\[node\s+name=""NightGlowView""\s+type=""Node""\s+parent=""\.""\]\s*\r?\nscript\s*=\s*ExtResource\(""" + id + @"""\)",
            farm);
    }

    // ---- AC7: art_night goes back to 0 on leaving; the sandbox sets it per preset ----

    [Fact]
    public void AC7_night_glow_view_resets_art_night_to_zero_in_exit_tree()
    {
        var exit = MethodBody(ReadNightGlowView(), "_ExitTree");

        Assert.Matches(@"GlobalShaderParameterSet\s*\([^;]*,\s*0(\.0)?f?\s*\)\s*;", exit);
    }

    [Fact]
    public void AC7_sandbox_sets_art_night_from_the_look()
    {
        var source = ReadRepoFile("game", "art", "sandbox", "ArtSandbox.cs");

        Assert.Contains("\"art_night\"", source, StringComparison.Ordinal);
        Assert.Matches(@"GlobalShaderParameterSet\s*\([^;]*\.ArtNight\b", source);
    }

    [Fact]
    public void AC7_sandbox_resets_art_night_to_zero_in_exit_tree()
    {
        var exit = MethodBody(ReadRepoFile("game", "art", "sandbox", "ArtSandbox.cs"), "_ExitTree");

        Assert.Matches(@"GlobalShaderParameterSet\s*\([^;]*,\s*0(\.0)?f?\s*\)\s*;", exit);
    }

    // ---- Docs ----

    [Fact]
    public void Docs_blueprint_names_night_glow_view_and_art_night()
    {
        var blueprint = ReadRepoFile("docs", "requirements", "technical-blueprint.md");

        Assert.Contains("crops/NightGlowView.cs", blueprint, StringComparison.Ordinal);
        Assert.Contains("art_night", blueprint, StringComparison.Ordinal);
    }

    [Fact]
    public void Docs_art_direction_says_glows_show_only_on_ripe_crops_from_1930_to_2030()
    {
        var art = ReadRepoFile("docs", "ART_DIRECTION.md");

        Assert.Contains("19:30", art, StringComparison.Ordinal);
        Assert.Contains("20:30", art, StringComparison.Ordinal);
    }

    // ---- helpers ----

    private static string ReadShader() => ReadRepoFile("game", "art", "shaders", "low_poly.gdshader");

    private static string ReadNightGlowView() => ReadRepoFile("game", "art", "crops", "NightGlowView.cs");

    /// <summary>
    /// The body of the first method named <paramref name="name"/>: from its opening brace to the matching closing
    /// brace. Fails the test if there is no such method.
    /// </summary>
    private static string MethodBody(string source, string name)
    {
        var head = Regex.Match(source, @"\b" + Regex.Escape(name) + @"\s*(<[^>]*>)?\s*\([^)]*\)\s*\{");
        Assert.True(head.Success, $"no method {name}");
        var depth = 0;
        for (var i = head.Index + head.Length - 1; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}' && --depth == 0)
            {
                return source[(head.Index + head.Length)..i];
            }
        }

        Assert.Fail($"method {name} has no closing brace");
        return string.Empty;
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

using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files (linked into the test output by the test project) and the blueprint and checks the
/// Godot glue of #102 that xUnit can't run: the Player scene draws the farmer instead of the prototype capsule and
/// nose, <c>FarmerModel</c> builds one skinned body under a humanoid skeleton with the outline as the shared
/// material's next pass, <c>HeldItemView</c> keeps the held item in hand, and the sandbox shows a <c>Farmer</c> row.
/// </summary>
public class FarmerSceneTests
{
    private const string FarmerScenePath = "res://art/farmer/Farmer.tscn";

    // ---- AC6: the Player is drawn as the farmer ----

    [Fact]
    public void AC6_player_scene_has_no_prototype_body_or_nose()
    {
        var scene = ReadGameFile("scenes", "Player.tscn");

        Assert.DoesNotMatch(@"\[node\s+name=""Body""", scene);
        Assert.DoesNotMatch(@"\[node\s+name=""Nose""", scene);
        Assert.DoesNotContain("CapsuleMesh", scene, StringComparison.Ordinal);
        Assert.DoesNotContain("BoxMesh", scene, StringComparison.Ordinal);
    }

    [Fact]
    public void AC6_player_model_instances_the_farmer_scene()
    {
        var scene = ReadGameFile("scenes", "Player.tscn");

        var resource = Regex.Match(
            scene,
            @"\[ext_resource[^\]]*type=""PackedScene""[^\]]*path=""" + Regex.Escape(FarmerScenePath) + @"""[^\]]*id=""([^""]+)""");
        Assert.True(resource.Success, $"Player.tscn has no PackedScene ext_resource for {FarmerScenePath}");
        var id = Regex.Escape(resource.Groups[1].Value);

        Assert.Matches(
            @"\[node\s+name=""[^""]+""\s+parent=""Model""\s+instance=ExtResource\(\s*""" + id + @"""\s*\)\]",
            scene);
    }

    /// <summary>Characterization: the collision capsule must not change with the model (passes before and after).</summary>
    [Fact]
    public void AC6_collision_capsule_is_still_radius_0_35_height_1_6_at_y_0_8()
    {
        var scene = ReadGameFile("scenes", "Player.tscn");

        Assert.Matches(@"\[sub_resource\s+type=""CapsuleShape3D""[^\]]*\]\s*radius\s*=\s*0\.35\s*height\s*=\s*1\.6\s", scene);
        Assert.Matches(
            @"\[node\s+name=""CollisionShape3D""\s+type=""CollisionShape3D""\s+parent=""\.""\]\s*transform\s*=\s*Transform3D\(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0\.8, 0\)",
            scene);
    }

    [Fact]
    public void AC6_player_scene_has_a_held_item_view()
    {
        var scene = ReadGameFile("scenes", "Player.tscn");

        var resource = Regex.Match(
            scene,
            @"\[ext_resource[^\]]*type=""Script""[^\]]*path=""res://scripts/Player/HeldItemView\.cs""[^\]]*id=""([^""]+)""");
        Assert.True(resource.Success, "Player.tscn has no Script ext_resource for res://scripts/Player/HeldItemView.cs");
        var id = Regex.Escape(resource.Groups[1].Value);

        Assert.Matches(
            @"\[node\s+name=""HeldItemView""[^\]]*\][^\[]*script\s*=\s*ExtResource\(\s*""" + id + @"""\s*\)",
            scene);
    }

    // ---- AC7: one skinned body under a humanoid skeleton, with the outline ----

    [Fact]
    public void AC7_farmer_scene_root_is_a_node3d_named_farmer_with_the_farmer_model_script()
    {
        var scene = ReadGameFile("art", "farmer", "Farmer.tscn");

        var resource = Regex.Match(
            scene,
            @"\[ext_resource[^\]]*type=""Script""[^\]]*path=""res://art/farmer/FarmerModel\.cs""[^\]]*id=""([^""]+)""");
        Assert.True(resource.Success, "Farmer.tscn has no Script ext_resource for res://art/farmer/FarmerModel.cs");
        var id = Regex.Escape(resource.Groups[1].Value);

        Assert.Matches(
            @"\[node\s+name=""Farmer""\s+type=""Node3D""\s*\][^\[]*script\s*=\s*ExtResource\(\s*""" + id + @"""\s*\)",
            scene);
    }

    [Fact]
    public void AC7_farmer_model_builds_a_skeleton_from_the_farmer_bones()
    {
        var source = ReadGameFile("art", "farmer", "FarmerModel.cs");

        Assert.Matches(@"namespace\s+LongWayHome\.Game\.Art\b", source);
        Assert.Matches(@"class\s+FarmerModel\b", source);
        Assert.Contains("Skeleton3D", source, StringComparison.Ordinal);
        Assert.Contains("FarmerArt.Bones", source, StringComparison.Ordinal);
        Assert.Contains("\"Skeleton\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC7_farmer_body_is_one_skinned_mesh_with_the_shared_material()
    {
        var source = ReadGameFile("art", "farmer", "FarmerModel.cs");

        Assert.Contains("FarmerArt.Parts", source, StringComparison.Ordinal);
        Assert.Contains("ArtMeshes.MergeSkinned(", source, StringComparison.Ordinal);
        Assert.Matches(@"\bSkin\b", source);
        Assert.Contains("\"Body\"", source, StringComparison.Ordinal);
        Assert.Contains("MaterialOverride", source, StringComparison.Ordinal);
        Assert.Contains("ArtMaterials.Shared", source, StringComparison.Ordinal);
        Assert.Matches(@"\bstatic\b", source);
    }

    [Fact]
    public void AC7_farmer_body_material_has_the_outline_as_its_next_pass()
    {
        var source = ReadGameFile("art", "farmer", "FarmerModel.cs");

        Assert.Contains("NextPass", source, StringComparison.Ordinal);
        Assert.Contains("outline.gdshader", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC7_art_meshes_has_a_skinned_merge_writing_bones_and_weights()
    {
        var source = ReadGameFile("art", "ArtMeshes.cs");

        Assert.Matches(@"public\s+static\s+ArrayMesh\s+MergeSkinned\s*\(", source);
        Assert.Contains("SetBones(", source, StringComparison.Ordinal);
        Assert.Contains("SetWeights(", source, StringComparison.Ordinal);
        Assert.Matches(@"public\s+static\s+ArrayMesh\s+Merge\s*\(\s*IEnumerable<MeshPart>", source);
    }

    [Fact]
    public void AC7_outline_shader_is_an_unshaded_inverted_hull_with_a_width()
    {
        var shader = ReadGameFile("art", "shaders", "outline.gdshader");

        var renderMode = Regex.Match(shader, @"render_mode\s+([^;]*);");
        Assert.True(renderMode.Success, "outline.gdshader has no render_mode");
        Assert.Contains("unshaded", renderMode.Groups[1].Value, StringComparison.Ordinal);
        Assert.Contains("cull_front", renderMode.Groups[1].Value, StringComparison.Ordinal);
        Assert.Matches(@"uniform\s+float\s+width\b", shader);
        Assert.Contains("NORMAL", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void AC7_farmer_has_a_contact_shadow_that_casts_no_shadow()
    {
        var text = FarmerSceneAndScript();

        // "Shadow" matches both a scene node (name="Shadow") and a node named in code (Name = "Shadow").
        Assert.Contains("\"Shadow\"", text, StringComparison.Ordinal);
        Assert.Matches(@"ShadowCastingSetting\.Off|cast_shadow\s*=\s*0\b", text);
    }

    [Fact]
    public void AC7_held_item_is_attached_to_the_right_hand_bone()
    {
        var text = FarmerSceneAndScript();

        Assert.Contains("BoneAttachment3D", text, StringComparison.Ordinal);
        Assert.Contains("RightHand", text, StringComparison.Ordinal);
        Assert.Contains("HeldItemArt.PartsFor(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AC7_farmer_model_shows_a_held_item()
    {
        var source = ReadGameFile("art", "farmer", "FarmerModel.cs");

        Assert.Matches(@"public\s+void\s+ShowHeld\s*\(\s*HeldItem\s+\w+\s*\)", source);
    }

    // ---- AC8: the held item follows the selection and the inventory ----

    [Fact]
    public void AC8_held_item_view_shows_the_selection_on_the_farmer()
    {
        var source = ReadGameFile("scripts", "Player", "HeldItemView.cs");

        Assert.Contains(".ShowHeld(", source, StringComparison.Ordinal);
        Assert.Contains(".Held", source, StringComparison.Ordinal);
        Assert.Matches(@"override\s+void\s+_Ready\s*\(", source);
    }

    [Fact]
    public void AC8_held_item_view_subscribes_to_selection_and_inventory_changes()
    {
        var source = ReadGameFile("scripts", "Player", "HeldItemView.cs");

        Assert.Matches(@"\.SelectionChanged\s*\+=", source);
        Assert.Matches(@"\.InventoryChanged\s*\+=", source);
    }

    [Fact]
    public void AC8_held_item_view_unsubscribes_on_exit_tree()
    {
        var source = ReadGameFile("scripts", "Player", "HeldItemView.cs");

        var exit = Regex.Match(source, @"override\s+void\s+_ExitTree\s*\(\s*\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(exit.Success, "HeldItemView has no _ExitTree");
        Assert.Matches(@"\.SelectionChanged\s*-=", exit.Groups["body"].Value);
        Assert.Matches(@"\.InventoryChanged\s*-=", exit.Groups["body"].Value);
    }

    // ---- AC10: the sandbox's Farmer row ----

    [Fact]
    public void AC10_sandbox_builds_a_farmer_row_at_four_yaws()
    {
        var source = ReadGameFile("art", "sandbox", "ArtSandbox.cs");

        Assert.Contains("\"Farmer\"", source, StringComparison.Ordinal);
        Assert.True(
            source.Contains(FarmerScenePath, StringComparison.Ordinal) || source.Contains("FarmerModel", StringComparison.Ordinal),
            "ArtSandbox.cs builds no farmer from Farmer.tscn or FarmerModel");
        Assert.Matches(@"\b0(?:\.0)?f?\s*,\s*90(?:\.0)?f?\s*,\s*180(?:\.0)?f?\s*,\s*270(?:\.0)?f?", source);
        Assert.Contains("ShowHeld(", source, StringComparison.Ordinal);
        Assert.Contains("CropTypeId.Emberroot", source, StringComparison.Ordinal);
    }

    // ---- Blueprint ----

    [Fact]
    public void Docs_blueprint_names_the_farmer_and_its_outline()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "docs", "requirements", "technical-blueprint.md");
        Assert.True(File.Exists(path), "docs/requirements/technical-blueprint.md not found");
        var blueprint = File.ReadAllText(path);

        Assert.Contains("farmer/Farmer.tscn", blueprint, StringComparison.Ordinal);
        Assert.Contains("FarmerModel.cs", blueprint, StringComparison.Ordinal);
        Assert.Contains("outline.gdshader", blueprint, StringComparison.Ordinal);
        Assert.Contains("MergeSkinned", blueprint, StringComparison.Ordinal);
    }

    // ---- helpers ----

    /// <summary>The farmer's scene and builder together: a node may be declared in either.</summary>
    private static string FarmerSceneAndScript() =>
        ReadGameFile("art", "farmer", "Farmer.tscn") + "\n" + ReadGameFile("art", "farmer", "FarmerModel.cs");

    private static string ReadGameFile(params string[] parts)
    {
        var path = Path.Combine(new[] { AppContext.BaseDirectory, "game" }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"game/{string.Join('/', parts)} not found");
        return File.ReadAllText(path);
    }
}

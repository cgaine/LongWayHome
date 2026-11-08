using System.Globalization;
using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files (found by walking up to <c>LongWayHome.sln</c>) for #138's scene and script tests: a
/// <c>.tscn</c>'s nodes, their ext_resources, string and number properties, and positions with their parents'
/// translations added.
/// </summary>
internal static class SceneText
{
    private static readonly Regex NodeBlock = new(
        @"^\[node\s+(?<attrs>[^\]]*)\]\s*$(?<body>.*?)(?=^\[|\z)",
        RegexOptions.Multiline | RegexOptions.Singleline);

    internal sealed record Node(string Name, string? Type, string? Parent, string? Instance, string Body)
    {
        public string Path => Parent is null ? "." : Parent == "." ? Name : $"{Parent}/{Name}";
    }

    public static List<Node> Nodes(string scene) =>
        NodeBlock.Matches(scene)
            .Select(match =>
            {
                var attrs = match.Groups["attrs"].Value;
                var instance = Regex.Match(attrs, @"instance=ExtResource\(""([^""]+)""\)");
                return new Node(
                    Attribute(attrs, "name") ?? "",
                    Attribute(attrs, "type"),
                    Attribute(attrs, "parent"),
                    instance.Success ? instance.Groups[1].Value : null,
                    match.Groups["body"].Value);
            })
            .ToList();

    /// <summary>The id of the scene's ext_resource of <paramref name="type"/> at <paramref name="path"/>, or null.</summary>
    public static string? ExtResourceId(string scene, string type, string path)
    {
        foreach (Match match in Regex.Matches(scene, @"^\[ext_resource\s+(?<attrs>[^\]]*)\]", RegexOptions.Multiline))
        {
            var attrs = match.Groups["attrs"].Value;
            if (Attribute(attrs, "type") == type && Attribute(attrs, "path") == path)
            {
                return Attribute(attrs, "id");
            }
        }

        return null;
    }

    /// <summary>The nodes whose script is the given ext_resource script; none when the scene doesn't load it.</summary>
    public static List<Node> ScriptedNodes(string scene, string scriptPath)
    {
        var id = ExtResourceId(scene, "Script", scriptPath);
        if (id is null)
        {
            return [];
        }

        var script = new Regex(@"^script\s*=\s*ExtResource\(""" + Regex.Escape(id) + @"""\)\s*$", RegexOptions.Multiline);
        return Nodes(scene).Where(node => script.IsMatch(node.Body)).ToList();
    }

    /// <summary>The nodes that instance the given packed scene; none when the scene doesn't load it.</summary>
    public static List<Node> InstancesOf(string scene, string scenePath)
    {
        var id = ExtResourceId(scene, "PackedScene", scenePath);
        return id is null ? [] : Nodes(scene).Where(node => node.Instance == id).ToList();
    }

    /// <summary>A string property set on the node, e.g. <c>ResidentId = "maggie"</c>.</summary>
    public static string? Property(Node node, string key)
    {
        var match = Regex.Match(node.Body, @"^" + key + @"\s*=\s*""([^""]*)""\s*$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>A number property set on the node, e.g. <c>Range = 1.5</c>.</summary>
    public static double? Number(Node node, string key)
    {
        var match = Regex.Match(node.Body, @"^" + key + @"\s*=\s*([-0-9.eE]+)\s*$", RegexOptions.Multiline);
        return match.Success ? double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
    }

    /// <summary>The node's position in the scene: its and its parents' <c>Transform3D</c> origins added.</summary>
    public static (double X, double Y, double Z) Position(string scene, Node node)
    {
        var byPath = Nodes(scene).ToDictionary(n => n.Path);
        var (x, y, z) = (0.0, 0.0, 0.0);
        for (var current = node; current is not null;
             current = current.Parent is { } parent && byPath.TryGetValue(parent, out var up) ? up : null)
        {
            if (Origin(current) is { } origin)
            {
                (x, y, z) = (x + origin.X, y + origin.Y, z + origin.Z);
            }
        }

        return (x, y, z);
    }

    /// <summary>The source without its <c>//</c> comments.</summary>
    public static string Code(string source) => Regex.Replace(source, @"//[^\n]*", "");

    public static string ReadRepoFile(params string[] parts)
    {
        var path = System.IO.Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"{string.Join('/', parts)} not found");
        return File.ReadAllText(path);
    }

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "LongWayHome.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"no folder holding LongWayHome.sln above {AppContext.BaseDirectory}");
        return dir!.FullName;
    }

    private static (double X, double Y, double Z)? Origin(Node node)
    {
        var match = Regex.Match(node.Body, @"^transform\s*=\s*Transform3D\(([^)]*)\)", RegexOptions.Multiline);
        if (!match.Success)
        {
            return null;
        }

        var numbers = match.Groups[1].Value
            .Split(',')
            .Select(part => double.Parse(part.Trim(), CultureInfo.InvariantCulture))
            .ToArray();
        return numbers.Length == 12 ? (numbers[9], numbers[10], numbers[11]) : null;
    }

    private static string? Attribute(string attrs, string key)
    {
        var match = Regex.Match(attrs, @"(?:^|\s)" + key + @"=""([^""]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }
}

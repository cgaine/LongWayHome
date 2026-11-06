using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>docs/smoke-checklist.md</c> and <c>docs/requirements/technical-blueprint.md</c>, linked into the
/// test output by the test project, and checks the manual smoke checklist covers a full Day on each scheme (REQ-13,
/// issue #54 AC8).
/// </summary>
public class SmokeChecklistTests
{
    private const string ChecklistPath = "docs/smoke-checklist.md";

    /// <summary>Each step of a full Day, as one or more alternative keywords (matched case-insensitively).</summary>
    private static readonly string[][] FullDaySteps =
    [
        ["start", "load"], ["game"],
        ["walk"], ["run"],
        ["cycle"], ["held item"],
        ["till"], ["plant"], ["water"], ["refill"], ["pond"],
        ["harvest"], ["ship"], ["bin"],
        ["shop"], ["buy"], ["seed"], ["close"],
        ["pause"], ["resume"],
        ["sleep"], ["door"], ["night"], ["morning"],
    ];

    private static readonly string[] PassNames = ["controller", "keyboard", "steam deck"];

    public static TheoryData<string> Passes => new(PassNames);

    [Fact]
    public void AC8_checklist_has_controller_keyboard_and_steam_deck_passes()
    {
        var sections = ReadSections(ReadChecklist());

        foreach (var pass in PassNames)
        {
            Assert.True(FindPass(sections, pass) is not null, $"{ChecklistPath} has no '## ' section for the {pass} pass");
        }
    }

    [Theory]
    [MemberData(nameof(Passes))]
    public void AC8_each_pass_lists_every_step_of_a_full_day(string pass)
    {
        var section = FindPass(ReadSections(ReadChecklist()), pass);
        Assert.True(section is not null, $"{ChecklistPath} has no '## ' section for the {pass} pass");

        Assert.True(section.Value.Body.Contains("- [ ]", StringComparison.Ordinal), $"The {pass} pass has no tick boxes ('- [ ]')");
        foreach (var alternatives in FullDaySteps)
        {
            Assert.True(
                alternatives.Any(keyword => section.Value.Body.Contains(keyword, StringComparison.OrdinalIgnoreCase)),
                $"The {pass} pass never mentions '{string.Join("' or '", alternatives)}'");
        }
    }

    [Fact]
    public void AC8_blueprint_testing_section_links_the_checklist()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "docs", "requirements", "technical-blueprint.md");
        Assert.True(File.Exists(path), "docs/requirements/technical-blueprint.md not found");

        var testing = ReadSections(File.ReadAllText(path))
            .FirstOrDefault(section => section.Heading.StartsWith("6.", StringComparison.Ordinal));
        Assert.True(testing.Heading is not null, "technical-blueprint.md has no '## 6.' section");

        Assert.Contains("smoke-checklist.md", testing.Body, StringComparison.Ordinal);
    }

    private static string ReadChecklist()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "docs", "smoke-checklist.md");
        Assert.True(File.Exists(path), $"{ChecklistPath} not found");
        return File.ReadAllText(path);
    }

    /// <summary>The section for <paramref name="pass"/>: the first <c>##</c> heading naming it. The controller and
    /// keyboard passes skip the Steam Deck section, which may also mention controls.</summary>
    private static (string Heading, string Body)? FindPass(IReadOnlyList<(string Heading, string Body)> sections, string pass)
    {
        foreach (var section in sections)
        {
            var heading = section.Heading;
            var isSteamDeck = heading.Contains("steam deck", StringComparison.OrdinalIgnoreCase);
            if (heading.Contains(pass, StringComparison.OrdinalIgnoreCase) && (pass == "steam deck" || !isSteamDeck))
            {
                return section;
            }
        }

        return null;
    }

    private static List<(string Heading, string Body)> ReadSections(string markdown)
    {
        var sections = new List<(string Heading, string Body)>();
        foreach (Match match in Regex.Matches(markdown, @"^## (?<heading>.+?)\s*$(?<body>.*?)(?=^## |\z)", RegexOptions.Multiline | RegexOptions.Singleline))
        {
            sections.Add((match.Groups["heading"].Value, match.Groups["body"].Value));
        }

        return sections;
    }
}

using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>docs/smoke-checklist.md</c> and <c>docs/requirements/technical-blueprint.md</c>, linked into the
/// test output by the test project, and checks the manual smoke checklist covers a full Day on each scheme (REQ-13,
/// issue #54 AC8), including the camera turn and zoom on each pass (issue #71 AC6).
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
        ["camera"], ["turn"], ["zoom"],
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

    [Theory]
    [MemberData(nameof(Passes))]
    public void AC6_each_pass_lists_camera_turn_and_zoom(string pass)
    {
        var section = FindPass(ReadSections(ReadChecklist()), pass);
        Assert.True(section is not null, $"{ChecklistPath} has no '## ' section for the {pass} pass");
        var body = section.Value.Body;

        foreach (var keyword in new[] { "camera", "turn", "zoom" })
        {
            Assert.True(body.Contains(keyword, StringComparison.OrdinalIgnoreCase), $"The {pass} pass never mentions '{keyword}'");
        }

        string[] buttons = pass switch
        {
            "controller" => ["(R stick)"],
            "keyboard" => ["[Z]", "[C]", "[R]", "[V]"],
            _ => [],
        };
        if (buttons.Length == 0)
        {
            return;
        }

        var buttonsLine = ReadButtonsLine(body);
        Assert.True(buttonsLine is not null, $"The {pass} pass has no 'Buttons:' line");
        foreach (var button in buttons)
        {
            Assert.True(buttonsLine.Contains(button, StringComparison.Ordinal), $"The {pass} pass's Buttons: line never mentions {button}");
        }
    }

    /// <summary>#103 AC14: each action's step says the farmer plays its animation.</summary>
    [Theory]
    [MemberData(nameof(Passes))]
    public void AC14_each_pass_checks_the_farmers_animation_for_every_action(string pass)
    {
        var section = FindPass(ReadSections(ReadChecklist()), pass);
        Assert.True(section is not null, $"{ChecklistPath} has no '## ' section for the {pass} pass");
        var steps = ReadSteps(section.Value.Body);

        foreach (var action in new[] { "till", "plant", "water", "refill", "harvest", "ship", "sleep" })
        {
            Assert.True(
                steps.Any(step =>
                    step.Contains(action, StringComparison.OrdinalIgnoreCase) &&
                    step.Contains("animation", StringComparison.OrdinalIgnoreCase)),
                $"The {pass} pass has no step mentioning both '{action}' and the farmer's animation");
        }
    }

    /// <summary>#103 AC14: each pass lets the clock reach 2:00 am and sees the pass out before the night fade.</summary>
    [Theory]
    [MemberData(nameof(Passes))]
    public void AC14_each_pass_checks_the_pass_out_at_2am_before_the_night_fade(string pass)
    {
        var section = FindPass(ReadSections(ReadChecklist()), pass);
        Assert.True(section is not null, $"{ChecklistPath} has no '## ' section for the {pass} pass");
        var steps = ReadSteps(section.Value.Body);

        var step = steps.FirstOrDefault(s =>
            s.Contains("2:00", StringComparison.Ordinal) &&
            Regex.IsMatch(s, @"\bpass(?:es|ed)?\s+out\b", RegexOptions.IgnoreCase));
        Assert.True(step is not null, $"The {pass} pass has no step where the clock reaches 2:00 and the farmer passes out");

        var passOut = Regex.Match(step, @"\bpass(?:es|ed)?\s+out\b", RegexOptions.IgnoreCase);
        var fade = step.IndexOf("fade", StringComparison.OrdinalIgnoreCase);
        Assert.True(fade > passOut.Index, $"The {pass} pass's 2:00 step does not put the night fade after the pass out: {step}");
    }

    /// <summary>#129 AC12: the controller and keyboard passes cycle the hotbar row with that device's button.</summary>
    [Theory]
    [InlineData("controller", "(View)")]
    [InlineData("keyboard", "[X]")]
    public void AC12_pass_has_a_step_cycling_the_hotbar_row_with_its_button(string pass, string button)
    {
        var section = FindPass(ReadSections(ReadChecklist()), pass);
        Assert.True(section is not null, $"{ChecklistPath} has no '## ' section for the {pass} pass");
        var steps = ReadSteps(section.Value.Body);

        Assert.True(
            steps.Any(step =>
                step.Contains(button, StringComparison.Ordinal) &&
                step.Contains("hotbar row", StringComparison.OrdinalIgnoreCase)),
            $"The {pass} pass has no step cycling the hotbar row with {button}");
    }

    /// <summary>#129 AC12: the new button is in each pass's Buttons: list.</summary>
    [Theory]
    [InlineData("controller", "(View)")]
    [InlineData("keyboard", "[X]")]
    public void AC12_pass_buttons_line_lists_the_hotbar_cycle_button(string pass, string button)
    {
        var section = FindPass(ReadSections(ReadChecklist()), pass);
        Assert.True(section is not null, $"{ChecklistPath} has no '## ' section for the {pass} pass");

        var buttonsLine = ReadButtonsLine(section.Value.Body);
        Assert.True(buttonsLine is not null, $"The {pass} pass has no 'Buttons:' line");
        Assert.True(buttonsLine.Contains(button, StringComparison.Ordinal), $"The {pass} pass's Buttons: line never mentions {button}");
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

    /// <summary>The pass's <c>Buttons:</c> paragraph, which wraps over several source lines up to the next blank
    /// line.</summary>
    private static string? ReadButtonsLine(string body)
    {
        var match = Regex.Match(body, @"^Buttons:.*?(?=^\s*$|\z)", RegexOptions.Multiline | RegexOptions.Singleline);
        return match.Success ? match.Value : null;
    }

    /// <summary>The pass's tick-box steps, each from its <c>- [ ]</c> up to the next step or blank line, so a step
    /// wrapped over several source lines stays whole.</summary>
    private static List<string> ReadSteps(string body) =>
        Regex.Matches(body, @"^- \[ \].*?(?=^- \[ \]|^\s*$|\z)", RegexOptions.Multiline | RegexOptions.Singleline)
            .Select(match => match.Value)
            .ToList();

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

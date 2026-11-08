using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>
/// The Opening's step order over a <see cref="GameSession"/> (BR-I1). Each step moves on only through its own input:
/// a dialogue step when its lines finish, the crash when it finishes, the walk when Maggie arrives, and the till,
/// plant and water steps only when the session's TileChanged shows that action done. Finishing the Seeds lines gives
/// the starter Seeds (BR-P6); finishing the Farewell lines completes the Opening. Holds no Godot state: the
/// presentation plays each step and reports back.
/// </summary>
public sealed class OpeningFlow : IDisposable
{
    /// <summary>The speaker of every line in the Opening.</summary>
    public const string MaggieName = "Maggie";

    private static readonly IReadOnlyList<string> NoLines = [];

    private static readonly IReadOnlyDictionary<OpeningStep, IReadOnlyList<string>> StepLines =
        new Dictionary<OpeningStep, IReadOnlyList<string>>
        {
            [OpeningStep.Arrival] =
            [
                "Easy now. That was some landing. I saw your ship come down over the trees.",
                "Don't you worry, your secret's safe with me. I'm Maggie. I keep the store in town.",
                "There's an old farmhouse up here nobody's used in years. Come on, it's yours if you want it.",
            ],
            [OpeningStep.Farmhouse] =
            [
                "Here we are. A bit tired, like the rest of us, but it'll keep the rain off.",
                "You've a hoe there. Pick a patch of grass and turn the soil over.",
            ],
            [OpeningStep.Seeds] =
            [
                "That's it. Now, these are for you: emberroot and dusktuber, they grow well in spring.",
            ],
            [OpeningStep.PlantLine] = ["Pick the seeds and plant one in that soil."],
            [OpeningStep.WaterLine] = ["Now give it a drink. The pond's there when the can runs dry."],
            [OpeningStep.Farewell] =
            [
                "Water them every day and they'll grow. Sell what you harvest at my store.",
                "When you've some gold put by, I've an axe and a pickaxe that'll help you clear this meadow. Follow the path down through the forest to town.",
            ],
        };

    /// <summary>The Opening's only day: Year 1, Spring 1 (BR-I1).</summary>
    private static readonly GameDate OpeningDay = new(1, Season.Spring, 1);

    private readonly GameSession _session;

    /// <summary>What each Tile looked like at the last change, so a TileChanged can be read as the action it was.</summary>
    private readonly Dictionary<GridPosition, TileLook> _looks = [];

    private bool _disposed;

    /// <summary>
    /// Starts the flow over <paramref name="session"/>: at the crash when the Game's Opening isn't done and it is Year
    /// 1, Spring 1; done otherwise. An unfinished Opening on any other day is completed at once (BR-I1).
    /// </summary>
    public OpeningFlow(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;

        var game = session.Game;
        if (game.OpeningDone)
        {
            // BR-I1: a Game that loads with openingDone never replays it.
            Current = OpeningStep.Done;
            return;
        }

        if (game.Date != OpeningDay)
        {
            // BR-I1: only on Spring 1; an unfinished Opening on any other day is simply over.
            session.CompleteOpening();
            Current = OpeningStep.Done;
            return;
        }

        Current = OpeningStep.Crash;
        foreach (var tile in game.FarmPlot.Tiles)
        {
            _looks[tile.Position] = TileLook.Of(tile);
        }

        session.TileChanged += OnTileChanged;
        session.DayEnded += OnDayEnded;
    }

    /// <summary>Raised once each time <see cref="Current"/> moves on.</summary>
    public event Action? StepChanged;

    public OpeningStep Current { get; private set; }

    /// <summary>Whether the Opening is still playing; the farm's south exit and the door's Sleep wait for it (BR-I1).</summary>
    public bool IsActive => Current != OpeningStep.Done;

    /// <summary>Who speaks the current step's <see cref="Lines"/>.</summary>
    public string Speaker => MaggieName;

    /// <summary>Maggie's lines for the current step, one DialogueBox block; empty on steps where she doesn't talk.</summary>
    public IReadOnlyList<string> Lines => StepLines.TryGetValue(Current, out var lines) ? lines : NoLines;

    /// <summary>
    /// Whether the clock may run on the current step (BR-G2): not during the crash or while Maggie talks; during the
    /// walk, the till, plant and water steps, and once the Opening is done.
    /// </summary>
    public bool ClockRuns => Current is OpeningStep.Walk or OpeningStep.Till or OpeningStep.Plant or OpeningStep.Water
        or OpeningStep.Done;

    /// <summary>The HUD's step hint on a tool step, in <paramref name="device"/>'s glyph; null on every other step.</summary>
    public string? Hint(InputDeviceKind device)
    {
        var text = Current switch
        {
            OpeningStep.Till => "Till a tile",
            OpeningStep.Plant => "Plant a seed",
            OpeningStep.Water => "Water it",
            _ => null,
        };

        return text is null ? null : $"{InputGlyphs.For("use_tool", device)} {text}";
    }

    /// <summary>The crash scene has ended (or was skipped); ignored on any other step.</summary>
    public void CrashFinished()
    {
        if (Current == OpeningStep.Crash)
        {
            MoveTo(OpeningStep.Arrival);
        }
    }

    /// <summary>The current block of Maggie's lines has ended (read or skipped); ignored on steps without lines.</summary>
    public void LinesFinished()
    {
        switch (Current)
        {
            case OpeningStep.Arrival:
                MoveTo(OpeningStep.Walk);
                break;
            case OpeningStep.Farmhouse:
                MoveTo(OpeningStep.Till);
                break;
            case OpeningStep.Seeds:
                // BR-P6: Maggie hands over the starter Seeds; the session refuses a second time (BR-I1).
                _session.GiveStarterSeeds();
                MoveTo(OpeningStep.PlantLine);
                break;
            case OpeningStep.PlantLine:
                MoveTo(OpeningStep.Plant);
                break;
            case OpeningStep.WaterLine:
                MoveTo(OpeningStep.Water);
                break;
            case OpeningStep.Farewell:
                // BR-I1: the Opening ends with her last line and is never played again.
                _session.CompleteOpening();
                Detach();
                MoveTo(OpeningStep.Done);
                break;
        }
    }

    /// <summary>Maggie has reached the farmhouse with the Player near; ignored on any other step.</summary>
    public void WalkArrived()
    {
        if (Current == OpeningStep.Walk)
        {
            MoveTo(OpeningStep.Farmhouse);
        }
    }

    /// <summary>Stops following the session; the flow stays on its current step.</summary>
    public void Dispose() => Detach();

    // BR-I1: the till, plant and water steps move on only when the Tile shows that action was just done.
    private void OnTileChanged(GridPosition position)
    {
        if (!_looks.TryGetValue(position, out var before))
        {
            return;
        }

        var after = TileLook.Of(_session.Game.FarmPlot[position]);
        _looks[position] = after;

        var done = Current switch
        {
            OpeningStep.Till => !before.Tilled && after.Tilled,
            OpeningStep.Plant => !before.Planted && after.Planted,
            OpeningStep.Water => after.Planted && !before.Watered && after.Watered,
            _ => false,
        };

        if (!done)
        {
            return;
        }

        MoveTo(Current switch
        {
            OpeningStep.Till => OpeningStep.Seeds,
            OpeningStep.Plant => OpeningStep.WaterLine,
            _ => OpeningStep.Farewell,
        });
    }

    // BR-I1, BR-G3: a Day that ends mid-Opening completes it in the session (seeds given first), so the flow is over.
    private void OnDayEnded(DayEndReason reason)
    {
        if (IsActive && _session.Game.OpeningDone)
        {
            Detach();
            MoveTo(OpeningStep.Done);
        }
    }

    private void MoveTo(OpeningStep step)
    {
        Current = step;
        StepChanged?.Invoke();
    }

    private void Detach()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session.TileChanged -= OnTileChanged;
        _session.DayEnded -= OnDayEnded;
    }

    /// <summary>The parts of a Tile the tool steps read.</summary>
    private readonly record struct TileLook(bool Tilled, bool Watered, bool Planted)
    {
        public static TileLook Of(Tile tile) =>
            new(tile.State == TileState.Tilled, tile.Watered, tile.Planting is not null);
    }
}

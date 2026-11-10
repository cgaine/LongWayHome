#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Infrastructure.Data;
using LongWayHome.Infrastructure.Random;
using LongWayHome.Infrastructure.Saves;

namespace LongWayHome.Game.Core;

/// <summary>
/// The composition root, registered as an autoload. First caps the frame rate with <see cref="DisplayProfile"/>:
/// 30 fps when the <c>SteamDeck</c> environment variable is "1", uncapped elsewhere. Then loads the reference data when the game starts and
/// quits with exit code 1 if it is missing or invalid: a game with broken data must not start. With the data
/// loaded it holds a fallback new-Game <see cref="Session"/> that saves nowhere, so <c>Farm.tscn</c> runs on its own.
/// The title screen lists the save slots with <see cref="SaveSlots"/> from a <see cref="JsonSaveStore"/> on
/// <c>user://saves</c>, then replaces <see cref="Session"/> through <see cref="StartNewGame"/> or
/// <see cref="LoadGame"/> (via <see cref="GameStarter"/>, with a <see cref="SystemRandomSource"/> for each Night's
/// weather roll) before changing to the farm; that session saves to its slot each Night (BR-G5). A failed nightly
/// save is pushed to the error log. Between area scenes (#136) it carries the <see cref="PendingArrivalId"/> and the
/// <see cref="PendingNight"/> message, both cleared when a new session starts. Holds no rules.
/// </summary>
public partial class GameRoot : Node
{
    private const string DataFolder = "res://data/";

    /// <summary>The save folder; only this composition root knows the <c>user://</c> path.</summary>
    private const string SaveFolder = "user://saves";

    private JsonSaveStore _store = null!;

    public GameData Data { get; private set; } = null!;

    /// <summary>The current Game session. Set once the data has loaded; null only while the game is quitting.</summary>
    public GameSession Session { get; private set; } = null!;

    /// <summary>
    /// The <c>AreaArrival</c> the Player walks in at when the next area scene loads (#136), set by the <c>AreaExit</c>
    /// he left through and cleared by the arrival that places him. Null when the scene should leave him where it put
    /// him (starting or loading a Game, waking on the farm).
    /// </summary>
    public string? PendingArrivalId { get; set; }

    /// <summary>
    /// The night message still to show after a Night that ended away from the farm (#136): the area scene changes to
    /// the farm on NightCompleted, and the farm's <c>NightTransition</c> shows this on start and clears it.
    /// </summary>
    public PendingNightMessage? PendingNight { get; set; }

    public override void _Ready()
    {
        // OS.GetEnvironment returns "" when SteamDeck is unset. V-sync is left as the project sets it.
        Engine.MaxFps = DisplayProfile.MaxFps(DisplayProfile.IsSteamDeck(OS.GetEnvironment("SteamDeck")));

        var errors = new List<string>();
        var crops = ReadFile(JsonGameDataSource.CropsFileName, errors);
        var shop = ReadFile(JsonGameDataSource.ShopFileName, errors);
        var tuning = ReadFile(JsonGameDataSource.TuningFileName, errors);
        var nodes = ReadFile(JsonGameDataSource.NodesFileName, errors);
        var materials = ReadFile(JsonGameDataSource.MaterialsFileName, errors);
        var buildings = ReadFile(JsonGameDataSource.BuildingsFileName, errors);

        if (errors.Count == 0)
        {
            try
            {
                Data = new JsonGameDataSource(crops!, shop!, tuning!, nodes!, materials!, buildings!).Load();
            }
            catch (GameDataException exception)
            {
                errors.AddRange(exception.Errors);
            }
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                GD.PushError(error);
            }

            GetTree().Quit(1);
            return;
        }

        GD.Print($"Loaded {Data.CropTypes.Count} crop types, {Data.ShopListings.Count} shop listings, {Data.NodeSpawns.Count} node spawn points and tuning");

        _store = new JsonSaveStore(ProjectSettings.GlobalizePath(SaveFolder), Data.NodeSpawns);

        // The fallback for running Farm.tscn on its own: a new Game that saves nowhere. The title screen replaces it.
        UseSession(new GameSession(NewGameFactory.Create(Data, DateTimeOffset.UtcNow), Data, new SystemRandomSource()));
    }

    public override void _ExitTree()
    {
        if (Session is not null)
        {
            Session.SaveFailed -= OnSaveFailed;
        }
    }

    /// <summary>The save slots, 1 to 3, as the store lists them.</summary>
    public IReadOnlyList<SaveSlotInfo> SaveSlots() => _store.List();

    /// <summary>
    /// Starts a new Game in the slot, saving it there at once, and makes it the <see cref="Session"/>. A store
    /// exception propagates and leaves the session unchanged.
    /// </summary>
    public void StartNewGame(int slot) =>
        UseSession(GameStarter.StartNew(Data, _store, slot, new SystemRandomSource(), TimeProvider.System));

    /// <summary>
    /// Loads the slot's Game and makes it the <see cref="Session"/>, saving to that slot. A store exception
    /// propagates and leaves the session unchanged.
    /// </summary>
    public void LoadGame(int slot) =>
        UseSession(GameStarter.Load(Data, _store, slot, new SystemRandomSource(), TimeProvider.System));

    private void UseSession(GameSession session)
    {
        if (Session is not null)
        {
            Session.SaveFailed -= OnSaveFailed;
        }

        Session = session;
        PendingArrivalId = null;
        PendingNight = null;
        Session.SaveFailed += OnSaveFailed;
    }

    private static void OnSaveFailed(string reason) => GD.PushError($"The game could not be saved: {reason}");

    private static string? ReadFile(string fileName, List<string> errors)
    {
        var path = DataFolder + fileName;
        var contents = Godot.FileAccess.GetFileAsString(path);
        var openError = Godot.FileAccess.GetOpenError();
        if (openError != Error.Ok)
        {
            errors.Add($"{fileName}: could not read {path}: {openError}");
            return null;
        }

        return contents;
    }
}

/// <summary>The night message to show on the farm after a Night that ended in another area (#136).</summary>
/// <param name="Reason">How the Day ended: "You slept." or "You passed out…".</param>
/// <param name="SaveFailed">Whether that Night's save failed, so the save-failed line shows too.</param>
/// <param name="Rebuilt">The Buildings that Night rebuilt (#165, BR-B3), one line each under the message.</param>
public sealed record PendingNightMessage(DayEndReason Reason, bool SaveFailed, IReadOnlyList<BuildingId> Rebuilt);

#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Infrastructure.Data;
using LongWayHome.Infrastructure.Random;
using LongWayHome.Infrastructure.Saves;

namespace LongWayHome.Game.Core;

/// <summary>
/// The composition root, registered as an autoload. Loads the reference data when the game starts and
/// quits with exit code 1 if it is missing or invalid: a game with broken data must not start. With the data
/// loaded it starts a new Game and holds it in <see cref="Session"/> (there is no title screen yet), with a
/// <see cref="SystemRandomSource"/> for each Night's weather roll and a <see cref="JsonSaveStore"/> on
/// <c>user://saves</c> that each Night saves the Game to (slot 1). A failed save is pushed to the error log.
/// Holds no rules.
/// </summary>
public partial class GameRoot : Node
{
    private const string DataFolder = "res://data/";

    /// <summary>The save folder; only this composition root knows the <c>user://</c> path.</summary>
    private const string SaveFolder = "user://saves";

    /// <summary>The slot the new Game saves to, until a title screen lets the Player choose one.</summary>
    private const int NewGameSlot = 1;

    public GameData Data { get; private set; } = null!;

    /// <summary>The current Game session. Set once the data has loaded; null only while the game is quitting.</summary>
    public GameSession Session { get; private set; } = null!;

    public override void _Ready()
    {
        var errors = new List<string>();
        var crops = ReadFile(JsonGameDataSource.CropsFileName, errors);
        var shop = ReadFile(JsonGameDataSource.ShopFileName, errors);
        var tuning = ReadFile(JsonGameDataSource.TuningFileName, errors);

        if (errors.Count == 0)
        {
            try
            {
                Data = new JsonGameDataSource(crops!, shop!, tuning!).Load();
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

        GD.Print($"Loaded {Data.CropTypes.Count} crop types, {Data.ShopListings.Count} shop listings and tuning");

        // Until the title screen exists, every launch starts a new Game that saves to slot 1 each Night (BR-G5).
        var store = new JsonSaveStore(ProjectSettings.GlobalizePath(SaveFolder));
        Session = new GameSession(
            NewGameFactory.Create(Data, DateTimeOffset.UtcNow),
            Data,
            new SystemRandomSource(),
            store,
            NewGameSlot,
            TimeProvider.System);
        Session.SaveFailed += OnSaveFailed;
    }

    public override void _ExitTree()
    {
        if (Session is not null)
        {
            Session.SaveFailed -= OnSaveFailed;
        }
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

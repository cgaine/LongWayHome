#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Data;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Game.Core;

/// <summary>
/// The composition root, registered as an autoload. Loads the reference data when the game starts and
/// quits with exit code 1 if it is missing or invalid: a game with broken data must not start.
/// Holds no rules.
/// </summary>
public partial class GameRoot : Node
{
    private const string DataFolder = "res://data/";

    public GameData Data { get; private set; } = null!;

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
    }

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

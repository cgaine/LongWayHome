#nullable enable

using System.Collections.Generic;
using Godot;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The seed and crop icon textures (#95), shared by the <see cref="Hud"/> and the <see cref="ShopMenu"/>. Each path
/// (from <see cref="LongWayHome.Application.Art.ItemIcons"/>) is loaded with <see cref="GD.Load{T}(string)"/> once
/// and cached, so rebuilding a row never loads again. Also builds the fixed-size, mouse-ignoring icon node.
/// </summary>
public static class IconTextures
{
    private static readonly Dictionary<string, Texture2D> Cache = [];

    /// <summary>The texture at <paramref name="path"/>, loaded on first use.</summary>
    public static Texture2D Get(string path)
    {
        if (!Cache.TryGetValue(path, out var texture))
        {
            texture = GD.Load<Texture2D>(path);
            Cache[path] = texture;
        }

        return texture;
    }

    /// <summary>A <paramref name="size"/>-pixel square <see cref="TextureRect"/> showing the icon at
    /// <paramref name="path"/>, centred vertically in its row and ignoring the mouse.</summary>
    public static TextureRect Rect(string path, int size, string name = "Icon") => new()
    {
        Name = name,
        Texture = Get(path),
        CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}

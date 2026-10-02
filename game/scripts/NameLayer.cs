using Godot;
using Keladam.Core;

namespace Keladam;

/// <summary>Diyar adlarını ve ordu büyüklüğünü toprakların ortasına (dünya koordinatında) yazar.</summary>
public partial class NameLayer : Node2D
{
    public Game Game { get; set; } = null!;

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        foreach (var p in Game.Players)
        {
            if (!p.IsAlive || p.TileCount < 40) continue;
            var center = new Vector2((float)p.SumX / p.TileCount + 0.5f, (float)p.SumY / p.TileCount + 0.5f);
            int size = Mathf.Clamp((int)(Mathf.Sqrt(p.TileCount) / 4.5f), 3, 48);
            DrawCentered(font, center, p.Name, size);
            DrawCentered(font, center + new Vector2(0, size * 1.05f), Fmt.Num(p.Troops), Mathf.Max(2, size * 3 / 4));
        }
    }

    private void DrawCentered(Font font, Vector2 pos, string text, int size)
    {
        var w = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        var at = pos - new Vector2(w / 2, 0);
        DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, Mathf.Max(1, size / 6), new Color(0, 0, 0, 0.7f));
        DrawString(font, at, text, HorizontalAlignment.Left, -1, size, new Color(1, 0.97f, 0.88f));
    }
}

public static class Fmt
{
    public static string Num(long v) => v switch
    {
        >= 1_000_000 => $"{v / 100_000 / 10.0:0.#}M",
        >= 1_000 => $"{v / 100 / 10.0:0.#}K",
        _ => v.ToString(),
    };
}

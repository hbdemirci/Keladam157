namespace Keladam.Core;

/// <summary>Arazi ve diyar renkleri (0..255). Godot ve Sim aynı paleti kullanır.</summary>
public static class Palette
{
    public static (int, int, int) TerrainColor(Terrain t, int e) => t switch
    {
        Terrain.Water => (24 + e / 6, 52 + e / 4, 82 + e / 3),
        Terrain.Plains => (150 + e / 8, 168 + e / 10, 96),
        Terrain.Forest => (60, 104 + e / 10, 58),
        Terrain.Hills => (150, 134, 92),
        Terrain.Mountain => (120 + e / 4, 116 + e / 4, 112 + e / 4),
        Terrain.Swamp => (84, 98, 70),
        _ => (255, 0, 255),
    };

    // Altın oran ile dağıtılmış ton → komşu ID'ler farklı renkler alır.
    public static (int, int, int) Player(int id)
    {
        int hue = id * 137 % 360;
        return Hsv(hue, 70, 92);
    }

    private static (int, int, int) Hsv(int h, int s, int v)
    {
        int c = v * s * 255 / 10000, x = c * (60 - Math.Abs(h % 120 - 60)) / 60, m = v * 255 / 100 - c;
        var (r, g, b) = h < 60 ? (c, x, 0) : h < 120 ? (x, c, 0) : h < 180 ? (0, c, x) : h < 240 ? (0, x, c) : h < 300 ? (x, 0, c) : (c, 0, x);
        return (r + m, g + m, b + m);
    }
}

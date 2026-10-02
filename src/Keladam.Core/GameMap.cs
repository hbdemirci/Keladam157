namespace Keladam.Core;

public enum Terrain : byte
{
    Water = 0,
    Plains = 1,
    Forest = 2,
    Hills = 3,
    Mountain = 4,
    Swamp = 5,
}

/// <summary>Piksel ızgarası. Her piksel bir tile; indeks = y * Width + x.</summary>
public sealed class GameMap
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Terrain { get; }
    /// <summary>0..255 yükseklik (sadece görüntü gölgelendirmesi için).</summary>
    public byte[] Elevation { get; }
    public int LandTileCount { get; }

    public GameMap(int width, int height, byte[] terrain, byte[] elevation)
    {
        if (terrain.Length != width * height || elevation.Length != width * height)
            throw new ArgumentException("map size mismatch");
        Width = width;
        Height = height;
        Terrain = terrain;
        Elevation = elevation;
        int land = 0;
        foreach (var t in terrain) if (t != (byte)Core.Terrain.Water) land++;
        LandTileCount = land;
    }

    public int Size => Width * Height;
    public int Index(int x, int y) => y * Width + x;
    public int X(int tile) => tile % Width;
    public int Y(int tile) => tile / Width;
    public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
    public Terrain TerrainAt(int tile) => (Terrain)Terrain[tile];
    public bool IsLand(int tile) => Terrain[tile] != (byte)Core.Terrain.Water;

    /// <summary>4-komşuları buf'a yazar, sayısını döner. Sıra sabittir (determinizm).</summary>
    public int Neighbors4(int tile, Span<int> buf)
    {
        int x = tile % Width, y = tile / Width, n = 0;
        if (x > 0) buf[n++] = tile - 1;
        if (x < Width - 1) buf[n++] = tile + 1;
        if (y > 0) buf[n++] = tile - Width;
        if (y < Height - 1) buf[n++] = tile + Width;
        return n;
    }
}

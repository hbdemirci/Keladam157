using System.IO.Compression;
using System.Text;
using Keladam.Core;

// Ekransız simülasyon: yapay zekâ maçı oynatır, durumu yazdırır ve haritayı PNG'ye çizer.
// Kullanım: dotnet run --project tools/Keladam.Sim -- [tohum] [tick] [çıktı.png]
ulong seed = args.Length > 0 ? ulong.Parse(args[0]) : 1;
int ticks = args.Length > 1 ? int.Parse(args[1]) : 3000;
string output = args.Length > 2 ? args[2] : "out/sim.png";

var map = MapGenerator.Generate(1200, 700, seed);
var game = new Game(map, new GameConfig(), seed, NameGen.SinglePlayerRoster("Oyuncu", 14, 40, seed));
var session = new LocalSession(game);

var sw = System.Diagnostics.Stopwatch.StartNew();
for (int i = 0; i < ticks && game.Winner == null; i++)
{
    session.Advance();
    foreach (var e in game.Events) Console.WriteLine($"[{game.Tick / 10,5}s] {e}");
    game.Events.Clear();
}
sw.Stop();

Console.WriteLine($"\n{game.Tick} tick, {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalMilliseconds / Math.Max(1, game.Tick):F2} ms/tick)");
Console.WriteLine($"Harita {map.Width}x{map.Height}, kara {map.LandTileCount}");
foreach (var p in game.Players.Where(p => p.IsAlive).OrderByDescending(p => p.TileCount).Take(10))
    Console.WriteLine($"  {p.Name,-28} toprak {p.TileCount,7} ({p.TileCount * 100 / map.LandTileCount,2}%)  asker {p.Troops,9}  altın {p.Gold,9}");
Console.WriteLine($"Hash {game.StateHash():X16}");

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
File.WriteAllBytes(output, Render(game));
Console.WriteLine($"Görüntü: {output}");

// --- Godot shader'ındaki görünümün CPU kopyası (palet + sınır çizgisi) ---
static byte[] Render(Game g)
{
    var m = g.Map;
    var rgb = new byte[m.Size * 3];
    for (int t = 0; t < m.Size; t++)
    {
        var (r, gr, b) = Palette.TerrainColor(m.TerrainAt(t), m.Elevation[t]);
        int o = g.Owner[t];
        if (o != 0)
        {
            var (pr, pg, pb) = Palette.Player(o);
            bool border = false;
            int x = m.X(t), y = m.Y(t);
            if (x > 0 && g.Owner[t - 1] != o || x < m.Width - 1 && g.Owner[t + 1] != o ||
                y > 0 && g.Owner[t - m.Width] != o || y < m.Height - 1 && g.Owner[t + m.Width] != o) border = true;
            if (border) { r = pr * 6 / 10; gr = pg * 6 / 10; b = pb * 6 / 10; }
            else { r = (r * 45 + pr * 55) / 100; gr = (gr * 45 + pg * 55) / 100; b = (b * 45 + pb * 55) / 100; }
        }
        rgb[t * 3] = (byte)r; rgb[t * 3 + 1] = (byte)gr; rgb[t * 3 + 2] = (byte)b;
    }
    return Png.Encode(m.Width, m.Height, rgb);
}

static class Png
{
    public static byte[] Encode(int w, int h, byte[] rgb)
    {
        using var raw = new MemoryStream();
        using (var z = new ZLibStream(raw, CompressionLevel.Optimal, true))
            for (int y = 0; y < h; y++) { z.WriteByte(0); z.Write(rgb, y * w * 3, w * 3); }
        using var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, w); BE(ihdr, 4, h); ihdr[8] = 8; ihdr[9] = 2;
        Chunk(png, "IHDR", ihdr);
        Chunk(png, "IDAT", raw.ToArray());
        Chunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, data.Length); s.Write(len);
        var td = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        s.Write(td);
        var crc = new byte[4]; BE(crc, 0, (int)Crc(td)); s.Write(crc);
    }

    static void BE(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    static uint Crc(byte[] d)
    {
        uint c = 0xFFFFFFFF;
        foreach (var x in d) { c ^= x; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; }
        return ~c;
    }
}

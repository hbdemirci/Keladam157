namespace Keladam.Core;

/// <summary>
/// Prosedürel kıta üretici (tam sayı değer gürültüsü). Gerçek haritalar ileride
/// PNG'den MapTool ile gelecek; prototip için bu yeterli.
/// </summary>
public static class MapGenerator
{
    public static GameMap Generate(int width, int height, ulong seed)
    {
        var elev = new byte[width * height];
        var terr = new byte[width * height];
        uint s1 = (uint)seed, s2 = (uint)(seed >> 32) ^ 0x5bd1e995u;

        int cx = width / 2, cy = height / 2;
        long maxD = (long)cx * cx + (long)cy * cy;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int e = Fractal(x, y, s1, 5, 160); // 0..255
            // Kenarlara doğru alçalt → kenarları deniz olan kıtalar.
            long dx = (x - cx) * (long)height / width * 2, dy = (y - cy) * 2L;
            long d2 = (dx * dx + dy * dy) * 255 / (maxD * 2);
            e = (int)FixedMath.Clamp(e + 40 - d2 * 3 / 4, 0, 255);
            int moist = Fractal(x, y, s2, 4, 110);

            int i = y * width + x;
            elev[i] = (byte)e;
            Terrain t;
            if (e < 118) t = Terrain.Water;
            else if (e > 205) t = Terrain.Mountain;
            else if (e > 178) t = Terrain.Hills;
            else if (e < 128 && moist > 150) t = Terrain.Swamp;
            else if (moist > 138) t = Terrain.Forest;
            else t = Terrain.Plains;
            terr[i] = (byte)t;
        }
        return new GameMap(width, height, terr, elev);
    }

    // Çok oktavlı değer gürültüsü, sonuç 0..255.
    private static int Fractal(int x, int y, uint seed, int octaves, int baseScale)
    {
        long sum = 0, norm = 0;
        int amp = 256, scale = baseScale;
        for (int o = 0; o < octaves; o++)
        {
            sum += (long)ValueNoise(x, y, scale, seed + (uint)o * 1013u) * amp;
            norm += amp;
            amp /= 2;
            scale = Math.Max(2, scale / 2);
        }
        return (int)(sum / norm);
    }

    private static int ValueNoise(int x, int y, int scale, uint seed)
    {
        int gx = x / scale, gy = y / scale;
        int fx = (x % scale) * 256 / scale, fy = (y % scale) * 256 / scale;
        fx = Smooth(fx);
        fy = Smooth(fy);
        int a = Hash(gx, gy, seed), b = Hash(gx + 1, gy, seed);
        int c = Hash(gx, gy + 1, seed), d = Hash(gx + 1, gy + 1, seed);
        int top = a + (b - a) * fx / 256;
        int bot = c + (d - c) * fx / 256;
        return top + (bot - top) * fy / 256;
    }

    private static int Smooth(int t) => t * t * (768 - 2 * t) / 65536; // 3t²-2t³, t∈[0,256)

    private static int Hash(int x, int y, uint seed)
    {
        uint h = (uint)x * 374761393u + (uint)y * 668265263u + seed * 2246822519u;
        h = (h ^ (h >> 13)) * 1274126177u;
        return (int)((h ^ (h >> 16)) & 255);
    }
}

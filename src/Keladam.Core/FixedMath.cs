using System.Numerics;

namespace Keladam.Core;

/// <summary>
/// Kayan nokta kullanmadan deterministik matematik. Q16 = 16 bit kesirli sabit nokta.
/// Tüm istemciler bit bit aynı sonucu üretmeli; bu yüzden Math.Pow vb. yasak.
/// </summary>
public static class FixedMath
{
    public const int One = 1 << 16;

    // 2^(2^-i) değerleri, Q30, i = 1..16 (önceden hesaplanmış sabitler).
    private static readonly long[] Exp2Table =
    {
        1518500250, 1276901417, 1170923762, 1121280436, 1097253708, 1085434106,
        1079572136, 1076653033, 1075196443, 1074468888, 1074105294, 1073923544,
        1073832680, 1073787251, 1073764537, 1073753181,
    };

    /// <summary>log2(x), Q16. x &gt;= 1.</summary>
    public static long Log2Q16(long x)
    {
        if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x));
        int ip = 63 - BitOperations.LeadingZeroCount((ulong)x);
        ulong y = ip >= 30 ? (ulong)x >> (ip - 30) : (ulong)x << (30 - ip); // [2^30, 2^31)
        long result = (long)ip << 16;
        for (int bit = 15; bit >= 0; bit--)
        {
            y = (y * y) >> 30;
            if (y >= 2UL << 30)
            {
                y >>= 1;
                result |= 1L << bit;
            }
        }
        return result;
    }

    /// <summary>2^(q / 65536), tam sayıya yuvarlanmış (aşağı).</summary>
    public static long Exp2Q16(long q)
    {
        if (q < 0) return 0;
        long ip = q >> 16;
        long frac = q & 0xFFFF;
        long r = 1L << 30;
        for (int i = 0; i < 16; i++)
        {
            if ((frac & (1L << (15 - i))) != 0) r = (r * Exp2Table[i]) >> 30;
        }
        return ip >= 30 ? r << (int)(ip - 30) : r >> (int)(30 - ip);
    }

    /// <summary>x^e, e = expNum/expDen. x &gt;= 1 için.</summary>
    public static long Pow(long x, int expNum, int expDen)
    {
        if (x <= 1) return x <= 0 ? 0 : 1;
        long log = Log2Q16(x);
        return Exp2Q16(log * expNum / expDen);
    }

    /// <summary>Tam sayı karekök (aşağı yuvarlı).</summary>
    public static long ISqrt(long x)
    {
        if (x <= 0) return 0;
        long r = (long)Math.Sqrt(x); // ilk tahmin; aşağıda tam sayıyla düzeltilir → sonuç deterministik
        while (r * r > x) r--;
        while ((r + 1) * (r + 1) <= x) r++;
        return r;
    }

    public static long Clamp(long v, long min, long max) => v < min ? min : v > max ? max : v;
}

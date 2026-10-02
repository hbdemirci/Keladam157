namespace Keladam.Core;

/// <summary>
/// Denge değerleri. Hepsi tam sayı. Formüller kendi tasarımımızdır (docs/02);
/// prototip değerleridir, oyun testleriyle ayarlanacak.
/// </summary>
public sealed class GameConfig
{
    public const int TicksPerSecond = 10;

    public int SpawnPhaseTicks { get; init; } = 15 * TicksPerSecond;
    public int SpawnRadius { get; init; } = 4;
    public long StartTroops { get; init; } = 20_000;
    public long TribeStartTroops { get; init; } = 8_000;
    public int WinLandPercent { get; init; } = 80;
    /// <summary>Bu kadar tile'ın altına inen oyuncu fatihine boyun eğer.</summary>
    public int AnnexBelowTiles { get; init; } = 60;

    public long GoldPerTick(Player p) => p.Kind == PlayerKind.Tribe ? 40 : 90 + p.TileCount / 500;

    /// <summary>Nüfus tavanı: toprakla alt-doğrusal (tile^0.55) büyür.</summary>
    public long MaxTroops(Player p)
    {
        long max = 50_000 + 2_500 * FixedMath.Pow(Math.Max(1, p.TileCount), 55, 100);
        return p.Kind == PlayerKind.Tribe ? max / 3 : max;
    }

    /// <summary>Klasik lojistik büyüme: en hızlı tavanın yarısında.</summary>
    public long TroopGrowth(Player p)
    {
        long max = MaxTroops(p);
        if (p.Troops >= max) return 0;
        long add = (p.Troops / 80 + 30) * (max - p.Troops) / max;
        if (p.Kind == PlayerKind.Tribe) add /= 2;
        return Math.Max(1, Math.Min(add, max - p.Troops));
    }

    // --- Savaş ---

    /// <summary>Arazi: (sahipsiz arazi kaybı, savaş kayıp çarpanı, zaman ağırlığı).</summary>
    public static (int NeutralLoss, int Mag, int Time) TerrainCost(Terrain t) => t switch
    {
        Terrain.Plains => (12, 50, 10),
        Terrain.Forest => (15, 62, 13),
        Terrain.Hills => (16, 66, 14),
        Terrain.Mountain => (20, 80, 18),
        Terrain.Swamp => (14, 56, 19),
        _ => (0, 0, 0),
    };

    /// <summary>Sahipsiz arazi: (saldıran kaybı, tick bütçesinden harcanan pay, binde).</summary>
    public (long Loss, int TimeCostPermille) NeutralTileCost(Terrain t, long attackTroops, int frontier, PlayerKind kind)
    {
        var (neutralLoss, _, time) = TerrainCost(t);
        long loss = kind == PlayerKind.Tribe ? neutralLoss / 2 : neutralLoss;
        long cost = time * 50_000L / (Math.Max(1, frontier) * (FixedMath.ISqrt(attackTroops) + 59));
        return (loss, (int)Math.Max(1, cost));
    }

    /// <summary>Oyuncu toprağı: (saldıran kaybı, savunan kaybı, tick bütçesinden harcanan pay, binde).</summary>
    public (long AttLoss, long DefLoss, int TimeCostPermille) PlayerTileCost(
        Terrain t, long attackTroops, Player defender, int frontier, PlayerKind attackerKind)
    {
        var (_, mag, time) = TerrainCost(t);
        if (attackerKind != PlayerKind.Tribe && defender.Kind == PlayerKind.Tribe) mag = mag * 2 / 3;

        long density = defender.Troops / Math.Max(1, defender.TileCount); // savunanın tile başı askeri
        long power = attackTroops * 1000 / Math.Max(1, defender.Troops);  // güç oranı, binde

        long attLoss = mag * (30 + density) * 1000 / (FixedMath.Clamp(power, 300, 3000) + 700) / 60;
        long cost = time * 400_000L / ((FixedMath.Clamp(power, 250, 5000) + 1000) * Math.Max(1, frontier));
        return (Math.Max(1, attLoss), density, (int)Math.Max(1, cost));
    }
}

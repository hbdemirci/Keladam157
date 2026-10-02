using Keladam.Core;
using Xunit;

namespace Keladam.Core.Tests;

public class FixedMathTests
{
    [Theory]
    [InlineData(2L, 55, 100)]
    [InlineData(1000L, 55, 100)]
    [InlineData(123_456L, 55, 100)]
    [InlineData(50_000L, 1, 2)]
    [InlineData(9_999_999L, 3, 4)]
    public void Pow_matches_math_pow_within_one_percent(long x, int num, int den)
    {
        double expected = Math.Pow(x, (double)num / den);
        long actual = FixedMath.Pow(x, num, den);
        Assert.InRange(actual, expected * 0.99 - 1, expected * 1.01 + 1);
    }

    [Fact]
    public void ISqrt_is_exact()
    {
        for (long x = 0; x < 5000; x++)
        {
            long r = FixedMath.ISqrt(x);
            Assert.True(r * r <= x && (r + 1) * (r + 1) > x);
        }
    }
}

public class GameTests
{
    private static Game NewGame(ulong seed = 42, int kingdoms = 6, int tribes = 10)
    {
        var map = MapGenerator.Generate(400, 240, seed);
        return new Game(map, new GameConfig(), seed, NameGen.SinglePlayerRoster("Sen", kingdoms, tribes, seed));
    }

    private static int FirstFreeLand(Game g)
    {
        for (int t = 0; t < g.Map.Size; t++)
            if (g.Map.IsLand(t) && g.Owner[t] == 0) return t;
        throw new InvalidOperationException();
    }

    private static void Run(LocalSession s, int ticks)
    {
        for (int i = 0; i < ticks; i++) s.Advance();
    }

    [Fact]
    public void Map_has_land_and_water()
    {
        var map = MapGenerator.Generate(400, 240, 7);
        Assert.InRange(map.LandTileCount, map.Size / 5, map.Size * 4 / 5);
    }

    [Fact]
    public void Spawn_claims_tiles_and_ai_spawns()
    {
        var g = NewGame();
        var s = new LocalSession(g);
        s.Advance(); // tick 0: AI doğar
        s.Submit(new SpawnIntent(1, FirstFreeLand(g)));
        s.Advance();
        Assert.True(g.PlayerById(1).TileCount > 10);
        Assert.All(g.Players.Skip(1), p => Assert.True(p.HasSpawned));
    }

    [Fact]
    public void Unspawned_human_is_placed_at_end_of_spawn_phase()
    {
        var g = NewGame();
        Run(new LocalSession(g), g.Config.SpawnPhaseTicks);
        Assert.True(g.PlayerById(1).HasSpawned);
    }

    [Fact]
    public void Neutral_expansion_grows_territory()
    {
        var g = NewGame(kingdoms: 0, tribes: 0);
        var s = new LocalSession(g);
        s.Submit(new SpawnIntent(1, FirstFreeLand(g)));
        Run(s, g.Config.SpawnPhaseTicks);
        int before = g.PlayerById(1).TileCount;
        s.Submit(new SetAttackRatioIntent(1, 500));
        s.Submit(new AttackIntent(1, 0));
        Run(s, 50);
        Assert.True(g.PlayerById(1).TileCount > before * 3, $"{before} -> {g.PlayerById(1).TileCount}");
    }

    [Fact]
    public void Troops_never_exceed_cap()
    {
        var g = NewGame(kingdoms: 0, tribes: 0);
        var s = new LocalSession(g);
        Run(s, g.Config.SpawnPhaseTicks + 3000);
        var p = g.PlayerById(1);
        Assert.True(p.Troops <= g.Config.MaxTroops(p));
        Assert.True(p.Troops > g.Config.MaxTroops(p) * 9 / 10);
    }

    [Fact]
    public void Attack_without_border_returns_troops()
    {
        var g = NewGame();
        var s = new LocalSession(g);
        Run(s, g.Config.SpawnPhaseTicks);
        var me = g.PlayerById(1);
        var far = g.Players.First(p => p.Id != 1 && !p.BorderTiles.Any(b => NeighborOwnedBy(g, b, 1)));
        long before = me.Troops;
        s.Submit(new AttackIntent(1, far.Id));
        s.Advance();
        Assert.DoesNotContain(g.Attacks, a => a.Attacker == me);
        Assert.True(me.Troops >= before);
    }

    [Fact]
    public void Strong_attacker_conquers_weak_neighbor()
    {
        var map = MapGenerator.Generate(200, 120, 3);
        var g = new Game(map, new GameConfig(), 3,
            new[] { new PlayerSetup("A", PlayerKind.Human), new PlayerSetup("B", PlayerKind.Human) });
        var s = new LocalSession(g);
        int a = FirstFreeLand(g);
        s.Submit(new SpawnIntent(1, a));
        s.Advance();
        // B'yi A'nın hemen yanına yerleştir.
        int b = -1;
        for (int t = a; t < map.Size; t++)
            if (map.IsLand(t) && g.Owner[t] == 0 && t - a > 9) { b = t; break; }
        s.Submit(new SpawnIntent(2, b));
        Run(s, g.Config.SpawnPhaseTicks);

        var pa = g.PlayerById(1);
        var pb = g.PlayerById(2);
        if (!pa.BorderTiles.Any(t => NeighborOwnedBy(g, t, 2)))
        {
            // Komşu değillerse önce boş araziye yayıl.
            s.Submit(new SetAttackRatioIntent(1, 300));
            s.Submit(new AttackIntent(1, 0));
            Run(s, 100);
        }
        Assert.True(pa.BorderTiles.Any(t => NeighborOwnedBy(g, t, 2)), "A and B should be neighbors");

        s.Submit(new SetAttackRatioIntent(1, 1000));
        s.Submit(new AttackIntent(1, 2));
        Run(s, 300);
        Assert.False(pb.IsAlive);
    }

    [Fact]
    public void Same_seed_and_turns_give_identical_state()
    {
        ulong Play()
        {
            var g = NewGame(seed: 99, kingdoms: 8, tribes: 15);
            var s = new LocalSession(g);
            s.Submit(new SpawnIntent(1, FirstFreeLand(g)));
            Run(s, g.Config.SpawnPhaseTicks);
            s.Submit(new SetAttackRatioIntent(1, 400));
            s.Submit(new AttackIntent(1, 0));
            Run(s, 1500);
            return g.StateHash();
        }
        Assert.Equal(Play(), Play());
    }

    [Fact]
    public void Replaying_history_reproduces_state()
    {
        var g = NewGame(seed: 5);
        var s = new LocalSession(g);
        s.Submit(new SpawnIntent(1, FirstFreeLand(g)));
        Run(s, g.Config.SpawnPhaseTicks);
        s.Submit(new AttackIntent(1, 0));
        Run(s, 800);

        var g2 = NewGame(seed: 5);
        foreach (var turn in s.History) g2.ExecuteTurn(turn);
        Assert.Equal(g.StateHash(), g2.StateHash());
    }

    [Fact]
    public void Ai_only_game_progresses_and_eliminates_players()
    {
        var g = NewGame(seed: 11, kingdoms: 8, tribes: 20);
        Run(new LocalSession(g), 6000);
        Assert.Contains(g.Players, p => !p.IsAlive);
        Assert.True(g.Players.Where(p => p.IsAlive).Sum(p => p.TileCount) > g.Map.LandTileCount / 2);
    }

    private static bool NeighborOwnedBy(Game g, int tile, ushort owner)
    {
        Span<int> nb = stackalloc int[4];
        int n = g.Map.Neighbors4(tile, nb);
        for (int i = 0; i < n; i++)
            if (g.Owner[nb[i]] == owner) return true;
        return false;
    }
}

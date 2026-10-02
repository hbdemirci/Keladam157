namespace Keladam.Core;

public sealed record PlayerSetup(string Name, PlayerKind Kind);

/// <summary>
/// Deterministik oyun durumu. Tek giriş noktası <see cref="ExecuteTurn"/>:
/// aynı harita + tohum + turn dizisi → bit bit aynı durum (lockstep'in temeli).
/// </summary>
public sealed class Game
{
    public GameMap Map { get; }
    public GameConfig Config { get; }
    /// <summary>Tile sahibi; 0 = sahipsiz.</summary>
    public ushort[] Owner { get; }
    public IReadOnlyList<Player> Players => _players;
    public IReadOnlyList<Attack> Attacks => _attacks;
    public int Tick { get; private set; }
    public Player? Winner { get; private set; }
    public bool IsSpawnPhase => Tick < Config.SpawnPhaseTicks;

    /// <summary>Son tüketimden beri sahibi değişen tile'lar (render için).</summary>
    public List<int> DirtyTiles { get; } = new();
    /// <summary>Arayüze gösterilecek olaylar (fetih vb.). Arayüz okuyup temizler.</summary>
    public List<string> Events { get; } = new();

    private readonly List<Player> _players = new();
    private readonly List<Attack> _attacks = new();
    private readonly Rng _rng;
    private readonly int[] _nbuf = new int[4];
    private readonly int[] _nbuf2 = new int[4];
    private bool _annexing;

    public Game(GameMap map, GameConfig config, ulong seed, IEnumerable<PlayerSetup> players)
    {
        Map = map;
        Config = config;
        Owner = new ushort[map.Size];
        _rng = new Rng(seed);
        ushort id = 1;
        foreach (var s in players)
        {
            var p = new Player(id++, s.Name, s.Kind)
            {
                Troops = s.Kind == PlayerKind.Tribe ? config.TribeStartTroops : config.StartTroops,
                AttackRatioPermille = s.Kind == PlayerKind.Tribe ? 150 : 250,
            };
            _players.Add(p);
        }
    }

    public Player PlayerById(ushort id) => _players[id - 1];
    public Player? OwnerOf(int tile) => Owner[tile] == 0 ? null : _players[Owner[tile] - 1];

    // ------------------------------------------------------------------ turn

    public void ExecuteTurn(Turn turn)
    {
        if (turn.Tick != Tick) throw new InvalidOperationException($"turn {turn.Tick} != tick {Tick}");
        if (Winner == null)
        {
            foreach (var intent in turn.Intents) Apply(intent);
            Step();
        }
        Tick++;
    }

    private void Apply(Intent intent)
    {
        if (intent.PlayerId == 0 || intent.PlayerId > _players.Count) return;
        var p = PlayerById(intent.PlayerId);
        if (!p.IsAlive) return;
        switch (intent)
        {
            case SpawnIntent s when IsSpawnPhase:
                if (s.Tile >= 0 && s.Tile < Map.Size && Map.IsLand(s.Tile) &&
                    (Owner[s.Tile] == 0 || Owner[s.Tile] == p.Id))
                    Spawn(p, s.Tile);
                break;
            case AttackIntent a when !IsSpawnPhase && p.HasSpawned:
                LaunchAttack(p, a.TargetId);
                break;
            case SetAttackRatioIntent r:
                p.AttackRatioPermille = (int)FixedMath.Clamp(r.Permille, 10, 1000);
                break;
        }
    }

    private void Step()
    {
        if (Tick == 0) SpawnAi();
        if (IsSpawnPhase)
        {
            if (Tick == Config.SpawnPhaseTicks - 1)
                foreach (var p in _players)
                    if (!p.HasSpawned) SpawnRandom(p);
            return;
        }

        foreach (var p in _players)
        {
            if (!p.IsAlive || !p.HasSpawned) continue;
            p.Troops += Config.TroopGrowth(p);
            p.Gold += Config.GoldPerTick(p);
        }

        foreach (var p in _players)
            if (p.IsAlive && p.Kind != PlayerKind.Human && Tick >= p.NextAiTick)
                AiDecide(p);

        // Saldırılar oluşturulma sırasıyla işlenir (determinizm).
        for (int i = 0; i < _attacks.Count; i++)
            if (_attacks[i].Active) StepAttack(_attacks[i]);
        _attacks.RemoveAll(a => !a.Active);

        CheckWinner();
    }

    // ----------------------------------------------------------------- spawn

    private void Spawn(Player p, int center)
    {
        foreach (var t in p.SpawnTiles)
            if (Owner[t] == p.Id) SetOwner(t, 0);
        p.SpawnTiles.Clear();

        int r = Config.SpawnRadius, cx = Map.X(center), cy = Map.Y(center);
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (dx * dx + dy * dy > r * r) continue;
            int x = cx + dx, y = cy + dy;
            if (!Map.InBounds(x, y)) continue;
            int t = Map.Index(x, y);
            if (!Map.IsLand(t) || Owner[t] != 0) continue;
            SetOwner(t, p.Id);
            p.SpawnTiles.Add(t);
        }
        p.HasSpawned = p.TileCount > 0;
    }

    private void SpawnAi()
    {
        foreach (var p in _players)
            if (p.Kind != PlayerKind.Human) SpawnRandom(p);
    }

    private void SpawnRandom(Player p)
    {
        int minDist = Math.Max(Map.Width, Map.Height) / 12;
        for (int attempt = 0; attempt < 400; attempt++)
        {
            int t = _rng.Next(0, Map.Size);
            if (!Map.IsLand(t) || Owner[t] != 0) continue;
            if (attempt < 300 && !FarFromOthers(t, minDist * (300 - attempt) / 300)) continue;
            Spawn(p, t);
            if (p.HasSpawned) return;
        }
    }

    private bool FarFromOthers(int tile, int minDist)
    {
        int x = Map.X(tile), y = Map.Y(tile);
        foreach (var o in _players)
        {
            if (o.TileCount == 0) continue;
            long ox = o.SumX / o.TileCount - x, oy = o.SumY / o.TileCount - y;
            if (ox * ox + oy * oy < (long)minDist * minDist) return false;
        }
        return true;
    }

    // ---------------------------------------------------------------- attack

    private void LaunchAttack(Player attacker, ushort targetId)
    {
        if (targetId == attacker.Id || targetId > _players.Count) return;
        Player? target = targetId == 0 ? null : PlayerById(targetId);
        if (target != null && !target.IsAlive) return;

        long troops = attacker.Troops * attacker.AttackRatioPermille / 1000;
        if (troops < 1) return;
        attacker.Troops -= troops;

        // Karşı saldırı: hedef bize saldırıyorsa iki cephe önce birbirini eritir.
        if (target != null)
        {
            var counter = _attacks.Find(a => a.Active && a.Attacker == target && a.Target == attacker);
            if (counter != null)
            {
                long m = Math.Min(counter.Troops, troops);
                counter.Troops -= m;
                troops -= m;
                if (counter.Troops < 1) counter.Active = false;
                if (troops < 1) return;
            }
        }

        var existing = _attacks.Find(a => a.Active && a.Attacker == attacker && a.Target == target);
        if (existing != null)
        {
            existing.Troops += troops;
            return;
        }

        var attack = new Attack(attacker, target, troops);
        RefillFrontier(attack);
        if (attack.Queue.Count == 0)
        {
            attacker.Troops += troops; // komşu değil → asker geri döner
            return;
        }
        _attacks.Add(attack);
    }

    private void RefillFrontier(Attack attack)
    {
        Span<int> nb = _nbuf;
        foreach (var b in attack.Attacker.Border)
        {
            int n = Map.Neighbors4(b, nb);
            for (int i = 0; i < n; i++)
                if (Owner[nb[i]] == attack.TargetId && Map.IsLand(nb[i]))
                    Enqueue(attack, nb[i]);
        }
    }

    private void Enqueue(Attack attack, int tile)
    {
        if (!attack.Queued.Add(tile)) return;
        Span<int> nb = _nbuf2;
        int n = Map.Neighbors4(tile, nb), mine = 0;
        for (int i = 0; i < n; i++)
            if (Owner[nb[i]] == attack.Attacker.Id) mine++;
        var (_, _, time) = GameConfig.TerrainCost(Map.TerrainAt(tile));
        // Düşük = önce. Çevrili cepler önce dolar, engebeli arazi geride kalır, biraz rastgelelik.
        long prio = Tick * 64L + _rng.Next(0, 24) + time * 2 - mine * 12;
        attack.Queue.Enqueue(tile, (prio << 24) | (attack.Seq++ & 0xFFFFFF));
    }

    private void StepAttack(Attack a)
    {
        var target = a.Target;
        if (target != null && !target.IsAlive)
        {
            EndAttack(a);
            return;
        }

        int frontier = a.FrontierSize + _rng.Next(0, 4);
        int budget = 1000;
        Span<int> nb = _nbuf;
        bool refilled = false;

        while (budget > 0)
        {
            if (a.Troops < 1)
            {
                a.Troops = 0;
                EndAttack(a);
                return;
            }
            if (a.Queue.Count == 0)
            {
                if (refilled) { EndAttack(a); return; }
                a.Queued.Clear();
                RefillFrontier(a);
                refilled = true;
                continue;
            }

            int tile = a.Queue.Dequeue();
            a.Queued.Remove(tile);
            if (Owner[tile] != a.TargetId || !Map.IsLand(tile)) continue;
            bool adjacent = false;
            int n = Map.Neighbors4(tile, nb);
            for (int i = 0; i < n && !adjacent; i++) adjacent = Owner[nb[i]] == a.Attacker.Id;
            if (!adjacent) continue;

            var terrain = Map.TerrainAt(tile);
            if (target == null)
            {
                var (loss, cost) = Config.NeutralTileCost(terrain, a.Troops, frontier, a.Attacker.Kind);
                a.Troops -= loss;
                budget -= cost;
            }
            else
            {
                var (attLoss, defLoss, cost) = Config.PlayerTileCost(terrain, a.Troops, target, frontier, a.Attacker.Kind);
                a.Troops -= attLoss;
                target.Troops = Math.Max(0, target.Troops - defLoss);
                budget -= cost;
            }

            SetOwner(tile, a.Attacker.Id);
            n = Map.Neighbors4(tile, nb);
            for (int i = 0; i < n; i++)
                if (Owner[nb[i]] == a.TargetId && Map.IsLand(nb[i])) Enqueue(a, nb[i]);

            if (target != null && target.TileCount < Config.AnnexBelowTiles)
            {
                Annex(a.Attacker, target);
                EndAttack(a);
                return;
            }
        }
    }

    private void EndAttack(Attack a)
    {
        if (!a.Active) return;
        a.Active = false;
        a.Attacker.Troops += Math.Max(0, a.Troops);
        a.Troops = 0;
    }

    /// <summary>Kalan topraklar, asker ve altının bir kısmı fatihe geçer.</summary>
    private void Annex(Player winner, Player loser)
    {
        _annexing = true;
        for (int t = 0; t < Owner.Length; t++)
            if (Owner[t] == loser.Id) SetOwner(t, winner.Id);
        _annexing = false;
        winner.Gold += loser.Gold;
        winner.Troops += loser.Troops / 2;
        loser.Gold = 0;
        loser.Troops = 0;
        Kill(loser);
        Events.Add($"{winner.Name}, {loser.Name} topraklarını fethetti!");
    }

    private void Kill(Player p)
    {
        p.IsAlive = false;
        foreach (var a in _attacks)
            if (a.Attacker == p) { a.Active = false; a.Troops = 0; }
    }

    // ----------------------------------------------------------------- tiles

    private void SetOwner(int tile, ushort newOwner)
    {
        ushort old = Owner[tile];
        if (old == newOwner) return;
        int x = Map.X(tile), y = Map.Y(tile);
        if (old != 0)
        {
            var op = _players[old - 1];
            op.TileCount--;
            op.SumX -= x;
            op.SumY -= y;
            op.Border.Remove(tile);
            if (op.TileCount == 0 && op.HasSpawned && !IsSpawnPhase && op.IsAlive && !_annexing)
            {
                Kill(op);
                Events.Add($"{op.Name} yıkıldı.");
            }
        }
        Owner[tile] = newOwner;
        if (newOwner != 0)
        {
            var np = _players[newOwner - 1];
            np.TileCount++;
            np.SumX += x;
            np.SumY += y;
        }
        DirtyTiles.Add(tile);

        UpdateBorder(tile);
        Span<int> nb = stackalloc int[4];
        int n = Map.Neighbors4(tile, nb);
        for (int i = 0; i < n; i++) UpdateBorder(nb[i]);
    }

    private void UpdateBorder(int tile)
    {
        ushort o = Owner[tile];
        if (o == 0) return;
        Span<int> nb = stackalloc int[4];
        int n = Map.Neighbors4(tile, nb);
        bool border = n < 4;
        for (int i = 0; i < n && !border; i++) border = Owner[nb[i]] != o;
        var p = _players[o - 1];
        if (border) p.Border.Add(tile);
        else p.Border.Remove(tile);
    }

    // -------------------------------------------------------------------- AI

    private void AiDecide(Player p)
    {
        bool tribe = p.Kind == PlayerKind.Tribe;
        p.NextAiTick = Tick + (tribe ? _rng.Next(30, 80) : _rng.Next(15, 45));

        long max = Config.MaxTroops(p);
        if (p.Troops * 100 < max * (tribe ? 35 : 50)) return;

        // Komşuları say (sahipsiz arazi = 0).
        bool neutral = false;
        Player? weakest = null;
        Span<int> nb = _nbuf;
        foreach (var b in p.Border)
        {
            int n = Map.Neighbors4(b, nb);
            for (int i = 0; i < n; i++)
            {
                int t = nb[i];
                if (!Map.IsLand(t)) continue;
                ushort o = Owner[t];
                if (o == p.Id) continue;
                if (o == 0) { neutral = true; continue; }
                var q = _players[o - 1];
                if (weakest == null || q.Troops < weakest.Troops) weakest = q;
            }
        }

        if (neutral)
        {
            if (!_attacks.Exists(a => a.Active && a.Attacker == p && a.Target == null))
                LaunchAttack(p, 0);
            return;
        }
        if (weakest == null) return;
        if (tribe && !_rng.Chance(250)) return;
        // Krallık: hedef zayıfsa ya da bir beylikse saldır.
        if (weakest.Kind == PlayerKind.Tribe || p.Troops * p.AttackRatioPermille / 1000 * 2 > weakest.Troops)
            LaunchAttack(p, weakest.Id);
    }

    // ------------------------------------------------------------------- win

    private void CheckWinner()
    {
        foreach (var p in _players)
        {
            if (p.IsAlive && (long)p.TileCount * 100 >= (long)Map.LandTileCount * Config.WinLandPercent)
            {
                Winner = p;
                Events.Add($"{p.Name} diyarın hâkimi oldu!");
                return;
            }
        }
    }

    // ------------------------------------------------------------------ hash

    /// <summary>Durum özeti; istemciler arası senkron kontrolü (desync) için.</summary>
    public ulong StateHash()
    {
        ulong h = 14695981039346656037UL;
        void Mix(long v) { h ^= (ulong)v; h *= 1099511628211UL; }
        Mix(Tick);
        foreach (var o in Owner) Mix(o);
        foreach (var p in _players)
        {
            Mix(p.Troops); Mix(p.Gold); Mix(p.TileCount); Mix(p.IsAlive ? 1 : 0);
        }
        foreach (var a in _attacks) { Mix(a.Attacker.Id); Mix(a.TargetId); Mix(a.Troops); }
        Mix((long)_rng.State);
        return h;
    }
}

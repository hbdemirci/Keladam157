namespace Keladam.Core;

public enum PlayerKind : byte
{
    Human = 0,
    /// <summary>Zayıf, saldırgan olmayan küçük beylikler (eski adıyla bot).</summary>
    Tribe = 1,
    /// <summary>Yapay zekâ krallıkları.</summary>
    Kingdom = 2,
}

public sealed class Player
{
    public ushort Id { get; }
    public string Name { get; }
    public PlayerKind Kind { get; }

    public long Troops { get; internal set; }
    public long Gold { get; internal set; }
    /// <summary>Saldırıya gönderilecek ordu oranı, binde.</summary>
    public int AttackRatioPermille { get; internal set; } = 200;

    public int TileCount { get; internal set; }
    public bool HasSpawned { get; internal set; }
    public bool IsAlive { get; internal set; } = true;

    // Ad etiketi yerleşimi için koordinat toplamları.
    public long SumX { get; internal set; }
    public long SumY { get; internal set; }

    /// <summary>Komşusu başka sahipte olan tile'lar (cephe başlangıcı için).</summary>
    internal readonly HashSet<int> Border = new();
    /// <summary>Doğuşta alınan tile'lar (doğuş evresinde yer değiştirmek için).</summary>
    internal readonly List<int> SpawnTiles = new();

    internal int NextAiTick;

    public Player(ushort id, string name, PlayerKind kind)
    {
        Id = id;
        Name = name;
        Kind = kind;
    }

    public IReadOnlyCollection<int> BorderTiles => Border;
}

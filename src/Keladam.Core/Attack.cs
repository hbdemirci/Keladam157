namespace Keladam.Core;

/// <summary>
/// Bir cephe: saldıran, hedef (null = sahipsiz arazi) ve cepheye ayrılmış ordu.
/// Hedef tile'lar öncelik kuyruğunda; her tick bir zaman bütçesi harcanarak alınır.
/// </summary>
public sealed class Attack
{
    public Player Attacker { get; }
    public Player? Target { get; }
    public long Troops { get; internal set; }
    public bool Active { get; internal set; } = true;

    internal readonly PriorityQueue<int, long> Queue = new();
    internal readonly HashSet<int> Queued = new();
    internal long Seq;

    public Attack(Player attacker, Player? target, long troops)
    {
        Attacker = attacker;
        Target = target;
        Troops = troops;
    }

    public ushort TargetId => Target?.Id ?? 0;
    public int FrontierSize => Queued.Count;
}

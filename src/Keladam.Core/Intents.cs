namespace Keladam.Core;

/// <summary>
/// Oyuncu niyeti. Ağ üzerinden yalnızca bunlar gider; host bunları tick'e göre
/// Turn olarak paketler, herkes aynı sırayla uygular.
/// </summary>
public abstract record Intent(ushort PlayerId);

/// <summary>Doğuş evresinde başlangıç yeri seçimi.</summary>
public sealed record SpawnIntent(ushort PlayerId, int Tile) : Intent(PlayerId);

/// <summary>Hedef oyuncuya (0 = sahipsiz arazi) mevcut ordu oranıyla saldır.</summary>
public sealed record AttackIntent(ushort PlayerId, ushort TargetId) : Intent(PlayerId);

/// <summary>Saldırı oranını değiştir (binde, 10..1000).</summary>
public sealed record SetAttackRatioIntent(ushort PlayerId, int Permille) : Intent(PlayerId);

/// <summary>Bir tick'te uygulanacak niyetler. Sıra önemlidir.</summary>
public sealed record Turn(int Tick, IReadOnlyList<Intent> Intents)
{
    public static Turn Empty(int tick) => new(tick, Array.Empty<Intent>());
}

namespace Keladam.Core;

/// <summary>
/// Tek oyunculu "host". Çok oyunculuda bunun yerini Steam lobisi üzerinden turn
/// dağıtan LockstepHost alacak (M2); oyun tarafı aynı arayüzü kullanır.
/// </summary>
public sealed class LocalSession
{
    private readonly List<Intent> _pending = new();

    public Game Game { get; }
    /// <summary>Uygulanan tüm turn'ler (replay + determinizm testi için).</summary>
    public List<Turn> History { get; } = new();

    public LocalSession(Game game) => Game = game;

    public void Submit(Intent intent) => _pending.Add(intent);

    public void Advance()
    {
        var turn = new Turn(Game.Tick, _pending.ToArray());
        _pending.Clear();
        History.Add(turn);
        Game.ExecuteTurn(turn);
    }
}

namespace Keladam.Core;

/// <summary>Deterministik büyülü orta çağ diyar adları.</summary>
public static class NameGen
{
    private static readonly string[] Start =
        { "Kara", "Kızıl", "Demir", "Gümüş", "Ak", "Gök", "Yeşil", "Altın", "Kemik", "Gölge", "Ateş", "Buz", "Yıldız", "Kurt", "Ejder" };
    private static readonly string[] End =
        { "kale", "taç", "koru", "diyar", "yurt", "burç", "pınar", "vadi", "hisar", "orman", "dağ", "ova", "sur", "geçit" };
    private static readonly string[] KingdomTitle = { "Krallığı", "Beyliği", "Hanedanı", "Ordosu", "Loncası", "Prensliği" };
    private static readonly string[] TribeTitle = { "Köyü", "Obası", "Kampı", "Çetesi" };

    public static string Kingdom(Rng rng) => $"{Start[rng.Next(0, Start.Length)]}{End[rng.Next(0, End.Length)]} {KingdomTitle[rng.Next(0, KingdomTitle.Length)]}";

    public static string Tribe(Rng rng) => $"{Start[rng.Next(0, Start.Length)]}{End[rng.Next(0, End.Length)]} {TribeTitle[rng.Next(0, TribeTitle.Length)]}";

    /// <summary>Tek oyunculu maç için oyuncu listesi: önce insan, sonra krallıklar ve beylikler.</summary>
    public static List<PlayerSetup> SinglePlayerRoster(string humanName, int kingdoms, int tribes, ulong seed)
    {
        var rng = new Rng(seed ^ 0xA5A5_5A5A_1234_4321UL);
        var list = new List<PlayerSetup> { new(humanName, PlayerKind.Human) };
        var used = new HashSet<string>();
        for (int i = 0; i < kingdoms; i++) list.Add(new(Unique(() => Kingdom(rng), used), PlayerKind.Kingdom));
        for (int i = 0; i < tribes; i++) list.Add(new(Unique(() => Tribe(rng), used), PlayerKind.Tribe));
        return list;
    }

    private static string Unique(Func<string> make, HashSet<string> used)
    {
        for (int i = 0; i < 20; i++)
        {
            var n = make();
            if (used.Add(n)) return n;
        }
        var fallback = make() + " " + used.Count;
        used.Add(fallback);
        return fallback;
    }
}

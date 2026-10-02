using Godot;
using Keladam.Core;

namespace Keladam;

/// <summary>Ekran arayüzü: kaynaklar, saldırı oranı, sıralama, olay günlüğü, imleç bilgisi.</summary>
public partial class Hud : CanvasLayer
{
    public event Action<int>? RatioChanged;

    private Label _stats = null!, _board = null!, _phase = null!, _log = null!, _hover = null!, _ratioLabel = null!;
    private HSlider _ratio = null!;
    private readonly List<string> _events = new();
    private bool _settingRatio;

    private static readonly string[] TerrainNames = { "Deniz", "Ova", "Orman", "Tepe", "Dağ", "Bataklık" };

    public override void _Ready()
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        _stats = Panel(root, Control.LayoutPreset.TopLeft, new Vector2(12, 12));
        _board = Panel(root, Control.LayoutPreset.TopRight, new Vector2(-12, 12));
        _board.GrowHorizontal = Control.GrowDirection.Begin;
        _log = Panel(root, Control.LayoutPreset.BottomLeft, new Vector2(12, -12));
        _log.GrowVertical = Control.GrowDirection.Begin;

        _phase = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        _phase.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _phase.Position += new Vector2(0, 16);
        _phase.GrowHorizontal = Control.GrowDirection.Both;
        _phase.AddThemeFontSizeOverride("font_size", 26);
        _phase.AddThemeColorOverride("font_outline_color", Colors.Black);
        _phase.AddThemeConstantOverride("outline_size", 6);
        root.AddChild(_phase);

        _hover = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        _hover.AddThemeColorOverride("font_outline_color", Colors.Black);
        _hover.AddThemeConstantOverride("outline_size", 4);
        root.AddChild(_hover);

        var bar = new HBoxContainer();
        bar.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        bar.GrowHorizontal = Control.GrowDirection.Both;
        bar.GrowVertical = Control.GrowDirection.Begin;
        bar.Position += new Vector2(-200, -16);
        bar.CustomMinimumSize = new Vector2(400, 0);
        _ratioLabel = new Label { CustomMinimumSize = new Vector2(150, 0) };
        _ratio = new HSlider { MinValue = 1, MaxValue = 100, Step = 1, CustomMinimumSize = new Vector2(250, 24) };
        _ratio.ValueChanged += v =>
        {
            _ratioLabel.Text = $"Saldırı oranı: %{(int)v}";
            if (!_settingRatio) RatioChanged?.Invoke((int)v * 10);
        };
        bar.AddChild(_ratioLabel);
        bar.AddChild(_ratio);
        root.AddChild(bar);
    }

    private static Label Panel(Control root, Control.LayoutPreset preset, Vector2 offset)
    {
        var l = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        var sb = new StyleBoxFlat { BgColor = new Color(0.08f, 0.06f, 0.05f, 0.72f), BorderColor = new Color(0.6f, 0.48f, 0.25f) };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(4);
        sb.SetContentMarginAll(8);
        l.AddThemeStyleboxOverride("normal", sb);
        l.SetAnchorsPreset(preset);
        l.Position += offset;
        root.AddChild(l);
        return l;
    }

    public void SetRatio(int permille)
    {
        _settingRatio = true;
        _ratio.Value = permille / 10;
        _ratioLabel.Text = $"Saldırı oranı: %{permille / 10}";
        _settingRatio = false;
    }

    public void AddEvent(string e)
    {
        _events.Add(e);
        if (_events.Count > 6) _events.RemoveAt(0);
        _log.Text = string.Join("\n", _events);
    }

    public void Refresh(Game g, ushort me, int hoverTile, bool paused, int speed)
    {
        var p = g.PlayerById(me);
        long max = g.Config.MaxTroops(p);
        long front = g.Attacks.Where(a => a.Attacker == p).Sum(a => a.Troops);
        _stats.Text =
            $"{p.Name}\n" +
            $"Asker: {Fmt.Num(p.Troops)} / {Fmt.Num(max)}  (+{Fmt.Num(g.Config.TroopGrowth(p) * GameConfig.TicksPerSecond)}/sn)\n" +
            $"Cephede: {Fmt.Num(front)}\n" +
            $"Altın: {Fmt.Num(p.Gold)}\n" +
            $"Toprak: {p.TileCount} (%{(g.Map.LandTileCount == 0 ? 0 : p.TileCount * 1000L / g.Map.LandTileCount / 10.0):0.0})";

        var top = g.Players.Where(x => x.IsAlive).OrderByDescending(x => x.TileCount).Take(10).ToList();
        _board.Text = "Diyarlar\n" + string.Join("\n", top.Select((x, i) =>
            $"{i + 1,2}. {(x.Id == me ? "► " : "")}{x.Name}  {x.TileCount * 100 / Math.Max(1, g.Map.LandTileCount)}%"));

        string speedText = speed > 1 ? $"  ×{speed}" : "";
        if (g.Winner != null)
            _phase.Text = g.Winner.Id == me ? "ZAFER! Diyarın hâkimi sensin. (F5: yeni harita)" : $"{g.Winner.Name} kazandı. (F5: yeni harita)";
        else if (!p.IsAlive)
            _phase.Text = "Diyarın düştü. (F5: yeni harita)";
        else if (g.IsSpawnPhase)
            _phase.Text = $"Başlangıç yerini seç — {(g.Config.SpawnPhaseTicks - g.Tick) / GameConfig.TicksPerSecond + 1} sn";
        else
            _phase.Text = paused ? "DURAKLATILDI (P)" : speedText;

        if (hoverTile >= 0)
        {
            var o = g.OwnerOf(hoverTile);
            string terr = TerrainNames[(int)g.Map.TerrainAt(hoverTile)];
            _hover.Text = o == null ? terr : $"{o.Name}\n{terr} · Asker {Fmt.Num(o.Troops)} · Toprak {o.TileCount}";
            _hover.Position = GetViewport().GetMousePosition() + new Vector2(18, 18);
            _hover.Visible = true;
        }
        else _hover.Visible = false;
    }
}

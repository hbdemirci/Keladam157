using Godot;
using Keladam.Core;

namespace Keladam;

/// <summary>
/// M0 prototip: tek oyunculu maç. Simülasyon Keladam.Core'da; burası sadece
/// render, girdi ve arayüz. Simülasyon 10 tick/sn sabit adımla ilerler.
/// </summary>
public partial class Main : Node2D
{
    private const int MapWidth = 1200, MapHeight = 700;
    private const int PaletteSize = 1024;

    private Game _game = null!;
    private LocalSession _session = null!;
    private ushort _me = 1;

    private Sprite2D _mapSprite = null!;
    private ShaderMaterial _mat = null!;
    private Image _ownerImage = null!;
    private ImageTexture _ownerTex = null!;
    private byte[] _ownerBytes = null!;

    private CameraController _camera = null!;
    private NameLayer _names = null!;
    private Hud _hud = null!;

    private double _acc;
    private int _speed = 1;
    private bool _paused;

    public override void _Ready()
    {
        ulong seed = (ulong)GD.Randi() | ((ulong)GD.Randi() << 32);
        StartGame(seed);
    }

    private void StartGame(ulong seed)
    {
        foreach (var child in GetChildren()) child.QueueFree();

        var map = MapGenerator.Generate(MapWidth, MapHeight, seed);
        _game = new Game(map, new GameConfig(), seed, NameGen.SinglePlayerRoster("Sen", 14, 40, seed));
        _session = new LocalSession(_game);

        BuildMapSprite(map);

        _names = new NameLayer { Game = _game };
        AddChild(_names);

        _camera = new CameraController();
        AddChild(_camera);
        _camera.Fit(new Vector2(map.Width, map.Height));

        _hud = new Hud();
        AddChild(_hud);
        _hud.RatioChanged += permille => _session.Submit(new SetAttackRatioIntent(_me, permille));
        _hud.SetRatio(_game.PlayerById(_me).AttackRatioPermille);
        _acc = 0;
    }

    private void BuildMapSprite(GameMap map)
    {
        var terrain = new byte[map.Size * 3];
        for (int t = 0; t < map.Size; t++)
        {
            var (r, g, b) = Palette.TerrainColor(map.TerrainAt(t), map.Elevation[t]);
            terrain[t * 3] = (byte)r;
            terrain[t * 3 + 1] = (byte)g;
            terrain[t * 3 + 2] = (byte)b;
        }
        var terrainTex = ImageTexture.CreateFromImage(
            Image.CreateFromData(map.Width, map.Height, false, Image.Format.Rgb8, terrain));

        _ownerBytes = new byte[map.Size * 2];
        _ownerImage = Image.CreateFromData(map.Width, map.Height, false, Image.Format.Rg8, _ownerBytes);
        _ownerTex = ImageTexture.CreateFromImage(_ownerImage);

        var pal = new byte[PaletteSize * 4];
        for (int id = 1; id < PaletteSize; id++)
        {
            var (r, g, b) = Palette.Player(id);
            pal[id * 4] = (byte)r;
            pal[id * 4 + 1] = (byte)g;
            pal[id * 4 + 2] = (byte)b;
            pal[id * 4 + 3] = 255;
        }
        var palTex = ImageTexture.CreateFromImage(
            Image.CreateFromData(PaletteSize, 1, false, Image.Format.Rgba8, pal));

        _mat = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/territory.gdshader") };
        _mat.SetShaderParameter("owner_tex", _ownerTex);
        _mat.SetShaderParameter("palette_tex", palTex);
        _mat.SetShaderParameter("map_size", new Vector2(map.Width, map.Height));
        _mat.SetShaderParameter("local_owner", (int)_me);

        _mapSprite = new Sprite2D
        {
            Texture = terrainTex,
            Centered = false,
            TextureFilter = TextureFilterEnum.Nearest,
            Material = _mat,
        };
        AddChild(_mapSprite);
    }

    public override void _Process(double delta)
    {
        if (!_paused && _game.Winner == null)
        {
            _acc += delta * _speed;
            int steps = 0;
            double tickLen = 1.0 / GameConfig.TicksPerSecond;
            while (_acc >= tickLen && steps < 8)
            {
                _session.Advance();
                _acc -= tickLen;
                steps++;
            }
            if (_acc > tickLen) _acc = 0; // geride kaldıysak yetişmeye çalışma
            if (steps > 0)
            {
                UploadDirtyTiles();
                _names.QueueRedraw();
            }
        }

        foreach (var e in _game.Events) _hud.AddEvent(e);
        _game.Events.Clear();

        int hoverTile = TileUnderMouse();
        var hoverOwner = hoverTile >= 0 ? _game.OwnerOf(hoverTile) : null;
        _mat.SetShaderParameter("hover_owner", (int)(hoverOwner?.Id ?? 0));
        _hud.Refresh(_game, _me, hoverTile, _paused, _speed);
    }

    private void UploadDirtyTiles()
    {
        var dirty = _game.DirtyTiles;
        if (dirty.Count == 0) return;
        var owner = _game.Owner;
        foreach (int t in dirty)
        {
            ushort o = owner[t];
            _ownerBytes[t * 2] = (byte)(o & 0xFF);
            _ownerBytes[t * 2 + 1] = (byte)(o >> 8);
        }
        dirty.Clear();
        _ownerImage.SetData(_game.Map.Width, _game.Map.Height, false, Image.Format.Rg8, _ownerBytes);
        _ownerTex.Update(_ownerImage);
    }

    private int TileUnderMouse()
    {
        var p = GetGlobalMousePosition();
        int x = Mathf.FloorToInt(p.X), y = Mathf.FloorToInt(p.Y);
        return _game.Map.InBounds(x, y) ? _game.Map.Index(x, y) : -1;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            OnTileClicked(TileUnderMouse());
            GetViewport().SetInputAsHandled();
        }
        else if (e is InputEventKey { Pressed: true, Echo: false } k)
        {
            switch (k.Keycode)
            {
                case Key.P: _paused = !_paused; break;
                case Key.Equal or Key.KpAdd: _speed = Math.Min(8, _speed * 2); break;
                case Key.Minus or Key.KpSubtract: _speed = Math.Max(1, _speed / 2); break;
                case Key.Space: CenterOnMe(); break;
                case Key.F5: StartGame((ulong)GD.Randi() | ((ulong)GD.Randi() << 32)); break;
                case >= Key.Key1 and <= Key.Key9:
                    int permille = (int)(k.Keycode - Key.Key0) * 100;
                    _session.Submit(new SetAttackRatioIntent(_me, permille));
                    _hud.SetRatio(permille);
                    break;
            }
        }
    }

    private void OnTileClicked(int tile)
    {
        if (tile < 0 || _game.Winner != null) return;
        var me = _game.PlayerById(_me);
        if (!me.IsAlive) return;

        if (_game.IsSpawnPhase)
        {
            if (_game.Map.IsLand(tile)) _session.Submit(new SpawnIntent(_me, tile));
            return;
        }
        if (!_game.Map.IsLand(tile)) return; // denizden çıkarma M1'de
        ushort target = _game.Owner[tile];
        if (target == _me) return;
        _session.Submit(new AttackIntent(_me, target));
    }

    private void CenterOnMe()
    {
        var me = _game.PlayerById(_me);
        if (me.TileCount > 0)
            _camera.Position = new Vector2(me.SumX / me.TileCount, me.SumY / me.TileCount);
    }
}

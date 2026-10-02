using Godot;

namespace Keladam;

/// <summary>Sağ/orta tuşla sürükle, tekerlekle imlece doğru yakınlaş, WASD/oklarla kaydır.</summary>
public partial class CameraController : Camera2D
{
    private const float MinZoom = 0.4f, MaxZoom = 24f, PanSpeed = 900f;

    public void Fit(Vector2 mapSize)
    {
        var vp = GetViewportRect().Size;
        float z = Mathf.Min(vp.X / mapSize.X, vp.Y / mapSize.Y) * 0.95f;
        Zoom = Vector2.One * Mathf.Clamp(z, MinZoom, MaxZoom);
        Position = mapSize / 2;
    }

    public override void _Process(double delta)
    {
        var dir = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) dir.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) dir.Y += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) dir.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) dir.X += 1;
        if (dir != Vector2.Zero) Position += dir.Normalized() * PanSpeed * (float)delta / Zoom.X;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp } mb:
                ZoomAt(mb.Position, 1.15f);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown } mb:
                ZoomAt(mb.Position, 1 / 1.15f);
                break;
            case InputEventMouseMotion mm when (mm.ButtonMask & (MouseButtonMask.Right | MouseButtonMask.Middle)) != 0:
                Position -= mm.Relative / Zoom.X;
                break;
        }
    }

    private void ZoomAt(Vector2 screenPos, float factor)
    {
        var half = GetViewportRect().Size / 2;
        var world = Position + (screenPos - half) / Zoom.X;
        float z = Mathf.Clamp(Zoom.X * factor, MinZoom, MaxZoom);
        Zoom = Vector2.One * z;
        Position = world - (screenPos - half) / z;
    }
}

using Raylib_cs;
using System.Numerics;

class Sprite(Texture2D texture, Color? color = null)
{
    public Texture2D Texture { get; set; } = texture;
    public Color Color { get; set; } = color ?? Color.White;

    public void DrawCentered(Vector2 Position)
        => Raylib.DrawTexture(Texture,
            (int)(Position.X - Texture.Width / 2),
            (int)(Position.Y - Texture.Height / 2),
            Color
        );
}

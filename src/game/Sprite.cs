using Raylib_cs;
using System.Numerics;
using static EnvRenderer;

class Sprite
{
    public Texture2D Texture { get; set; }
    public Color Color { get; set; }

    public Sprite(Texture2D texture, Color? color = null)
    {
        Texture = texture;
        Color = color ?? Color.White;
    }

    public void DrawCentered(Vector2 Position)
        => Raylib.DrawTexture(Texture,
            (int)(Position.X - Texture.Width / 2),
            (int)(Position.Y - Texture.Height / 2),
            Color
        );
}

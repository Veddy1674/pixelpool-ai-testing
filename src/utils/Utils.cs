using Raylib_cs;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using static EnvRenderer;

static class Utils
{
    public static float Abs(this float value)
        => value >= 0 ? value : -value;

    public static Vector2 Normalized(this Vector2 vector)
        => vector / vector.Length();

    public static Rectangle RecFromLTRB(float left, float top, float right, float bottom)
        => new(left, top, right - left, bottom - top);

    public static Texture2D WithSize(this Texture2D t, int width, int height)
    {
        // builder
        t.Width = width * SCALE;
        t.Height = height * SCALE;
        return t;
    }

    public static float Lerp(float a, float b, float t)
        => a + (b - a) * t;

    public static string WithSeparatorDots(this int number)
        => number.ToString("#,##0").Replace(',', '.'); // EU

    public static string GetOrDefault(this string[] args, int index, string defaultValue = "") =>
            index >= 0 && index < args.Length ? args[index] : defaultValue; // doesn't throw IndexOutOfRangeException

    public static bool TryGetValueAfter(this string[] args, string flag, out string value, string defaultValue = "")
    {
        int index = Array.IndexOf(args, flag);
        if (index >= 0 && index + 1 < args.Length)
        {
            value = args[index + 1];
            return true;
        }
        value = defaultValue;
        return false;
    } // e.g: args.TryGetValueAfter("--threads", out string? threads);

    public static bool CopyTo(this ISaveState source, ISaveState target)
        => ISaveState.CopyTo(source, target);

    #region Embedded Resources Fetching

    public static Font BahnschriftFont;

    public static void LoadFonts()
    {
        BahnschriftFont = LoadFontER("assets.bahnschrift.ttf", 48);
    }

    // generalized
    private static byte[] GetEmbeddedResourceData(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string fullName = $"{assembly.GetName().Name}.{resourceName}";

        using Stream? stream = assembly.GetManifestResourceStream(fullName)
            ?? throw new FileNotFoundException($"Resource '{fullName}' not found.");

        using MemoryStream ms = new();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public static Font LoadFontER(string resourceName, int fontSize = 0)
    {
        byte[] data = GetEmbeddedResourceData(resourceName);
        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                ReadOnlySpan<byte> ext = ".ttf\0"u8;
                fixed (byte* extPtr = ext)
                {
                    return Raylib.LoadFontFromMemory((sbyte*)extPtr, dataPtr, data.Length, fontSize, null, 0);
                }
            }
        }
    }

    public static Texture2D LoadTextureER(string resourceName)
    {
        byte[] data = GetEmbeddedResourceData(resourceName);
        string ext = Path.GetExtension(resourceName).ToLower();

        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                ReadOnlySpan<byte> extBytes = (ext + "\0").Select(c => (byte)c).ToArray();
                fixed (byte* extPtr = extBytes)
                {
                    Image image = Raylib.LoadImageFromMemory((sbyte*)extPtr, dataPtr, data.Length);
                    Texture2D texture = Raylib.LoadTextureFromImage(image);
                    Raylib.UnloadImage(image);
                    return texture;
                }
            }
        }
    }

    #endregion
}

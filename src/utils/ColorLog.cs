public static class ColorLog
{
    private static readonly Dictionary<char, ConsoleColor> logColorMap = new()
    {
        // inspired by Minecraft's color codes
        ['0'] = ConsoleColor.Gray, // default
        ['1'] = ConsoleColor.DarkGray,
        ['2'] = ConsoleColor.White,
        ['3'] = ConsoleColor.Black, // invisible on black background!

        // greens
        ['q'] = ConsoleColor.Green,
        ['w'] = ConsoleColor.DarkGreen,

        // cyans/blues
        ['a'] = ConsoleColor.Cyan,
        ['s'] = ConsoleColor.DarkCyan,
        ['d'] = ConsoleColor.Blue,
        ['f'] = ConsoleColor.DarkBlue,

        // reds/magentas
        ['z'] = ConsoleColor.Magenta,
        ['x'] = ConsoleColor.DarkMagenta,
        ['c'] = ConsoleColor.Red,
        ['v'] = ConsoleColor.DarkRed,

        // yellows
        ['t'] = ConsoleColor.Yellow,
        ['y'] = ConsoleColor.DarkYellow,

        // made for QWERTY keyboards, as "qwerty", "asdfgh", "zxcvbn" are all near eachother
        // and the colors go from light to dark, all whats need to be remembered is q = greens, a = cyans..
        // and then it's just about using the key nearby to get a darker tone...
    };

    public static void Log(string text, bool newLine = true)
    {
        // no check for "if text doesn't contain & then skip" because regardless, .Contains() cycles through the characters anyway
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '&' && i + 1 < text.Length)
            {
                char code = text[i + 1];
                if (logColorMap.TryGetValue(code, out ConsoleColor value))
                {
                    Console.ForegroundColor = value;
                    i++;
                    continue;
                }
            }
            Console.Write(text[i]);
        }
        if (newLine) Console.WriteLine();
        Console.ResetColor();
    }

    public static void Log(object? message)
        => Console.WriteLine(message == null ? "null" : message.ToString());

    public static void LogError(object? message) // ???
        => Console.Error.WriteLine(message == null ? "null" : message.ToString());
}
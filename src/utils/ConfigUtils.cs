using System.Text.Json;

static class ConfigUtils
{
    private static readonly string ConfigPath = Path.Combine(
        Directory.GetCurrentDirectory(), "saved", "config.json"
    );

    // cache
    private static readonly JsonSerializerOptions CachedJsonOptions = new()
    {
        WriteIndented = true // make json readable
    };

    public static void CreateIfNotExists()
    {
        if (!File.Exists(ConfigPath))
            ReplaceTo(new Config());
    }

    public static Config Read()
    {
        // in the rare case config.json is deleted after application executed and before this method is called
        CreateIfNotExists();

        string json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<Config>(json) ?? new Config();
    }

    public static void ReplaceTo(Config config)
    {
        string json = JsonSerializer.Serialize(config, CachedJsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}

class Config
{
    public string python_version { get; set; } = "";
}
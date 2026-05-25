namespace SmartWasteManagement.Services;

public static class EnvLoader
{
    public static void LoadDotEnv(string contentRootPath)
    {
        var envPath = Path.Combine(contentRootPath, ".env");
        if (!File.Exists(envPath))
            return;

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var idx = trimmed.IndexOf('=');
            if (idx <= 0)
                continue;

            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim().Trim('"');
            if (!string.IsNullOrEmpty(key) && Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}

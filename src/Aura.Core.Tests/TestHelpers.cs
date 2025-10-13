using System.Text.Json;

namespace Aura.Core.Tests;

public static class TestHelpers
{
    /// <summary>
    /// Creates a settings dictionary that mimics the JSON deserialization behavior.
    /// In the real application, settings come from JSON and are JsonElement objects.
    /// </summary>
    public static Dictionary<string, object> CreateJsonSettings(Dictionary<string, object> settings)
    {
        var result = new Dictionary<string, object>();
        foreach (var kvp in settings)
        {
            result[kvp.Key] = JsonSerializer.SerializeToElement(kvp.Value);
        }
        return result;
    }

    /// <summary>
    /// Creates a settings dictionary with a single string setting.
    /// </summary>
    public static Dictionary<string, object> CreateStringSetting(string key, string value)
    {
        return CreateJsonSettings(new Dictionary<string, object> { [key] = value });
    }

    /// <summary>
    /// Creates a settings dictionary with a single int array setting.
    /// </summary>
    public static Dictionary<string, object> CreateIntArraySetting(string key, int[] values)
    {
        return CreateJsonSettings(new Dictionary<string, object> { [key] = values });
    }
}

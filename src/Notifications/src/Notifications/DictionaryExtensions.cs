using System.Text.Json;

#pragma warning disable IDE0130
namespace System.Collections.Generic;

public static class DictionaryExtensions
{
    extension (IDictionary<string, object?> @this)
    {
        public TData As<TData>()
            where TData : class
        {
            var configRaw = JsonSerializer.Serialize(@this, JsonSerializerOptions.Default);
            var config = JsonSerializer.Deserialize<TData>(configRaw, JsonSerializerOptions.Default)!;

            return config;
        }
    }
}

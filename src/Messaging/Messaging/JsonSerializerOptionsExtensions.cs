using System.Text.Json;

namespace EtherGizmos.Common;

public static class JsonSerializerOptionsExtensions
{
    extension(JsonSerializerOptions)
    {
        public static JsonSerializerOptions MessagingDefault => Data.Default;
    }

    private class Data
    {
        public static JsonSerializerOptions Default { get; }

        static Data()
        {
            Default = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }
    }
}

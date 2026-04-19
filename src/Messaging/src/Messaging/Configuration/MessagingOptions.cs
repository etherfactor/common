namespace EtherGizmos.Common.Configuration;

public class MessagingOptions
{
    public bool Serverless { get; set; }

    public Dictionary<string, MessagePublisherOptions> Publishers { get; set; } = [];

    public Dictionary<string, MessageListenerOptions> Listeners { get; set; } = [];

    public Dictionary<Type, string> TypeMappings { get; set; } = [];

    internal Dictionary<string, Type> ReverseTypeMappings { get; set; } = [];

    internal void Build()
    {
        ReverseTypeMappings = TypeMappings
            .Select(e => new KeyValuePair<string, Type>(e.Value, e.Key))
            .ToDictionary();
    }

    public string ConvertType(Type type)
    {
        string result;
        if (TypeMappings.Any())
        {
            if (!TypeMappings.TryGetValue(type, out var tempType))
            {
                throw new InvalidOperationException($"When at least one type is mapped, all types must be mapped. Missing a mapping for {type}.");
            }

            result = tempType;
        }
        else
        {
            result = type.AssemblyQualifiedName!;
        }

        return result;
    }

    public Type ConvertType(string type)
    {
        Type result;
        if (TypeMappings.Any())
        {
            if (!ReverseTypeMappings.TryGetValue(type, out var tempType))
            {
                throw new InvalidOperationException($"When at least one type is mapped, all types must be mapped. Missing a mapping for {type}.");
            }

            result = tempType;
        }
        else
        {
            result = Type.GetType(type)
                ?? throw new InvalidOperationException($"Unable to find a fully-qualified type for {type}.");
        }

        return result;
    }
}

public static class MessagingOptionsExtensions
{
    public static void AddMap(
        this Dictionary<Type, string> @this, Type type, string alias)
    {
        @this.Add(type, alias);
    }

    public static void AddQueue(
        this Dictionary<string, MessageListenerOptions> @this, string logicalName, string queue)
    {
        @this.Add(logicalName, new()
        {
            IsTopic = false,
            Name = queue,
        });
    }

    public static void AddTopic(
        this Dictionary<string, MessageListenerOptions> @this, string logicalName, string topic, string subscription)
    {
        @this.Add(logicalName, new()
        {
            IsTopic = true,
            Name = topic,
            Subscription = subscription,
        });
    }

    public static void AddQueue(
        this Dictionary<string, MessagePublisherOptions> @this, string logicalName, string queue)
    {
        @this.Add(logicalName, new()
        {
            IsTopic = false,
            Name = queue,
        });
    }

    public static void AddTopic(
        this Dictionary<string, MessagePublisherOptions> @this, string logicalName, string topic)
    {
        @this.Add(logicalName, new()
        {
            IsTopic = true,
            Name = topic,
        });
    }
}

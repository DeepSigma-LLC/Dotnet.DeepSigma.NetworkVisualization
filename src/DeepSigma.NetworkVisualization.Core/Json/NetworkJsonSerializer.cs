using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSigma.NetworkVisualization.Json;

public static class NetworkJsonSerializer
{
    public const string FormatId = "deepsigma.network";
    public const string FormatVersion = "1.0";

    public static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(Network network)
    {
        var envelope = new NetworkEnvelope(FormatId, FormatVersion, network);
        return JsonSerializer.Serialize(envelope, Options);
    }

    public static Network? Deserialize(string json)
    {
        var envelope = JsonSerializer.Deserialize<NetworkEnvelope>(json, Options);
        return envelope?.Network;
    }

    public static JsonSerializerOptions CreateOptions()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        opts.Converters.Add(new ColorJsonConverter());
        opts.Converters.Add(new NodeIdJsonConverter());
        opts.Converters.Add(new EdgeIdJsonConverter());
        opts.Converters.Add(new JsonStringEnumConverter());
        return opts;
    }

    public sealed record NetworkEnvelope(
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("network")] Network Network);

    public sealed class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Color.FromHex(reader.GetString() ?? throw new JsonException("Color must be a string."));

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToHex(includeAlpha: true));
    }

    public sealed class NodeIdJsonConverter : JsonConverter<NodeId>
    {
        public override NodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString() ?? throw new JsonException("NodeId must be a string."));

        public override void Write(Utf8JsonWriter writer, NodeId value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }

    public sealed class EdgeIdJsonConverter : JsonConverter<EdgeId>
    {
        public override EdgeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString() ?? throw new JsonException("EdgeId must be a string."));

        public override void Write(Utf8JsonWriter writer, EdgeId value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }

    internal static string FormatDouble(double value)
        => value.ToString("0.######", CultureInfo.InvariantCulture);
}

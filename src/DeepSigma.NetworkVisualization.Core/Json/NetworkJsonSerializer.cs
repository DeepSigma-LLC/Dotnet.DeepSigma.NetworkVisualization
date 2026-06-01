using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSigma.NetworkVisualization.Json;

/// <summary>
/// The one canonical JSON contract for <see cref="Network"/>. Produces and consumes a versioned envelope of the form
/// <c>{ "format": "deepsigma.network", "version": "1.0", "network": { … } }</c>. <see cref="Importers.NetworkImporter.FromJson"/>
/// is a thin wrapper around <see cref="Deserialize"/>.
/// </summary>
public static class NetworkJsonSerializer
{
    /// <summary>The format identifier embedded in the envelope.</summary>
    public const string FormatId = "deepsigma.network";

    /// <summary>Schema version embedded in the envelope. Breaking changes will bump this.</summary>
    public const string FormatVersion = "1.0";

    /// <summary>The configured <see cref="JsonSerializerOptions"/> instance used by serialize and deserialize. Reuse for consistency in adjacent code.</summary>
    public static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Serialize a network to the canonical envelope.</summary>
    public static string Serialize(Network network)
    {
        var envelope = new NetworkEnvelope(FormatId, FormatVersion, network);
        return JsonSerializer.Serialize(envelope, Options);
    }

    /// <summary>Deserialize the canonical envelope. Returns <c>null</c> if the input is empty/null JSON; throws on malformed JSON or schema mismatch.</summary>
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

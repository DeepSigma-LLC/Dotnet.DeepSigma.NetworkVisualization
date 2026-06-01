using System.Globalization;

namespace DeepSigma.NetworkVisualization;

public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    public static Color Transparent { get; } = new(0, 0, 0, 0);
    public static Color White { get; } = new(255, 255, 255);
    public static Color Black { get; } = new(0, 0, 0);

    public string ToHex(bool includeAlpha = false)
    {
        return includeAlpha || A != 255
            ? $"#{R:X2}{G:X2}{B:X2}{A:X2}"
            : $"#{R:X2}{G:X2}{B:X2}";
    }

    public override string ToString() => ToHex();

    public static Color FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) throw new ArgumentException("Hex string cannot be empty.", nameof(hex));
        var s = hex.AsSpan();
        if (s[0] == '#') s = s[1..];

        return s.Length switch
        {
            3 => new Color(Dup(s[0]), Dup(s[1]), Dup(s[2])),
            4 => new Color(Dup(s[0]), Dup(s[1]), Dup(s[2]), Dup(s[3])),
            6 => new Color(Byte(s[..2]), Byte(s.Slice(2, 2)), Byte(s.Slice(4, 2))),
            8 => new Color(Byte(s[..2]), Byte(s.Slice(2, 2)), Byte(s.Slice(4, 2)), Byte(s.Slice(6, 2))),
            _ => throw new FormatException($"Invalid hex color '{hex}'.")
        };

        static byte Byte(ReadOnlySpan<char> span) => byte.Parse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        static byte Dup(char c) { var b = Byte([c]); return (byte)((b << 4) | b); }
    }

    public static bool TryParse(string? hex, out Color color)
    {
        try { color = FromHex(hex!); return true; }
        catch { color = default; return false; }
    }
}

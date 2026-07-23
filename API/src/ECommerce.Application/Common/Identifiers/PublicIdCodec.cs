namespace ECommerce.Application.Common.Identifiers;

public static class PublicIdCodec
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int MinimumPayloadLength = 5;
    private const int MaximumPayloadLength = 7;
    private const long MaximumSupportedId = 78_364_164_095;

    public static string EncodeProductId(long id) => Encode('P', id);

    public static string EncodeUserId(long id) => Encode('U', id);

    public static long DecodeProductId(string value) => Decode('P', value, "product");

    public static long DecodeUserId(string value) => Decode('U', value, "user");

    public static bool TryDecodeProductId(string? value, out long id) => TryDecode('P', value, out id);

    public static bool TryDecodeUserId(string? value, out long id) => TryDecode('U', value, out id);

    private static string Encode(char prefix, long id)
    {
        if (id <= 0 || id > MaximumSupportedId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                $"Id must be between 1 and {MaximumSupportedId}.");
        }

        Span<char> buffer = stackalloc char[MaximumPayloadLength];
        var index = buffer.Length;
        var remaining = id;

        while (remaining > 0)
        {
            buffer[--index] = Alphabet[(int)(remaining % Alphabet.Length)];
            remaining /= Alphabet.Length;
        }

        var encoded = new string(buffer[index..]);
        return string.Concat(prefix, encoded.PadLeft(MinimumPayloadLength, '0'));
    }

    private static long Decode(char prefix, string value, string entityName)
    {
        if (!TryDecode(prefix, value, out var id))
        {
            throw new FormatException($"The {entityName} id must be 6-8 uppercase characters and start with '{prefix}'.");
        }

        return id;
    }

    private static bool TryDecode(char prefix, string? value, out long id)
    {
        id = 0;

        if (string.IsNullOrWhiteSpace(value) ||
            value.Length is < 6 or > 8 ||
            value[0] != prefix)
        {
            return false;
        }

        try
        {
            foreach (var character in value.AsSpan(1))
            {
                var digit = Alphabet.IndexOf(character);
                if (digit < 0)
                {
                    return false;
                }

                id = checked((id * Alphabet.Length) + digit);
            }
        }
        catch (OverflowException)
        {
            id = 0;
            return false;
        }

        return id > 0 && string.Equals(Encode(prefix, id), value, StringComparison.Ordinal);
    }
}

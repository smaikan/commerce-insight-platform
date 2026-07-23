using ECommerce.Application.Common.Identifiers;

namespace ECommerce.API.Routing;

public static class ApiPublicIdParser
{
    public static long ParseProductId(string value)
    {
        if (PublicIdCodec.TryDecodeProductId(value, out var id))
        {
            return id;
        }

        throw new BadHttpRequestException("Product id must be 6-8 uppercase characters and start with 'P'.");
    }

    public static long ParseUserId(string value)
    {
        if (PublicIdCodec.TryDecodeUserId(value, out var id))
        {
            return id;
        }

        throw new BadHttpRequestException("User id must be 6-8 uppercase characters and start with 'U'.");
    }
}

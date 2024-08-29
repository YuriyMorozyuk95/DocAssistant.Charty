

namespace MinimalApi.Extensions;

internal static class ConfigurationExtensions
{
    public static string GetStorageAccountEndpoint(this IConfiguration config)
    {
        var endpoint = config["AzureStorageAccountEndpoint"];
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

        return endpoint;
    }

    public static string ToCitationBaseUrl(this IConfiguration config)
    {
        var endpoint = config.GetStorageAccountEndpoint();

        var builder = new UriBuilder(endpoint)
        {
            Path = config["AzureStorageContainer"]
        };

        return builder.Uri.AbsoluteUri;
    }
}

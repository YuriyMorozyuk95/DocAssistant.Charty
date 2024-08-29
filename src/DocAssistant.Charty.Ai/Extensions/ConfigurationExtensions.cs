using DocAssistant.Charty.Ai.Exceptions;

using Microsoft.Extensions.Configuration;

namespace DocAssistant.Charty.Ai.Extensions
{
    public static class ConfigurationExtensions  
    {
        public static string GetStorageAccountEndpoint(this IConfiguration config)
        {
            var endpoint = config["AzureStorageAccountEndpoint"];
            ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

            return endpoint;
        }
        //TODO
        public static string ToCitationBaseUrl(this IConfiguration config)
        {
            var endpoint = config.GetStorageAccountEndpoint();

            var builder = new UriBuilder(endpoint)
                          {
                              Path = config["AzureStorageContainer"]
                          };

            return builder.Uri.AbsoluteUri;
        }

        public static T GetRequiredConfigurationValue<T>(this IConfiguration configuration, string key)  
        {  
            var value = configuration.GetValue<T>(key, default(T));  
          
            if (value is null)  
            {  
                throw new DocAssistantInvalidConfigurationException($"{key} is missing in config");  
            }  
  
            return value;  
        }  
    }
}

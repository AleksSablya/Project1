using System.Globalization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace HttpContentLib
{
    public static class JsonConvertHelper
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal },
                new StringEnumConverter(new CamelCaseNamingStrategy())
            },
            DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
        };

        public static T? FromJson<T>(string? json, ILogger? logger = null) where T : class, new()
        {
            if (logger != null)
            {
                Settings.Error = (sender, args) =>
                {
                    string message = $"{typeof(T)}: {args.ErrorContext.Error.Message}. json: {json}";
                    logger.LogError(message);
                    args.ErrorContext.Handled = true;
                };
            }
            return json != null ? JsonConvert.DeserializeObject<T>(json, Settings) : null;
        }

        public static string ToJson<T>(T @object) => JsonConvert.SerializeObject(@object, Settings);
    }
}

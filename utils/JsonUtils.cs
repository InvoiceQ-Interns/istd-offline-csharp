using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace istd_offline_csharp.utils
{
    public class jsonUtils
    {
        private static readonly JsonSerializerSettings mapper = newMapper();

        public static string toJson(object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, mapper);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static T readJson<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, mapper);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        private static JsonSerializerSettings newMapper()
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateParseHandling = DateParseHandling.DateTimeOffset
            };

            return settings;
        }
    }
}

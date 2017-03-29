using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Proto.Persistence.EventStore
{
    public static class SerializationSettings
    {
        /// <summary>
        /// Standard settings are:
        /// 1. Pretty (aka Indented)
        /// 2. Include null values
        /// 3. Include default values
        /// 4. Camelcase
        /// 5. Auto type information included
        /// </summary>
        /// <returns></returns>
        public static JsonSerializerSettings StandardSettings()
        {
            return CreateSettings(Formatting.Indented);
        }

        public static JsonSerializerSettings MinifiedSettings()
        {
            return CreateSettings(Formatting.None);
        }

        private static JsonSerializerSettings CreateSettings(Formatting formatting)
        {
            var contractResolver = new DoNotCamelCaseSingleCharacterPropertyNamesContractResolver();

            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = formatting,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                ContractResolver = contractResolver
            };

            serializerSettings.Converters.Add(new StringEnumConverter() { CamelCaseText = false });
            return serializerSettings;
        }

        /// <summary>
        /// This is to stop the CamelCasePropertyNamesContractResolver camelCasing single char property names
        /// Stops "A", "B", "C" etc being converted to "a", "b", "c" etc. 
        /// </summary>
        private class DoNotCamelCaseSingleCharacterPropertyNamesContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                if (propertyName.Count() == 1) return propertyName;
                return base.ResolvePropertyName(propertyName);
            }
        }
    }
}
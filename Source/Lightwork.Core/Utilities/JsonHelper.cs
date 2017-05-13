using Newtonsoft.Json;

namespace Lightwork.Core.Utilities
{
    public static class JsonHelper
    {
        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public static object Deserialize(string value)
        {
            return JsonConvert.DeserializeObject(value);
        }

        public static T Deserialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        public static T TryDeserialize<T>(string value, T defaultValue = default(T))
        {
            try
            {
                return Deserialize<T>(value);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}

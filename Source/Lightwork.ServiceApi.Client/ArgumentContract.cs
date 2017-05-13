using System.Runtime.Serialization;

namespace D3.Lightwork.ServiceApi.Client
{
    [DataContract]
    public class ArgumentContract
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public object Value { get; set; }

        public static ArgumentContract Create<T>(string name, T value)
        {
            return Create(typeof(T).AssemblyQualifiedName, name, value);
        }

        public static ArgumentContract Create(string name, object value)
        {
            var type = value?.GetType() ?? typeof(object);

            return Create(type.AssemblyQualifiedName, name, value);
        }

        public static ArgumentContract Create(string type, string name, object value)
        {
            return new ArgumentContract
            {
                Name = name,
                Type = type,
                Value = value
            };
        }
    }
}
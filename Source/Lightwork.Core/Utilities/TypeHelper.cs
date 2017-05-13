using System;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Lightwork.Core.Utilities
{
    public static class TypeHelper
    {
        public static async Task<T> ActionAsync<T>(this object obj, string action)
        {
            var method = GetActionMethod(obj, action);

            if (method == null)
            {
                return default(T);
            }

            var invokeResult = method.Invoke(obj, new object[] { });
            if (invokeResult is Task<T>)
            {
                return await
                    (Task<T>)invokeResult;
            }

            if (invokeResult is Task)
            {
                await
                    (Task)invokeResult;
                return default(T);
            }

            if (method.ReturnType != typeof(void))
            {
                return (T)invokeResult;
            }

            return default(T);
        }

        public static MethodInfo GetActionMethod(this object obj, string action)
        {
            var methodName = action.ToTitleCase().Replace(" ", string.Empty);
            var methodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var method = obj.GetType().GetMethod(methodName, methodFlags) ??
                obj.GetType().GetMethod("On" + methodName, methodFlags);

            return method;
        }

        public static object ChangeType(object value, Type type)
        {
            if (value == null && type.IsGenericType)
            {
                return Activator.CreateInstance(type);
            }

            if (value == null)
            {
                return null;
            }

            if (type == value.GetType())
            {
                return value;
            }

            if (type.IsEnum)
            {
                if (value is string)
                {
                    return Enum.Parse(type, value as string);
                }

                return Enum.ToObject(type, value);
            }

            if (value is JObject)
            {
                return ((JObject)value).ToObject(type);
            }

            if (!type.IsInterface && type.IsGenericType)
            {
                var innerType = type.GetGenericArguments()[0];
                var innerValue = ChangeType(value, innerType);
                return Activator.CreateInstance(type, new[] { innerValue });
            }

            if (value is string && type == typeof(Guid))
            {
                return new Guid(value as string);
            }

            if (value is string && type == typeof(Version))
            {
                return new Version(value as string);
            }

            if (!(value is IConvertible))
            {
                return value;
            }

            return Convert.ChangeType(value, type);
        }

        public static object ChangeType(object value, string typeName)
        {
            return ChangeType(value, Type.GetType(typeName));
        }

        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(value, typeof(T));
        }

        public static TProperty GetProperty<TProperty>(this object entity, string propertyName)
        {
            var property = entity.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return default(TProperty);
            }

            return GetValueOrDefault<TProperty>(property.GetValue(entity));
        }

        public static T GetValue<T>(object value)
        {
            return ChangeType<T>(value);
        }

        public static T GetValueOrDefault<T>(object value, T defaultValue = default(T))
        {
            if (value == null)
            {
                return defaultValue;
            }

            return GetValue<T>(value);
        }

        public static object GetDefaultValue(this Type source)
        {
            return source.IsValueType ? Activator.CreateInstance(source) : null;
        }
    }
}

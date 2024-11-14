using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DezinsectionApp.Extentions
{
    public static class EnumExtentions
    {
        public static bool TryParseDisplayNameToEnumValue<T>(this T _, string displayName, out T enumValue, bool ignoreCase = true) where T : struct, Enum
        {
            displayName = displayName.ToLower();

            foreach (var field in typeof(T).GetFields())
            {
                var attribute = field.GetCustomAttribute<DisplayAttribute>();
                if (attribute != null && !string.IsNullOrEmpty(attribute.Name) && attribute.Name.ToLower() == displayName)
                {
                    enumValue = (T)field.GetValue(null);

                    return true;
                }
            }

            enumValue = default;
            return false;
        }
    }
}

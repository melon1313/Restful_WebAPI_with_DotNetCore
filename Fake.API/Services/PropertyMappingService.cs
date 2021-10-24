using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fake.API.Services
{
    public static class PropertyMappingService
    {
        public static bool IsPropertiesExists<T>(this string fields)
        {
            if (string.IsNullOrEmpty(fields)) return true;

            var properties = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            var fieldsAfterSpilt = fields.Split(',');
            PropertyInfo propertyInfo = null;
            var propertyName = string.Empty;

            foreach (var field in fieldsAfterSpilt)
            {
                propertyName = field.Trim();
                propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                //如果沒有找回對應屬性，則返回
                if (propertyInfo == null) return false;
            }

            return true;
        }
    }
}

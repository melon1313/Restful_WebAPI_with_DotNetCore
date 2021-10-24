using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fake.API.Helper
{
    public static class ObjectExtensions
    {
        public static ExpandoObject ShapData<TSource>(this TSource source, string fields)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            List<PropertyInfo> propertyInfos = new List<PropertyInfo>();

            if (string.IsNullOrEmpty(fields))
            {
                propertyInfos = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).ToList();
            }
            else
            {
                var fieldInfos = fields.Split(',');

                foreach (var field in fieldInfos)
                {
                    var propertyInfo = typeof(TSource).GetProperty(field.Trim(), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (propertyInfo == null) throw new Exception($"{typeof(TSource)}屬性{field}找不到!");
                    propertyInfos.Add(propertyInfo);
                }
            }

            ExpandoObject expandoObject = new ExpandoObject();
            foreach (var property in propertyInfos)
            {
                ((IDictionary<string, object>)expandoObject).Add(property.Name, property.GetValue(source));
            }

            return expandoObject;
        }
    }
}

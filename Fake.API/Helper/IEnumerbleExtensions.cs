using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fake.API.Helper
{
    public static class IEnumerbleExtensions
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSouce>(this IEnumerable<TSouce> source, string fields)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var expandoObjectList = new List<ExpandoObject>();

            //避免在列表中遍歷所有屬性與數據，創建一個屬性信息列表
            var propertyInfoList = new List<PropertyInfo>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                //返回動態類型對象ExpandoObject所有屬性
                var propertyInfos = typeof(TSouce).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                var fieldsAfterSplit = fields.Split(',');

                foreach (var field in fieldsAfterSplit)
                {
                    var propertyName = field.Trim();

                    var propertyInfo = typeof(TSouce).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (propertyInfo == null) throw new Exception($"{typeof(TSouce)}屬性 {propertyName} 找不到。");

                    propertyInfoList.Add(propertyInfo);
                }
            }
            foreach (TSouce souceObject in source)
            {
                //建立動態類型對象，建立塑形對象
                var dataShapeObject = new ExpandoObject();

                foreach (var propertyInfo in propertyInfoList)
                {
                    //獲取對應屬性的真實數據
                    var propertyValue = propertyInfo.GetValue(souceObject);

                    ((IDictionary<string, object>)dataShapeObject).Add(propertyInfo.Name, propertyValue);
                }

                expandoObjectList.Add(dataShapeObject);
            }

            return expandoObjectList;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using QuickFix.Fields;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace QuickFIXExchangeExample
{
    static class Extensions
    {
        public static string ToString(this Message msg, bool parse = false)
        {
            if (!parse)
            {
                return msg.ToString();
            }
            StringBuilder stringBuilder = new StringBuilder();
            var ObjRef = msg.GetType();
            stringBuilder.AppendLine(ObjRef.Name);
            foreach (var property in ObjRef.GetProperties())
            {
                try
                {
                    object upValue = property.GetValue(msg);
                    object value = property.PropertyType.GetProperty("Obj").GetValue(upValue);

                    //Check and fiil static fileds used like enum
                    var StaticFiels = property.PropertyType
                        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                        .Where(p => p.IsLiteral && !p.IsInitOnly).GroupBy(p => p.GetRawConstantValue(), v => v.Name)
                        .ToDictionary(group => group.Key, group => group.Aggregate(
                            (res, add) => $"{add}  or  {res}"));

                    if (StaticFiels.Count > 0 && StaticFiels.ContainsKey(value))
                    {
                        value = $"{value} ({StaticFiels[value]})";
                    }

                    stringBuilder.AppendLine($"\t{property.Name}: {value}");
                }
                catch (Exception e)
                {
                }
            }
            return stringBuilder.ToString();
        }

        public static bool IsReject(this Message msg)
        {
            bool result = msg is Reject;
            return result;
        }
    }
}

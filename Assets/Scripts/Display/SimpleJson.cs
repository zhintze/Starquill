using System.Collections.Generic;

namespace Starquill.Display
{
    public static class SimpleJson
    {
        public static List<Dictionary<string, object>> ParseArray(string json)
        {
            var result = new List<Dictionary<string, object>>();
            var parsed = MiniJSON.Json.Deserialize(json);
            if (parsed is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                        result.Add(dict);
                }
            }
            return result;
        }

        public static string GetString(this Dictionary<string, object> dict, string key, string defaultValue = "")
        {
            if (dict.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return defaultValue;
        }

        public static int GetInt(this Dictionary<string, object> dict, string key, int defaultValue = 0)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is long l) return (int)l;
                if (val is double d) return (int)d;
                if (int.TryParse(val.ToString(), out int result)) return result;
            }
            return defaultValue;
        }

        public static float GetFloat(this Dictionary<string, object> dict, string key, float defaultValue = 0f)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is double d) return (float)d;
                if (val is long l) return l;
                if (float.TryParse(val.ToString(), out float result)) return result;
            }
            return defaultValue;
        }

        public static bool GetBool(this Dictionary<string, object> dict, string key, bool defaultValue = false)
        {
            if (dict.TryGetValue(key, out var val) && val is bool b)
                return b;
            return defaultValue;
        }

        public static string[] GetStringArray(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val))
            {
                if (val is List<object> list)
                {
                    var arr = new string[list.Count];
                    for (int i = 0; i < list.Count; i++)
                        arr[i] = list[i]?.ToString() ?? "";
                    return arr;
                }
                if (val is string s && !string.IsNullOrEmpty(s))
                    return new[] { s };
            }
            return new string[0];
        }

        public static int[] GetIntArray(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val is List<object> list)
            {
                var arr = new int[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is long l) arr[i] = (int)l;
                    else if (list[i] is double d) arr[i] = (int)d;
                }
                return arr;
            }
            return new int[0];
        }

        public static List<object> GetArray(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val is List<object> list)
                return list;
            return null;
        }

        public static string[] ToStringArray(Dictionary<string, object> dict, string key)
        {
            return dict.GetStringArray(key);
        }

        public static int[] ToIntArray(Dictionary<string, object> dict, string key)
        {
            return dict.GetIntArray(key);
        }
    }
}

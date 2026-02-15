using System.Collections.Generic;
using UnityEngine;

namespace Starquill.Characters
{
    public class NameGenerator
    {
        private string[] firstNames = { "Hero" };
        private string[] epithets = { "the Brave" };

        public void LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>("Data/names");
            if (asset != null) LoadFromJson(asset.text);
        }

        public void LoadFromJson(string json)
        {
            var parsed = MiniJSON.Json.Deserialize(json);
            if (parsed is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("first_names", out var fn) && fn is List<object> fnList)
                {
                    firstNames = new string[fnList.Count];
                    for (int i = 0; i < fnList.Count; i++)
                        firstNames[i] = fnList[i]?.ToString() ?? "Hero";
                }
                if (dict.TryGetValue("epithets", out var ep) && ep is List<object> epList)
                {
                    epithets = new string[epList.Count];
                    for (int i = 0; i < epList.Count; i++)
                        epithets[i] = epList[i]?.ToString() ?? "";
                }
            }
        }

        public string Generate(System.Random rng)
        {
            string first = firstNames[rng.Next(firstNames.Length)];
            string epithet = epithets[rng.Next(epithets.Length)];
            return $"{first} {epithet}";
        }
    }
}

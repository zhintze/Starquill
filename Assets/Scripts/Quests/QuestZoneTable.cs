using System;
using System.Collections.Generic;
using Starquill.Core;
using UnityEngine;

namespace Starquill.Quests
{
    public class QuestZoneTable
    {
        private readonly List<QuestZone> zones = new();

        public IReadOnlyList<QuestZone> Zones => zones;

        public void LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>("Data/quest_zones");
            if (asset != null) LoadFromJson(asset.text);
        }

        public void LoadFromJson(string json)
        {
            zones.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var zone = new QuestZone
                {
                    Id = obj.GetString("id"),
                    Name = obj.GetString("name"),
                    IntroDialogue = obj.GetStringArray("introDialogue"),
                    CompletionDialogue = obj.GetStringArray("completionDialogue")
                };

                var typeStrings = obj.GetStringArray("dominantTypes");
                var types = new List<StatType>();
                foreach (var s in typeStrings)
                {
                    if (Enum.TryParse<StatType>(s, out var st))
                        types.Add(st);
                }
                zone.DominantTypes = types.ToArray();

                zones.Add(zone);
            }
        }

        /// Clamped: indices past the end return the last zone (endless
        /// final-zone repeat for MVP); negatives return the first.
        public QuestZone GetZone(int index)
        {
            if (zones.Count == 0) return null;
            index = Math.Clamp(index, 0, zones.Count - 1);
            return zones[index];
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Characters
{
    public class Party
    {
        public const int MaxSize = 4;
        public List<CharacterInstance> Members { get; } = new();

        public Stats[] GetAllStats()
        {
            return Members.Select(m => m.GetTotalStats()).ToArray();
        }

        public List<VerbDefinition> GetAllVerbs()
        {
            return Members.SelectMany(m => m.equippedVerbs).ToList();
        }

        public float GetChaGoldBonus()
        {
            return Members.Sum(m => m.GetTotalStats().CHA) * 0.02f;
        }

        public float GetIntXPBonus()
        {
            return Members.Sum(m => m.GetTotalStats().INT) * 0.02f;
        }

        public float GetWisDiscoveryBonus()
        {
            return Members.Sum(m => m.GetTotalStats().WIS) * 0.02f;
        }

        public bool AddMember(CharacterInstance character)
        {
            if (Members.Count >= MaxSize) return false;
            Members.Add(character);
            return true;
        }

        public void RemoveMember(int index)
        {
            if (index >= 0 && index < Members.Count)
                Members.RemoveAt(index);
        }
    }
}

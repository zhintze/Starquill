using System;
using System.Collections.Generic;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Managers
{
    [Serializable]
    public class SaveData
    {
        public double gold;
        public int highestQuestLevel;
        public int currentQuestLevel = 1;
        public List<SerializedCharacter> roster = new();
        public int[] activePartyIndices = { 0, 1, 2, 3 };
        public PityTracker pityTracker = new();
        public List<SerializedEquipment> inventory = new();
        public long lastPlayedTimestamp;
        public float fragmentProgress;
        public float travelProgress;

        // Quest progression (Offered phase saves as Idle; offers re-roll)
        public int questZoneIndex;
        public int questNextIndex;
        public int questPhase;
        public int questActiveWave;
        public double questGoldEarned;

        // Shop / boosts / monetization
        public double boostAutoFireExpiry;
        public double boostSpeedUpExpiry;
        public double chestReadyAtTimestamp;
        public bool removeAdsOwned;

        public bool NeedsRosterInitialization()
        {
            return roster == null || roster.Count == 0;
        }
    }

    [Serializable]
    public class SerializedCharacter
    {
        public string id;
        public string displayName;
        public string speciesId;
        public int level;
        public int xp;
        public int speciesKills;
        public int[] baseStats = new int[6];
        public int[] allocatedStats = new int[6];
        public List<string> equippedVerbIds = new();
        public SerializedEquipment[] equipment = new SerializedEquipment[11];

        public static SerializedCharacter FromInstance(CharacterInstance c)
        {
            var sc = new SerializedCharacter
            {
                id = c.id,
                displayName = c.displayName,
                speciesId = c.speciesId,
                level = c.level,
                xp = c.xp,
                speciesKills = c.speciesKills,
                baseStats = StatsToArray(c.baseStats),
                allocatedStats = StatsToArray(c.allocatedStats)
            };

            foreach (var verb in c.equippedVerbs)
                if (verb != null) sc.equippedVerbIds.Add(verb.verbId);

            for (int i = 0; i < c.equipment.Length && i < 11; i++)
            {
                if (c.equipment[i] != null)
                    sc.equipment[i] = SerializedEquipment.FromInstance(c.equipment[i]);
            }

            return sc;
        }

        public CharacterInstance ToInstance()
        {
            return new CharacterInstance
            {
                id = id,
                displayName = displayName,
                speciesId = speciesId,
                level = level,
                xp = xp,
                speciesKills = speciesKills,
                baseStats = ArrayToStats(baseStats),
                allocatedStats = ArrayToStats(allocatedStats)
            };
        }

        private static int[] StatsToArray(Stats s)
        {
            return s == null ? new int[6] : new[] { s.STR, s.DEX, s.CON, s.INT, s.WIS, s.CHA };
        }

        private static Stats ArrayToStats(int[] arr)
        {
            if (arr == null || arr.Length < 6) return new Stats();
            return new Stats { STR = arr[0], DEX = arr[1], CON = arr[2], INT = arr[3], WIS = arr[4], CHA = arr[5] };
        }
    }
}

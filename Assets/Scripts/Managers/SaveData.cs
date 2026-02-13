using System;
using System.Collections.Generic;
using Starquill.Equipment;

namespace Starquill.Managers
{
    [Serializable]
    public class SaveData
    {
        public double gold;
        public int highestQuestLevel;
        public List<CharacterSaveData> characters = new();
        public List<int> partyMemberIndices = new();
        public PityTracker pityTracker = new();
        public long lastPlayedTimestamp;
        public int currentQuestLevel = 1;
        public float fragmentProgress;
        public List<string> codexEntries = new();
    }

    [Serializable]
    public class CharacterSaveData
    {
        public string id;
        public string displayName;
        public string speciesId;
        public int level;
        public int xp;
        public int speciesKills;
        public int[] allocatedStats = new int[6];
        public List<string> equippedVerbIds = new();
        public string[] equippedItemIds = new string[11];
    }
}

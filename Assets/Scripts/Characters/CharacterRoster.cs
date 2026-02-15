using System.Collections.Generic;

namespace Starquill.Characters
{
    public class CharacterRoster
    {
        public const int RosterCap = 20;
        public List<CharacterInstance> Characters { get; } = new();
        public int[] ActivePartyIndices { get; } = { -1, -1, -1, -1 };

        public bool AddCharacter(CharacterInstance character)
        {
            if (Characters.Count >= RosterCap) return false;
            Characters.Add(character);
            return true;
        }

        public void RemoveCharacter(int index)
        {
            if (index < 0 || index >= Characters.Count) return;
            Characters.RemoveAt(index);

            for (int i = 0; i < ActivePartyIndices.Length; i++)
            {
                if (ActivePartyIndices[i] == index)
                    ActivePartyIndices[i] = -1;
                else if (ActivePartyIndices[i] > index)
                    ActivePartyIndices[i]--;
            }
        }

        public void SetPartyMember(int partySlot, int rosterIndex)
        {
            if (partySlot < 0 || partySlot >= 4) return;
            if (rosterIndex < 0 || rosterIndex >= Characters.Count) return;
            ActivePartyIndices[partySlot] = rosterIndex;
        }

        public CharacterInstance[] GetActiveParty()
        {
            var party = new CharacterInstance[4];
            for (int i = 0; i < 4; i++)
            {
                int idx = ActivePartyIndices[i];
                party[i] = (idx >= 0 && idx < Characters.Count) ? Characters[idx] : null;
            }
            return party;
        }

        public void InitializeDefaultParty()
        {
            for (int i = 0; i < 4 && i < Characters.Count; i++)
                ActivePartyIndices[i] = i;
        }

        public bool IsInParty(int rosterIndex)
        {
            for (int i = 0; i < ActivePartyIndices.Length; i++)
                if (ActivePartyIndices[i] == rosterIndex) return true;
            return false;
        }

        public int GetPartySlot(int rosterIndex)
        {
            for (int i = 0; i < ActivePartyIndices.Length; i++)
                if (ActivePartyIndices[i] == rosterIndex) return i;
            return -1;
        }

        public bool AddToParty(int rosterIndex)
        {
            if (rosterIndex < 0 || rosterIndex >= Characters.Count) return false;
            if (IsInParty(rosterIndex)) return false;

            for (int i = 0; i < ActivePartyIndices.Length; i++)
            {
                if (ActivePartyIndices[i] == -1)
                {
                    ActivePartyIndices[i] = rosterIndex;
                    return true;
                }
            }
            return false;
        }

        public void RemoveFromParty(int rosterIndex)
        {
            for (int i = 0; i < ActivePartyIndices.Length; i++)
            {
                if (ActivePartyIndices[i] == rosterIndex)
                {
                    ActivePartyIndices[i] = -1;
                    return;
                }
            }
        }

        public void SwapPartyMember(int partySlot, int newRosterIndex)
        {
            if (partySlot < 0 || partySlot >= 4) return;
            if (newRosterIndex < 0 || newRosterIndex >= Characters.Count) return;
            RemoveFromParty(newRosterIndex);
            ActivePartyIndices[partySlot] = newRosterIndex;
        }
    }
}

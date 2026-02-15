using System;
using UnityEngine;
using Starquill.Characters;

namespace Starquill.UI
{
    public class PartyScreenController : MonoBehaviour
    {
        private CharacterRoster roster;

        public int SelectedRosterIndex { get; private set; }
        public int ActiveSubTab { get; private set; }
        public CharacterInstance SelectedCharacter =>
            roster != null && SelectedRosterIndex >= 0 && SelectedRosterIndex < roster.Characters.Count
                ? roster.Characters[SelectedRosterIndex]
                : null;

        public event Action<int> OnSelectionChanged;
        public event Action<int> OnSubTabChanged;

        public void SetRoster(CharacterRoster newRoster)
        {
            roster = newRoster;

            // Default selection: first active party member
            for (int i = 0; i < roster.ActivePartyIndices.Length; i++)
            {
                if (roster.ActivePartyIndices[i] >= 0)
                {
                    SelectedRosterIndex = roster.ActivePartyIndices[i];
                    return;
                }
            }

            SelectedRosterIndex = roster.Characters.Count > 0 ? 0 : -1;
        }

        public void SelectCharacter(int rosterIndex)
        {
            if (roster == null) return;
            if (rosterIndex < 0 || rosterIndex >= roster.Characters.Count) return;
            SelectedRosterIndex = rosterIndex;
            OnSelectionChanged?.Invoke(rosterIndex);
        }

        public void SetSubTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex > 2) return;
            ActiveSubTab = tabIndex;
            OnSubTabChanged?.Invoke(tabIndex);
        }
    }
}

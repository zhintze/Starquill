using System;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Data;
using Starquill.Display;
using Starquill.Managers;

namespace Starquill.UI
{
    public class PartyScreenController : MonoBehaviour
    {
        [SerializeField] private CharacterFocusDisplay focusDisplay;
        [SerializeField] private PartyPortraitStrip portraitStrip;
        [SerializeField] private RosterGridDisplay rosterGrid;
        [SerializeField] private EquipmentSlotsDisplay equipmentSlots;
        [SerializeField] private ActionLoadoutDisplay actionLoadout;
        [SerializeField] private ScreenManager screenManager;
        [SerializeField] private ExploreSceneController exploreController;
        [SerializeField] private GameObject rosterContent;
        [SerializeField] private GameObject equipmentContent;
        [SerializeField] private GameObject actionsContent;
        [SerializeField] private GameObject portraitStripObj;
        [SerializeField] private GameObject characterInfoObj;
        [SerializeField] private Button[] subTabButtons;

        private CharacterRoster roster;
        private DisplayDataRegistry registry;
        private DisplayBuilder builder;

        public int SelectedRosterIndex { get; private set; }
        public int ActiveSubTab { get; private set; }
        public CharacterInstance SelectedCharacter =>
            roster != null && SelectedRosterIndex >= 0 && SelectedRosterIndex < roster.Characters.Count
                ? roster.Characters[SelectedRosterIndex]
                : null;

        public event Action<int> OnSelectionChanged;
        public event Action<int> OnSubTabChanged;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            SetRoster(gm.Roster);

            registry = DisplayDataRegistry.Instance;
            builder = new DisplayBuilder(registry);

            if (focusDisplay != null) focusDisplay.Initialize(registry, builder);
            if (portraitStrip != null) portraitStrip.Initialize();
            if (equipmentSlots != null) equipmentSlots.Initialize();

            // Wire events
            if (rosterGrid != null) rosterGrid.OnCharacterTapped += SelectCharacter;
            if (portraitStrip != null) portraitStrip.OnPortraitTapped += HandlePortraitTapped;
            if (focusDisplay != null) focusDisplay.OnLevelUpPressed += HandleLevelUp;
            if (actionLoadout != null)
            {
                actionLoadout.OnAvailableTapped += HandleEquipAction;
                actionLoadout.OnEquippedTapped += HandleUnequipAction;
            }

            // Wire sub-tab buttons
            if (subTabButtons != null)
            {
                for (int i = 0; i < subTabButtons.Length; i++)
                {
                    if (subTabButtons[i] == null) continue;
                    int tabIdx = i;
                    subTabButtons[i].onClick.AddListener(() => SetSubTab(tabIdx));
                }
            }

            // Listen for screen changes to refresh when Party tab shown
            if (screenManager != null) screenManager.OnScreenChanged += HandleScreenChanged;

            // Initial sub-tab state
            ApplySubTabVisibility();
            RefreshAll();
        }

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
            RefreshAll();
        }

        public void SetSubTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex > 2) return;
            ActiveSubTab = tabIndex;
            OnSubTabChanged?.Invoke(tabIndex);
            ApplySubTabVisibility();
            RefreshAll();
        }

        private void HandleScreenChanged(int screenIndex)
        {
            // Mute explore when not on explore tab
            if (exploreController != null)
                exploreController.SetMuted(screenIndex != 0);

            // Refresh when Party tab shown
            if (screenIndex == 3)
                RefreshAll();
        }

        private void HandlePortraitTapped(int partySlot)
        {
            if (roster == null) return;
            if (partySlot < 0 || partySlot >= roster.ActivePartyIndices.Length) return;
            int rosterIndex = roster.ActivePartyIndices[partySlot];
            if (rosterIndex >= 0)
                SelectCharacter(rosterIndex);
        }

        private void HandleLevelUp()
        {
            var character = SelectedCharacter;
            if (character == null || !character.CanLevelUp()) return;

            character.LevelUp();
            RefreshAll();

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.BuildPartyFromRoster();
                gm.RebuildVerbPool();
            }
        }

        private void HandleEquipAction(VerbDefinition verb)
        {
            var character = SelectedCharacter;
            if (character == null) return;

            if (character.EquipVerb(verb))
            {
                RefreshAll();

                var gm = GameManager.Instance;
                if (gm != null)
                {
                    gm.BuildPartyFromRoster();
                    gm.RebuildVerbPool();
                }
            }
        }

        private void HandleUnequipAction(int slotIndex)
        {
            var character = SelectedCharacter;
            if (character == null) return;
            if (slotIndex < 0 || slotIndex >= character.equippedVerbs.Count) return;

            var verb = character.equippedVerbs[slotIndex];
            character.UnequipVerb(verb);
            RefreshAll();

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.BuildPartyFromRoster();
                gm.RebuildVerbPool();
            }
        }

        private void ApplySubTabVisibility()
        {
            if (rosterContent != null) rosterContent.SetActive(ActiveSubTab == 0);
            if (equipmentContent != null) equipmentContent.SetActive(ActiveSubTab == 1);
            if (actionsContent != null) actionsContent.SetActive(ActiveSubTab == 2);

            // On Equipment tab, hide portrait strip and character info to give more room
            bool showSidePanel = ActiveSubTab != 1;
            if (portraitStripObj != null) portraitStripObj.SetActive(showSidePanel);
            if (characterInfoObj != null) characterInfoObj.SetActive(showSidePanel);
        }

        private void RefreshAll()
        {
            var character = SelectedCharacter;
            if (character == null) return;

            if (focusDisplay != null) focusDisplay.ShowCharacter(character);
            if (portraitStrip != null && roster != null && registry != null && builder != null)
                portraitStrip.Refresh(roster, SelectedRosterIndex, registry, builder);

            switch (ActiveSubTab)
            {
                case 0:
                    if (rosterGrid != null && roster != null)
                        rosterGrid.Refresh(roster, SelectedRosterIndex);
                    break;
                case 1:
                    if (equipmentSlots != null)
                        equipmentSlots.Refresh(character);
                    break;
                case 2:
                    if (actionLoadout != null)
                        actionLoadout.Refresh(character);
                    break;
            }
        }

        private void OnDestroy()
        {
            if (rosterGrid != null) rosterGrid.OnCharacterTapped -= SelectCharacter;
            if (portraitStrip != null) portraitStrip.OnPortraitTapped -= HandlePortraitTapped;
            if (focusDisplay != null) focusDisplay.OnLevelUpPressed -= HandleLevelUp;
            if (actionLoadout != null)
            {
                actionLoadout.OnAvailableTapped -= HandleEquipAction;
                actionLoadout.OnEquippedTapped -= HandleUnequipAction;
            }
            if (screenManager != null) screenManager.OnScreenChanged -= HandleScreenChanged;
        }
    }
}

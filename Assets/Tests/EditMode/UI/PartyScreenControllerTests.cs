using NUnit.Framework;
using UnityEngine;
using Starquill.Characters;
using Starquill.Data;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    public class PartyScreenControllerTests
    {
        private PartyScreenController controller;
        private CharacterRoster roster;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("PartyScreen");
            controller = go.AddComponent<PartyScreenController>();
            roster = new CharacterRoster();
            for (int i = 0; i < 8; i++)
                roster.AddCharacter(CreateChar($"c{i}"));
            roster.InitializeDefaultParty();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void SetRoster_SetsSelectedToFirstPartyMember()
        {
            controller.SetRoster(roster);
            Assert.AreEqual(0, controller.SelectedRosterIndex);
        }

        [Test]
        public void SelectCharacter_UpdatesIndex()
        {
            controller.SetRoster(roster);
            controller.SelectCharacter(5);
            Assert.AreEqual(5, controller.SelectedRosterIndex);
        }

        [Test]
        public void SelectedCharacter_ReturnsCorrectInstance()
        {
            controller.SetRoster(roster);
            controller.SelectCharacter(3);
            Assert.AreEqual("c3", controller.SelectedCharacter.id);
        }

        [Test]
        public void SelectCharacter_InvalidIndex_DoesNothing()
        {
            controller.SetRoster(roster);
            controller.SelectCharacter(0);
            controller.SelectCharacter(99);
            Assert.AreEqual(0, controller.SelectedRosterIndex);
        }

        [Test]
        public void SetSubTab_UpdatesActiveTab()
        {
            controller.SetRoster(roster);
            controller.SetSubTab(2);
            Assert.AreEqual(2, controller.ActiveSubTab);
        }

        [Test]
        public void OnSelectionChanged_Fires_WhenCharacterSelected()
        {
            controller.SetRoster(roster);
            int firedIndex = -1;
            controller.OnSelectionChanged += idx => firedIndex = idx;
            controller.SelectCharacter(5);
            Assert.AreEqual(5, firedIndex);
        }

        [Test]
        public void OnSubTabChanged_Fires_WhenTabSwitched()
        {
            controller.SetRoster(roster);
            int firedTab = -1;
            controller.OnSubTabChanged += tab => firedTab = tab;
            controller.SetSubTab(1);
            Assert.AreEqual(1, firedTab);
        }

        private CharacterInstance CreateChar(string id)
        {
            return new CharacterInstance
            {
                id = id, displayName = id, level = 1,
                baseStats = new Stats { STR = 5, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
        }
    }
}

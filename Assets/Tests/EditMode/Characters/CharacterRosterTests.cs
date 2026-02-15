using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Tests.Characters
{
    public class CharacterRosterTests
    {
        private CharacterRoster roster;

        [SetUp]
        public void SetUp()
        {
            roster = new CharacterRoster();
        }

        [Test]
        public void AddCharacter_IncreasesCount()
        {
            var c = CreateTestCharacter("c1");
            roster.AddCharacter(c);
            Assert.AreEqual(1, roster.Characters.Count);
        }

        [Test]
        public void AddCharacter_RespectsRosterCap()
        {
            for (int i = 0; i < 20; i++)
                Assert.IsTrue(roster.AddCharacter(CreateTestCharacter($"c{i}")));
            Assert.IsFalse(roster.AddCharacter(CreateTestCharacter("overflow")));
            Assert.AreEqual(20, roster.Characters.Count);
        }

        [Test]
        public void RemoveCharacter_DecreasesCount()
        {
            roster.AddCharacter(CreateTestCharacter("c1"));
            roster.AddCharacter(CreateTestCharacter("c2"));
            roster.RemoveCharacter(0);
            Assert.AreEqual(1, roster.Characters.Count);
        }

        [Test]
        public void SetPartyMember_UpdatesIndices()
        {
            for (int i = 0; i < 8; i++)
                roster.AddCharacter(CreateTestCharacter($"c{i}"));

            roster.SetPartyMember(0, 0);
            roster.SetPartyMember(1, 1);
            roster.SetPartyMember(2, 2);
            roster.SetPartyMember(3, 3);

            var party = roster.GetActiveParty();
            Assert.AreEqual(4, party.Length);
            Assert.AreEqual("c0", party[0].id);
            Assert.AreEqual("c3", party[3].id);
        }

        [Test]
        public void GetActiveParty_ReturnsNullForUnset()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            roster.SetPartyMember(0, 0);
            var party = roster.GetActiveParty();
            Assert.IsNotNull(party[0]);
            Assert.IsNull(party[1]);
        }

        [Test]
        public void RemoveCharacter_AdjustsPartyIndices()
        {
            for (int i = 0; i < 4; i++)
                roster.AddCharacter(CreateTestCharacter($"c{i}"));
            roster.SetPartyMember(0, 0);
            roster.SetPartyMember(1, 2);
            roster.SetPartyMember(2, 3);

            // Remove index 1 (c1, not in party)
            roster.RemoveCharacter(1);

            var party = roster.GetActiveParty();
            Assert.AreEqual("c0", party[0].id);
            Assert.AreEqual("c2", party[1].id);
            Assert.AreEqual("c3", party[2].id);
        }

        [Test]
        public void InitializeDefaultParty_SelectsFirst4()
        {
            for (int i = 0; i < 8; i++)
                roster.AddCharacter(CreateTestCharacter($"c{i}"));
            roster.InitializeDefaultParty();
            var party = roster.GetActiveParty();
            Assert.AreEqual("c0", party[0].id);
            Assert.AreEqual("c3", party[3].id);
        }

        [Test]
        public void IsInParty_ReturnsTrue_WhenInParty()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            roster.SetPartyMember(0, 0);
            Assert.IsTrue(roster.IsInParty(0));
        }

        [Test]
        public void IsInParty_ReturnsFalse_WhenOnBench()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            Assert.IsFalse(roster.IsInParty(0));
        }

        [Test]
        public void GetPartySlot_ReturnsSlot_WhenInParty()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            roster.SetPartyMember(2, 0);
            Assert.AreEqual(2, roster.GetPartySlot(0));
        }

        [Test]
        public void GetPartySlot_ReturnsNegative_WhenNotInParty()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            Assert.AreEqual(-1, roster.GetPartySlot(0));
        }

        [Test]
        public void AddToParty_FillsFirstEmptySlot()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            roster.AddCharacter(CreateTestCharacter("c1"));
            roster.SetPartyMember(0, 0);
            Assert.IsTrue(roster.AddToParty(1));
            Assert.AreEqual(1, roster.GetPartySlot(1));
        }

        [Test]
        public void AddToParty_ReturnsFalse_WhenPartyFull()
        {
            for (int i = 0; i < 5; i++)
                roster.AddCharacter(CreateTestCharacter($"c{i}"));
            for (int i = 0; i < 4; i++)
                roster.SetPartyMember(i, i);
            Assert.IsFalse(roster.AddToParty(4));
        }

        [Test]
        public void RemoveFromParty_ClearsSlot()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            roster.SetPartyMember(0, 0);
            roster.RemoveFromParty(0);
            Assert.IsFalse(roster.IsInParty(0));
        }

        [Test]
        public void SwapPartyMember_SwapsBenchAndParty()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            roster.AddCharacter(CreateTestCharacter("c1"));
            roster.SetPartyMember(0, 0);

            roster.SwapPartyMember(0, 1); // swap party slot 0: c0 out, c1 in

            var party = roster.GetActiveParty();
            Assert.AreEqual("c1", party[0].id);
            Assert.IsFalse(roster.IsInParty(0));
        }

        private CharacterInstance CreateTestCharacter(string id)
        {
            return new CharacterInstance
            {
                id = id,
                displayName = id,
                baseStats = new Stats { STR = 10, DEX = 10, CON = 10, INT = 10, WIS = 10, CHA = 10 }
            };
        }
    }
}

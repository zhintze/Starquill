using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.UI;

namespace Starquill.Tests.EditMode.UI
{
    public class TavernPresenterTests
    {
        private static CharacterInstance Recruit(string name, string species, int level)
        {
            return new CharacterInstance { displayName = name, speciesId = species, level = level };
        }

        [Test]
        public void RowTitle_NameSpeciesLevel()
        {
            Assert.AreEqual("Bram · Dwarf Lv 7", TavernPresenter.RowTitle(Recruit("Bram", "dwarf", 7)));
        }

        [Test]
        public void RowTitle_HandlesMissingSpecies()
        {
            Assert.AreEqual("Bram · Lv 3", TavernPresenter.RowTitle(Recruit("Bram", "", 3)));
        }

        [Test]
        public void SpeciesLabel_CapitalizesFirstLetter()
        {
            Assert.AreEqual("Human", TavernPresenter.SpeciesLabel("human"));
            Assert.AreEqual("", TavernPresenter.SpeciesLabel(null));
        }

        [Test]
        public void PriceText_CompactWithSuffix()
        {
            Assert.AreEqual("2.5Kg", TavernPresenter.PriceText(2500));
            Assert.AreEqual("500g", TavernPresenter.PriceText(500));
        }

        [Test]
        public void CountdownText_HoursMinutesSeconds()
        {
            Assert.AreEqual("New faces in 5:59:59",
                TavernPresenter.CountdownText(5 * 3600 + 59 * 60 + 59));
            Assert.AreEqual("New faces in 0:00:59", TavernPresenter.CountdownText(59));
            Assert.AreEqual("New faces in 0:00:00", TavernPresenter.CountdownText(-5));
        }

        [Test]
        public void StatLine_ContainsNameAndValue()
        {
            var line = TavernPresenter.StatLine(StatType.STR, 24);
            StringAssert.Contains("STR", line);
            StringAssert.Contains("24", line);
        }

        [Test]
        public void VerbLine_ContainsNameAndStat()
        {
            var line = TavernPresenter.VerbLine("Slash", StatType.STR);
            StringAssert.Contains("Slash", line);
            StringAssert.Contains("STR", line);
        }

        [Test]
        public void RecruitLabel_ShowsPrice()
        {
            Assert.AreEqual("RECRUIT · 2.5Kg", TavernPresenter.RecruitLabel(2500));
        }
    }
}

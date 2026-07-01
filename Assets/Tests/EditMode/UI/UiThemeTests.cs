using NUnit.Framework;
using Starquill.UI;

namespace Starquill.Tests.EditMode.UI
{
    public class UiThemeTests
    {
        [Test]
        public void TypeScale_NothingBelowFloor()
        {
            Assert.GreaterOrEqual(UiTheme.FontDisplay, UiTheme.FontFloor);
            Assert.GreaterOrEqual(UiTheme.FontHeading, UiTheme.FontFloor);
            Assert.GreaterOrEqual(UiTheme.FontBody, UiTheme.FontFloor);
            Assert.GreaterOrEqual(UiTheme.FontCaption, UiTheme.FontFloor);
            Assert.GreaterOrEqual(UiTheme.FontFloor, 28f);
        }

        [Test]
        public void TouchTargets_MeetMinimum()
        {
            Assert.GreaterOrEqual(UiTheme.TouchMin, 120f);
            Assert.GreaterOrEqual(UiTheme.NavHeight, UiTheme.TouchMin);
            Assert.GreaterOrEqual(UiTheme.ButtonPrimaryHeight, UiTheme.TouchMin);
            Assert.GreaterOrEqual(UiTheme.CardHeight, UiTheme.TouchMin);
        }

        [Test]
        public void CardMetrics_ChipAndIconFit()
        {
            Assert.GreaterOrEqual(UiTheme.CardHeight, 2f * UiTheme.DeltaChipHeight);
            Assert.LessOrEqual(UiTheme.CardIcon + 2f * UiTheme.CardPadding, UiTheme.CardHeight + UiTheme.CardIcon);
            Assert.LessOrEqual(UiTheme.CardIcon, UiTheme.CardHeight);
        }

        [Test]
        public void Surfaces_ParseToDistinctOpaqueColors()
        {
            Assert.AreEqual(1f, UiTheme.Background.a, 0.001f);
            Assert.AreEqual(1f, UiTheme.Card.a, 0.001f);
            Assert.AreEqual(1f, UiTheme.Sheet.a, 0.001f);
            Assert.AreNotEqual(UiTheme.Background, UiTheme.Card);
            Assert.AreNotEqual(UiTheme.Card, UiTheme.Sheet);
        }
    }
}

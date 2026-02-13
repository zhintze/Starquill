using NUnit.Framework;
using Starquill.Core;

namespace Starquill.Tests.Data
{
    public class StatTypeTests
    {
        [Test] public void STR_IsPhysical() => Assert.AreEqual(VerbCategory.Physical, StatType.STR.GetCategory());
        [Test] public void DEX_IsPhysical() => Assert.AreEqual(VerbCategory.Physical, StatType.DEX.GetCategory());
        [Test] public void CON_IsPhysical() => Assert.AreEqual(VerbCategory.Physical, StatType.CON.GetCategory());
        [Test] public void INT_IsMental() => Assert.AreEqual(VerbCategory.Mental, StatType.INT.GetCategory());
        [Test] public void WIS_IsMental() => Assert.AreEqual(VerbCategory.Mental, StatType.WIS.GetCategory());
        [Test] public void CHA_IsMental() => Assert.AreEqual(VerbCategory.Mental, StatType.CHA.GetCategory());
    }
}

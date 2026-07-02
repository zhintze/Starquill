using System.Linq;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class ClassArchetypeTests
    {
        [Test]
        public void All_ContainsExactly15_OnePerStatPair()
        {
            Assert.AreEqual(15, ClassArchetype.All.Count);
            var pairs = ClassArchetype.All
                .Select(a => (Min: (int)a.StatA < (int)a.StatB ? a.StatA : a.StatB,
                              Max: (int)a.StatA < (int)a.StatB ? a.StatB : a.StatA))
                .Distinct().Count();
            Assert.AreEqual(15, pairs);
        }

        [Test]
        public void ForPair_FindsArchetype_RegardlessOfOrder()
        {
            var a = ClassArchetype.ForPair(StatType.STR, StatType.DEX);
            var b = ClassArchetype.ForPair(StatType.DEX, StatType.STR);
            Assert.AreEqual("Warrior", a.Name);
            Assert.AreSame(a, b);
        }

        [Test]
        public void ById_RoundTrips()
        {
            foreach (var a in ClassArchetype.All)
                Assert.AreSame(a, ClassArchetype.ById(a.Id));
        }
    }
}

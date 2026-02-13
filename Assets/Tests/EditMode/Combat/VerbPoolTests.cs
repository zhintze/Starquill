using NUnit.Framework;
using Starquill.Combat;
using Starquill.Core;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Tests.Combat
{
    public class VerbPoolTests
    {
        private VerbPool pool;

        private VerbDefinition CreateVerb(string id, StatType stat, float cooldown = 2f)
        {
            var verb = ScriptableObject.CreateInstance<VerbDefinition>();
            verb.verbId = id;
            verb.displayName = id;
            verb.statType = stat;
            verb.baseDamage = 50;
            verb.cooldownTicks = cooldown;
            return verb;
        }

        [SetUp]
        public void SetUp() { pool = new VerbPool(3, 10f, seed: 42); }

        [Test]
        public void FillSlots_FillsUpToMax()
        {
            pool.AddVerbs(0, new[] { CreateVerb("v1", StatType.STR), CreateVerb("v2", StatType.DEX) });
            pool.AddVerbs(1, new[] { CreateVerb("v3", StatType.INT), CreateVerb("v4", StatType.WIS) });
            pool.FillSlots(0f);
            Assert.AreEqual(3, pool.DrawnSlots.Count);
        }

        [Test]
        public void FillSlots_DoesNotExceedAvailable()
        {
            pool.AddVerbs(0, new[] { CreateVerb("v1", StatType.STR) });
            pool.FillSlots(0f);
            Assert.AreEqual(1, pool.DrawnSlots.Count);
        }

        [Test]
        public void ActivateVerb_PutsOnCooldown()
        {
            pool.AddVerbs(0, new[] { CreateVerb("v1", StatType.STR), CreateVerb("v2", StatType.DEX), CreateVerb("v3", StatType.INT), CreateVerb("v4", StatType.WIS) });
            pool.FillSlots(0f);
            var activated = pool.ActivateVerb(0);
            Assert.IsNotNull(activated);
            Assert.IsTrue(activated.IsOnCooldown);
            Assert.AreEqual(2, pool.DrawnSlots.Count);
        }

        [Test]
        public void RotateStaleVerbs_RemovesAfterTimeout()
        {
            pool.AddVerbs(0, new[] { CreateVerb("v1", StatType.STR), CreateVerb("v2", StatType.DEX), CreateVerb("v3", StatType.INT), CreateVerb("v4", StatType.WIS) });
            pool.FillSlots(0f);
            var rotated = pool.RotateStaleVerbs(10f);
            Assert.AreEqual(3, rotated.Count);
            Assert.AreEqual(0, pool.DrawnSlots.Count);
        }

        [Test]
        public void RotatedVerbs_AreNotOnCooldown()
        {
            pool.AddVerbs(0, new[] { CreateVerb("v1", StatType.STR), CreateVerb("v2", StatType.DEX), CreateVerb("v3", StatType.INT), CreateVerb("v4", StatType.WIS) });
            pool.FillSlots(0f);
            var rotated = pool.RotateStaleVerbs(10f);
            foreach (var v in rotated) Assert.IsFalse(v.IsOnCooldown);
        }

        [Test]
        public void TickCooldowns_EventuallyFreesVerbs()
        {
            var verb = CreateVerb("v1", StatType.STR, cooldown: 2f);
            pool.AddVerbs(0, new[] { verb, CreateVerb("v2", StatType.DEX) });
            pool.FillSlots(0f);
            pool.ActivateVerb(0);
            pool.TickCooldowns();
            pool.TickCooldowns();
            pool.FillSlots(0f);
            Assert.AreEqual(2, pool.DrawnSlots.Count);
        }
    }
}

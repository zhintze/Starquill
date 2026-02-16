using NUnit.Framework;
using UnityEngine;
using Starquill.Core;
using Starquill.Data;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    public class ActionCardDataTests
    {
        private VerbDefinition MakeVerb(string id, string name, StatType stat,
            float baseDmg, float cooldown, TargetMode target,
            bool isHealing = false, float healAmt = 0f)
        {
            var verb = ScriptableObject.CreateInstance<VerbDefinition>();
            verb.verbId = id;
            verb.displayName = name;
            verb.statType = stat;
            verb.baseDamage = baseDmg;
            verb.cooldownTicks = cooldown;
            verb.targetMode = target;
            verb.statScaling = 1f;
            verb.hitCount = 1;
            verb.isHealingVerb = isHealing;
            verb.healAmount = healAmt;
            return verb;
        }

        [Test]
        public void FromVerb_SetsDisplayName()
        {
            var verb = MakeVerb("slash", "Slash", StatType.STR, 50, 2, TargetMode.Single);
            var card = ActionCardData.FromVerb(verb);
            Assert.AreEqual("Slash", card.DisplayName);
        }

        [Test]
        public void FromVerb_SetsStatType()
        {
            var verb = MakeVerb("fireball", "Fireball", StatType.INT, 65, 3, TargetMode.AoE);
            var card = ActionCardData.FromVerb(verb);
            Assert.AreEqual(StatType.INT, card.StatType);
        }

        [Test]
        public void FromVerb_SetsDamageString_SingleTarget()
        {
            var verb = MakeVerb("slash", "Slash", StatType.STR, 50, 2, TargetMode.Single);
            var card = ActionCardData.FromVerb(verb);
            Assert.AreEqual("50", card.DamageText);
        }

        [Test]
        public void FromVerb_SetsCooldownText()
        {
            var verb = MakeVerb("slash", "Slash", StatType.STR, 50, 3, TargetMode.Single);
            var card = ActionCardData.FromVerb(verb);
            Assert.AreEqual("3s", card.CooldownText);
        }

        [Test]
        public void FromVerb_SetsTargetModeText()
        {
            var verb = MakeVerb("fireball", "Fireball", StatType.INT, 65, 3, TargetMode.AoE);
            var card = ActionCardData.FromVerb(verb);
            Assert.AreEqual("AoE", card.TargetModeText);
        }

        [Test]
        public void FromVerb_HealingVerb_ShowsHealAmount()
        {
            var verb = MakeVerb("heal", "Heal", StatType.WIS, 0, 4, TargetMode.Single, true, 40);
            var card = ActionCardData.FromVerb(verb);
            Assert.AreEqual("40", card.DamageText);
            Assert.IsTrue(card.IsHealing);
        }

        [Test]
        public void FromVerb_StatAbbreviation()
        {
            var verb = MakeVerb("slash", "Slash", StatType.STR, 50, 2, TargetMode.Single);
            var card = ActionCardData.FromVerb(verb);
            Assert.AreEqual("STR", card.StatAbbreviation);
        }

        [Test]
        public void FromVerb_CleaveTarget()
        {
            var verb = MakeVerb("cleave", "Cleave", StatType.STR, 40, 2, TargetMode.Cleave);
            var card = ActionCardData.FromVerb(verb);
            Assert.AreEqual("Cleave", card.TargetModeText);
        }
    }
}

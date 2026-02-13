using Starquill.Data;

namespace Starquill.Combat
{
    public class DrawnVerb
    {
        public VerbDefinition Verb { get; }
        public int OwnerIndex { get; }
        public float DrawTime { get; set; }
        public float CooldownRemaining { get; set; }
        public bool IsOnCooldown => CooldownRemaining > 0;

        public DrawnVerb(VerbDefinition verb, int ownerIndex, float drawTime)
        {
            Verb = verb;
            OwnerIndex = ownerIndex;
            DrawTime = drawTime;
            CooldownRemaining = 0;
        }

        public void StartCooldown() { CooldownRemaining = Verb.cooldownTicks; }
        public void TickCooldown() { if (CooldownRemaining > 0) CooldownRemaining -= 1f; }
    }
}

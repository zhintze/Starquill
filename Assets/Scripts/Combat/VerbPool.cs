using System.Collections.Generic;
using System.Linq;
using Starquill.Data;

namespace Starquill.Combat
{
    public class VerbPool
    {
        public struct PoolEntry
        {
            public VerbDefinition verb;
            public int ownerIndex;
        }

        private readonly List<PoolEntry> allVerbs = new();
        private readonly List<DrawnVerb> drawnSlots = new();
        private readonly List<DrawnVerb> onCooldown = new();
        private readonly int maxSlots;
        private readonly float rotationTime;
        private readonly System.Random rng;

        public IReadOnlyList<DrawnVerb> DrawnSlots => drawnSlots;
        public int AvailableCount => allVerbs.Count - onCooldown.Count;

        public event System.Action<int, DrawnVerb> OnVerbDrawn;
        public event System.Action<int, DrawnVerb> OnVerbRemoved;

        public VerbPool(int maxSlots, float rotationTime, int? seed = null)
        {
            this.maxSlots = maxSlots;
            this.rotationTime = rotationTime;
            this.rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public void AddVerbs(int ownerIndex, IEnumerable<VerbDefinition> verbs)
        {
            foreach (var verb in verbs)
                allVerbs.Add(new PoolEntry { verb = verb, ownerIndex = ownerIndex });
        }

        public void Clear()
        {
            allVerbs.Clear();
            drawnSlots.Clear();
            onCooldown.Clear();
        }

        public void FillSlots(float currentTime)
        {
            while (drawnSlots.Count < maxSlots)
            {
                var drawn = DrawRandom(currentTime);
                if (drawn == null) break;
                drawnSlots.Add(drawn);
                OnVerbDrawn?.Invoke(drawnSlots.Count - 1, drawn);
            }
        }

        public List<DrawnVerb> RotateStaleVerbs(float currentTime)
        {
            var rotated = new List<DrawnVerb>();
            for (int i = drawnSlots.Count - 1; i >= 0; i--)
            {
                if (currentTime - drawnSlots[i].DrawTime >= rotationTime)
                {
                    rotated.Add(drawnSlots[i]);
                    drawnSlots.RemoveAt(i);
                }
            }
            return rotated;
        }

        public void IncrementPassCounts(int activatedSlotIndex)
        {
            for (int i = 0; i < drawnSlots.Count; i++)
            {
                if (i != activatedSlotIndex)
                    drawnSlots[i].IncrementPassCount();
            }
        }

        public List<DrawnVerb> ReplaceStaleVerbs(float currentTime, int passThreshold = 2)
        {
            var removed = new List<DrawnVerb>();
            for (int i = drawnSlots.Count - 1; i >= 0; i--)
            {
                if (drawnSlots[i].PassCount >= passThreshold)
                {
                    var verb = drawnSlots[i];
                    drawnSlots.RemoveAt(i);
                    OnVerbRemoved?.Invoke(i, verb);
                    removed.Add(verb);
                }
            }
            return removed;
        }

        public DrawnVerb ActivateVerb(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= drawnSlots.Count) return null;
            var verb = drawnSlots[slotIndex];
            drawnSlots.RemoveAt(slotIndex);
            OnVerbRemoved?.Invoke(slotIndex, verb);
            verb.StartCooldown();
            onCooldown.Add(verb);
            return verb;
        }

        public void TickCooldowns()
        {
            for (int i = onCooldown.Count - 1; i >= 0; i--)
            {
                onCooldown[i].TickCooldown();
                if (!onCooldown[i].IsOnCooldown)
                    onCooldown.RemoveAt(i);
            }
        }

        private DrawnVerb DrawRandom(float currentTime)
        {
            var unavailable = new HashSet<string>();
            foreach (var d in drawnSlots) unavailable.Add(d.Verb.verbId);
            foreach (var c in onCooldown) unavailable.Add(c.Verb.verbId);

            var available = allVerbs.Where(e => !unavailable.Contains(e.verb.verbId)).ToList();
            if (available.Count == 0) return null;

            var pick = available[rng.Next(available.Count)];
            return new DrawnVerb(pick.verb, pick.ownerIndex, currentTime);
        }
    }
}

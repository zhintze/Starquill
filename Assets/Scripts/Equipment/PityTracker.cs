using System;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Equipment
{
    [Serializable]
    public class PityTracker
    {
        public int killsSinceUncommon;
        public int killsSinceRare;
        public int killsSinceEpic;
        public int killsSinceLegendary;

        public Rarity? RegisterKill(EconomyConfig config)
        {
            killsSinceUncommon++;
            killsSinceRare++;
            killsSinceEpic++;
            killsSinceLegendary++;

            Rarity? guaranteed = null;
            if (killsSinceLegendary >= config.pityLegendary) { guaranteed = Rarity.Legendary; killsSinceLegendary = 0; }
            else if (killsSinceEpic >= config.pityEpic) { guaranteed = Rarity.Epic; killsSinceEpic = 0; }
            else if (killsSinceRare >= config.pityRare) { guaranteed = Rarity.Rare; killsSinceRare = 0; }
            else if (killsSinceUncommon >= config.pityUncommon) { guaranteed = Rarity.Uncommon; killsSinceUncommon = 0; }
            return guaranteed;
        }

        public void RegisterDrop(Rarity rarity)
        {
            if (rarity >= Rarity.Uncommon) killsSinceUncommon = 0;
            if (rarity >= Rarity.Rare) killsSinceRare = 0;
            if (rarity >= Rarity.Epic) killsSinceEpic = 0;
            if (rarity >= Rarity.Legendary) killsSinceLegendary = 0;
        }
    }
}

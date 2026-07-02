using Starquill.Core;

namespace Starquill.Quests
{
    public class QuestZone
    {
        public const int QuestsPerZone = 11;

        public string Id = "";
        public string Name = "";
        public StatType[] DominantTypes = System.Array.Empty<StatType>();
        public string[] IntroDialogue = System.Array.Empty<string>();
        public string[] CompletionDialogue = System.Array.Empty<string>();
    }
}

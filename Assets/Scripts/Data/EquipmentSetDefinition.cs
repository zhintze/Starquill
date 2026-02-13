using UnityEngine;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Equipment Set")]
    public class EquipmentSetDefinition : ScriptableObject
    {
        public string setId;
        public string displayName;

        [Header("Set Bonuses")]
        public string twoPieceDescription;
        public Stats twoPieceStatBonus;

        public string threePieceDescription;
        public Stats threePieceStatBonus;

        public string fourPieceDescription;
        public Stats fourPieceStatBonus;
        public float fourPieceSpecialEffect;
    }
}

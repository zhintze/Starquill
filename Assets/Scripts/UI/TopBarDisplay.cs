using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class TopBarDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text goldLabel;

        [Header("Fragment Bar")]
        [SerializeField] private Image fragmentBarFill;
        [SerializeField] private TMP_Text fragmentLabel;

        [Header("Wave Info")]
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private TMP_Text questLevelLabel;

        public void SetGold(string formatted)
        {
            if (goldLabel != null) goldLabel.text = formatted;
        }

        public void SetFragments(int current, int max)
        {
            if (fragmentLabel != null) fragmentLabel.text = $"{current}/{max}";
            if (fragmentBarFill != null) fragmentBarFill.fillAmount = max > 0 ? (float)current / max : 0f;
        }

        public void SetWaveInfo(int wave, int maxWave)
        {
            if (waveLabel != null) waveLabel.text = $"Wave {wave}/{maxWave}";
        }

        public void SetQuestLevel(int level)
        {
            if (questLevelLabel != null) questLevelLabel.text = $"Quest Lv {level}";
        }
    }
}

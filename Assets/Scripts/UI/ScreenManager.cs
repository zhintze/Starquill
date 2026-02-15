using System;
using UnityEngine;

namespace Starquill.UI
{
    public class ScreenManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] screenPanels;

        public int ActiveScreenIndex { get; private set; }

        public event Action<int> OnScreenChanged;

        public void SetPanels(GameObject[] panels)
        {
            screenPanels = panels;
        }

        public void ShowScreen(int index)
        {
            if (screenPanels == null) return;
            if (index < 0 || index >= screenPanels.Length) return;
            if (screenPanels[index] == null) return;

            for (int i = 0; i < screenPanels.Length; i++)
            {
                if (screenPanels[i] != null)
                    screenPanels[i].SetActive(i == index);
            }

            ActiveScreenIndex = index;
            OnScreenChanged?.Invoke(index);
        }
    }
}

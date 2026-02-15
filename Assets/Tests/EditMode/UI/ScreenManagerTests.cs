using NUnit.Framework;
using UnityEngine;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    public class ScreenManagerTests
    {
        private ScreenManager manager;
        private GameObject[] panels;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("Canvas");
            manager = go.AddComponent<ScreenManager>();
            panels = new GameObject[5];
            for (int i = 0; i < 5; i++)
            {
                panels[i] = new GameObject($"Panel_{i}");
                panels[i].transform.SetParent(go.transform);
            }
            manager.SetPanels(panels);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void ShowScreen_ActivatesTargetPanel()
        {
            manager.ShowScreen(2);
            Assert.IsTrue(panels[2].activeSelf);
        }

        [Test]
        public void ShowScreen_DeactivatesOtherPanels()
        {
            manager.ShowScreen(2);
            Assert.IsFalse(panels[0].activeSelf);
            Assert.IsFalse(panels[1].activeSelf);
            Assert.IsFalse(panels[3].activeSelf);
        }

        [Test]
        public void ShowScreen_InvalidIndex_DoesNothing()
        {
            manager.ShowScreen(0);
            manager.ShowScreen(99);
            Assert.IsTrue(panels[0].activeSelf); // unchanged
        }

        [Test]
        public void ActiveScreenIndex_TracksCurrentScreen()
        {
            manager.ShowScreen(3);
            Assert.AreEqual(3, manager.ActiveScreenIndex);
        }

        [Test]
        public void ShowScreen_NullPanel_SkipsGracefully()
        {
            panels[2].transform.SetParent(null);
            Object.DestroyImmediate(panels[2]);
            panels[2] = null;
            manager.SetPanels(panels);
            manager.ShowScreen(0);
            Assert.AreEqual(0, manager.ActiveScreenIndex);
        }
    }
}

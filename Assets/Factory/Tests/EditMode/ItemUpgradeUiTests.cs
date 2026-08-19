using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class ItemUpgradeUiTests
    {
        [Test]
        public void PanelBuildsDenseRowsAndSwitchesBookmarkTabs()
        {
            var root = new GameObject("Item Upgrade UI Test");
            var catalog = new FactoryTileCatalog();
            var panel = ItemUpgradePanel.Create(root.transform, catalog, new PortalEconomy());

            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.RowCount, Is.EqualTo(PortalSupplyCatalog.Options.Count));
            Assert.That(panel.SelectedCategory, Is.EqualTo(ItemUpgradeCategory.Meso));

            panel.Open();
            panel.SelectCategory(ItemUpgradeCategory.Production);

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.SelectedCategory, Is.EqualTo(ItemUpgradeCategory.Production));

            var panel_rect = panel.transform.Find("Blocker/Panel").GetComponent<RectTransform>();
            Assert.That(panel.transform.Find("Blocker").GetComponent<UnityEngine.UI.Image>(), Is.Null);
            Assert.That(panel_rect.anchorMin, Is.EqualTo(new Vector2(1f, 0.5f)));
            Assert.That(panel_rect.pivot, Is.EqualTo(new Vector2(1f, 0.5f)));
            Assert.That(panel_rect.sizeDelta.x, Is.EqualTo(620f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void HotbarNumberSelectionUsesTheSameToolEventAsClicking()
        {
            var root = new GameObject("Hotbar Input Test");
            var hotbar = FactoryHotbar.Create(
                root.transform,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
            var selected = FactoryBuildTool.None;
            hotbar.ToolClicked += tool => selected = tool;

            hotbar.SelectSlot(0);
            Assert.That(selected, Is.EqualTo(FactoryBuildTool.Conveyor));

            hotbar.SelectSlot(6);
            Assert.That(selected, Is.EqualTo(FactoryBuildTool.Portal));

            hotbar.SelectSlot(9);
            Assert.That(selected, Is.EqualTo(FactoryBuildTool.Portal));
            Object.DestroyImmediate(root);
            var event_system = GameObject.Find("EventSystem");
            if (event_system != null) Object.DestroyImmediate(event_system);
        }

        [Test]
        public void PanelDoesNotOpenOverAnotherModal()
        {
            var root = new GameObject("Item Upgrade Modal Test");
            var catalog = new FactoryTileCatalog();
            var panel = ItemUpgradePanel.Create(root.transform, catalog, new PortalEconomy());
            panel.SetOtherModalCheck(() => true);

            panel.Open();

            Assert.That(panel.IsOpen, Is.False);
            Object.DestroyImmediate(root);
        }
    }
}

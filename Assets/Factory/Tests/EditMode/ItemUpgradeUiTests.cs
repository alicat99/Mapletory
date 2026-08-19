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
            Object.DestroyImmediate(root);
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

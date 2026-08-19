using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory.Tests
{
    public sealed class ProgressionTests
    {
        [Test]
        public void ContentConfigDefinesThreeStagesAndOneInitialGroundPerStage()
        {
            var config = LoadConfig();

            Assert.That(config.Stages.Count, Is.EqualTo(3));
            Assert.That(config.Stages[0].UnlockMesoCost, Is.Zero);
            foreach (var stage in config.Stages)
            {
                Assert.That(stage.HuntingGrounds.FindAll(ground => ground.InitiallyUnlocked).Count,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void LockedStageChargesOnceAndPersistsUnlock()
        {
            var config = LoadConfig();
            var economy = CreateEconomyWithMeso(300L);
            var save = new MemorySave();
            var progression = new FactoryProgression(
                config, economy, save, new FactoryProgressData
                {
                    economy = economy.ExportProgress()
                });

            Assert.That(progression.IsStageUnlocked("stage_1"), Is.True);
            Assert.That(progression.IsStageUnlocked("stage_2"), Is.False);
            Assert.That(progression.TryUnlockStage("stage_2"), Is.True);
            Assert.That(economy.TotalMeso, Is.EqualTo(50L));

            Assert.That(progression.TryUnlockStage("stage_2"), Is.True);
            Assert.That(economy.TotalMeso, Is.EqualTo(50L));
            Assert.That(save.LastProgress.unlocked_stages, Does.Contain("stage_2"));
        }

        [Test]
        public void HuntingGroundPurchaseIsAtomicAndChargesOnlyOnce()
        {
            var config = LoadConfig();
            var economy = new PortalEconomy();
            economy.ImportProgress(new PortalEconomyProgressData
            {
                meso_units = 3000L,
                monsters =
                {
                    new MonsterProgressData
                    {
                        material = RawMaterialType.MonsterSnailRed,
                        lifetime_production = 10L,
                        available_production = 10L
                    }
                }
            });
            var progression = new FactoryProgression(
                config, economy, new MemorySave(), new FactoryProgressData
                {
                    economy = economy.ExportProgress()
                });

            Assert.That(progression.TryUnlockHuntingGround("lith_harbor_outskirts"), Is.True);
            Assert.That(economy.TotalMeso, Is.EqualTo(5L));
            Assert.That(economy.GetAvailableProduction(RawMaterialType.MonsterSnailRed), Is.Zero);

            Assert.That(progression.TryUnlockHuntingGround("lith_harbor_outskirts"), Is.True);
            Assert.That(economy.TotalMeso, Is.EqualTo(5L));
            Assert.That(economy.GetAvailableProduction(RawMaterialType.MonsterSnailRed), Is.Zero);
        }

        [Test]
        public void MissingRequirementDoesNotPartiallyChargeMeso()
        {
            var config = LoadConfig();
            var economy = CreateEconomyWithMeso(30L);
            var progression = new FactoryProgression(
                config, economy, new MemorySave(), new FactoryProgressData
                {
                    economy = economy.ExportProgress()
                });

            Assert.That(progression.TryUnlockHuntingGround("lith_harbor_outskirts"), Is.False);
            Assert.That(economy.TotalMeso, Is.EqualTo(30L));
        }

        [Test]
        public void PortalRejectsDirectSelectionOfLockedMonster()
        {
            var portal = new PortalState(
                Vector2Int.zero,
                new PortalEconomy(),
                material => material == RawMaterialType.MonsterSnailRed);

            Assert.Throws<InvalidOperationException>(() =>
                portal.SelectMaterial(RawMaterialType.MonsterSnailGreen));
            Assert.That(portal.SelectedMaterial, Is.Null);
        }

        [Test]
        public void DebugSettingsRoundTripKeepsUnlockCostsAndRequirements()
        {
            var source = LoadConfig();
            var source_economy = new PortalEconomy();
            source.GetStage("stage_2").SetUnlockMesoCost(777L);
            source.GetHuntingGround("lith_harbor_outskirts").SetRequirement(
                RawMaterialType.MonsterMushroomBlue,
                42L);
            var settings = new FactorySettingsData();
            settings.Capture(source, source_economy);

            var target = LoadConfig();
            settings.Apply(target, new PortalEconomy());

            Assert.That(target.GetStage("stage_2").UnlockMesoCost, Is.EqualTo(777L));
            var ground = target.GetHuntingGround("lith_harbor_outskirts");
            Assert.That(ground.RequiredMaterial, Is.EqualTo(RawMaterialType.MonsterMushroomBlue));
            Assert.That(ground.RequiredAmount, Is.EqualTo(42L));
        }

        [Test]
        public void StageSelectionEntersOnlyTheInitiallyUnlockedStage()
        {
            var root = new GameObject("UI Root");
            var config = LoadConfig();
            var progression = new FactoryProgression(
                config, new PortalEconomy(), new MemorySave(), new FactoryProgressData());
            string entered_stage = null;
            StageSelectionPanel.Create(
                root.transform,
                new FactoryTileCatalog(),
                progression,
                stage_id => entered_stage = stage_id);

            var enter_buttons = root.GetComponentsInChildren<Button>(true)
                .Where(button => button.name == "Button 입장")
                .ToArray();
            enter_buttons[0].onClick.Invoke();
            Assert.That(entered_stage, Is.EqualTo("stage_1"));

            enter_buttons[1].onClick.Invoke();
            Assert.That(entered_stage, Is.EqualTo("stage_1"));
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void PortalSelectionClearlySeparatesUnlockedAndLockedGrounds()
        {
            var root = new GameObject("UI Root");
            var config = LoadConfig();
            var progression = new FactoryProgression(
                config, new PortalEconomy(), new MemorySave(), new FactoryProgressData());
            var panel = PortalSelectionPanel.Create(
                root.transform,
                new FactoryTileCatalog(),
                progression,
                config.GetStage("stage_1"));
            panel.Show(new PortalState(Vector2Int.zero, progression.Economy,
                material => progression.IsMonsterUnlocked("stage_1", material)));

            var buttons = root.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Single(button => button.name == "trail_1").interactable, Is.True);
            Assert.That(buttons.Single(button => button.name == "lith_harbor_outskirts").interactable,
                Is.False);
            Assert.That(buttons.Single(button => button.name == "Unlock lith_harbor_outskirts")
                .gameObject.activeSelf, Is.True);
            Assert.That(panel.RowCount, Is.EqualTo(3));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static FactoryContentConfig LoadConfig()
        {
            return Resources.Load<FactoryContentConfig>(
                "Factory/Progression/FactoryContentConfig").CreateRuntimeCopy();
        }

        private static PortalEconomy CreateEconomyWithMeso(long meso)
        {
            var economy = new PortalEconomy();
            economy.ImportProgress(new PortalEconomyProgressData { meso_units = meso * 100L });
            return economy;
        }

        private sealed class MemorySave : IFactoryProgressSave
        {
            public FactoryProgressData LastProgress { get; private set; }

            public void SaveProgress(FactoryProgressData progress)
            {
                LastProgress = progress;
            }
        }
    }
}

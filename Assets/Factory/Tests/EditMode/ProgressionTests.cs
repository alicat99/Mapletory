using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Maptory.Factory.Tests
{
    public sealed class ProgressionTests
    {
        [Test]
        public void ContentConfigDefinesTwoOrderedStagesAndOneInitialGroundPerStage()
        {
            var config = LoadConfig();

            Assert.That(config.Stages.Count, Is.EqualTo(2));
            Assert.That(config.Stages[0].UnlockMesoCost, Is.Zero);
            Assert.That(config.Stages[0].HuntingGrounds.Select(ground => ground.Monster),
                Is.EqualTo(new[]
                {
                    RawMaterialType.MonsterSnailGreen,
                    RawMaterialType.MonsterSnailRed,
                    RawMaterialType.MonsterSnailBlue
                }));
            Assert.That(config.Stages[1].HuntingGrounds.Select(ground => ground.Monster),
                Is.EqualTo(new[]
                {
                    RawMaterialType.MonsterMushroomBlue,
                    RawMaterialType.MonsterMushroomOrange,
                    RawMaterialType.MonsterMushroomGreen,
                    RawMaterialType.MonsterSpikeMushroomOrange,
                    RawMaterialType.MonsterSpikeMushroomGreen
                }));
            Assert.That(config.Stages[0].HuntingGrounds.Select(ground => ground.UnlockMesoCost),
                Is.EqualTo(new long[] { 0L, 50L, 300L }));
            Assert.That(config.Stages[1].HuntingGrounds.Select(ground => ground.UnlockMesoCost),
                Is.EqualTo(new long[] { 0L, 1000L, 3000L, 5000L, 10000L }));
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
                meso_units = 6000L,
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

            Assert.That(progression.TryUnlockHuntingGround("trail_1"), Is.True);
            Assert.That(economy.TotalMeso, Is.EqualTo(10L));
            Assert.That(economy.GetAvailableProduction(RawMaterialType.MonsterSnailRed), Is.EqualTo(10L));

            Assert.That(progression.TryUnlockHuntingGround("trail_1"), Is.True);
            Assert.That(economy.TotalMeso, Is.EqualTo(10L));
            Assert.That(economy.GetAvailableProduction(RawMaterialType.MonsterSnailRed), Is.EqualTo(10L));
        }

        [Test]
        public void InsufficientMesoDoesNotPartiallyUnlockHuntingGround()
        {
            var config = LoadConfig();
            var economy = CreateEconomyWithMeso(20L);
            var progression = new FactoryProgression(
                config, economy, new MemorySave(), new FactoryProgressData
                {
                    economy = economy.ExportProgress()
                });

            Assert.That(progression.TryUnlockHuntingGround("trail_1"), Is.False);
            Assert.That(economy.TotalMeso, Is.EqualTo(20L));
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
        public void DebugSettingsRoundTripKeepsUnlockEconomyAndMapValues()
        {
            var source = LoadConfig();
            var source_economy = new PortalEconomy();
            source.GetStage("stage_2").SetUnlockMesoCost(777L);
            source.GetHuntingGround("trail_1").SetUnlockMesoCost(42L);
            source_economy.SetBaseValue(RawMaterialType.MonsterSnailGreen, 9f);
            source_economy.SetUpgradeBaseCosts(
                RawMaterialType.MonsterSnailGreen,
                123L,
                456L);
            source_economy.SetUpgradeCostCoefficients(1.7f, 2.3f);
            var settings = new FactorySettingsData();
            settings.Capture(source, source_economy);
            settings.SetMap(new FactoryMapSettingsData
            {
                stage_id = "stage_1",
                width = 2,
                height = 1,
                grass_tiles = { 0, 1 },
                deposits =
                {
                    new DepositSettingsData
                    {
                        material = RawMaterialType.Snail,
                        x = 4,
                        y = 5
                    }
                }
            });

            var target = LoadConfig();
            var target_economy = new PortalEconomy();
            settings.Apply(target, target_economy);

            Assert.That(target.GetStage("stage_2").UnlockMesoCost, Is.EqualTo(777L));
            Assert.That(target.GetHuntingGround("trail_1").UnlockMesoCost, Is.EqualTo(42L));
            Assert.That(target_economy.GetBaseValue(RawMaterialType.MonsterSnailGreen), Is.EqualTo(9f));
            Assert.That(target_economy.GetMesoUpgradeBaseCost(
                RawMaterialType.MonsterSnailGreen), Is.EqualTo(123L));
            Assert.That(target_economy.MesoUpgradeCostCoefficient, Is.EqualTo(1.7f));
            Assert.That(settings.GetMap("stage_1").grass_tiles, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(settings.GetMap("stage_1").deposits[0].x, Is.EqualTo(4));
        }

        [Test]
        public void StageSelectionEntersOnlyTheInitiallyUnlockedStage()
        {
            var existing_event_system = GameObject.Find("EventSystem");
            if (existing_event_system != null)
            {
                UnityEngine.Object.DestroyImmediate(existing_event_system);
            }

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

            var event_system = GameObject.Find("EventSystem");
            Assert.That(event_system, Is.Not.Null);
            Assert.That(event_system.GetComponent<EventSystem>(), Is.Not.Null);
            Assert.That(event_system.GetComponent<BaseInputModule>(), Is.Not.Null);

            var enter_buttons = root.GetComponentsInChildren<Button>(true)
                .Where(button => button.name == "Button 입장")
                .ToArray();
            enter_buttons[0].onClick.Invoke();
            Assert.That(entered_stage, Is.EqualTo("stage_1"));

            enter_buttons[1].onClick.Invoke();
            Assert.That(entered_stage, Is.EqualTo("stage_1"));
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(event_system);
        }

        [Test]
        public void EconomyChangesWaitForAutosaveInsteadOfWritingImmediately()
        {
            var save = new MemorySave();
            var economy = new PortalEconomy();
            var progression = new FactoryProgression(
                LoadConfig(), economy, save, new FactoryProgressData());

            economy.RecordSupply(RawMaterialType.MonsterSnailRed);

            Assert.That(progression.NeedsSave, Is.True);
            Assert.That(save.LastProgress, Is.Null);
            progression.Save();
            Assert.That(progression.NeedsSave, Is.False);
            Assert.That(save.LastProgress.economy.monsters.Single(
                monster => monster.material == RawMaterialType.MonsterSnailRed)
                .lifetime_production, Is.EqualTo(1L));
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
            Assert.That(buttons.Single(button => button.name == "lith_harbor_outskirts").interactable,
                Is.True);
            Assert.That(buttons.Single(button => button.name == "trail_1").interactable,
                Is.False);
            Assert.That(buttons.Single(button => button.name == "Unlock trail_1")
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

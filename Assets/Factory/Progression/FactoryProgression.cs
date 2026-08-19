using System;
using System.Collections.Generic;

namespace Maptory.Factory
{
    public sealed class FactoryProgression
    {
        private readonly FactoryContentConfig config;
        private readonly PortalEconomy economy;
        private readonly IFactoryProgressSave save_service;
        private readonly HashSet<string> unlocked_stages;
        private readonly HashSet<string> unlocked_hunting_grounds;
        private bool defer_save;

        public FactoryContentConfig Config => config;
        public PortalEconomy Economy => economy;
        public event Action Changed;

        public FactoryProgression(
            FactoryContentConfig content_config,
            PortalEconomy portal_economy,
            IFactoryProgressSave service,
            FactoryProgressData progress)
        {
            config = content_config;
            economy = portal_economy;
            save_service = service;
            unlocked_stages = new HashSet<string>(progress.unlocked_stages);
            unlocked_hunting_grounds = new HashSet<string>(progress.unlocked_hunting_grounds);
            economy.ImportProgress(progress.economy);
            economy.Changed += OnEconomyChanged;
        }

        public bool IsStageUnlocked(string stage_id)
        {
            return config.Stages.Count > 0
                && (config.Stages[0].Id == stage_id || unlocked_stages.Contains(stage_id));
        }

        public bool TryUnlockStage(string stage_id)
        {
            var stage = config.GetStage(stage_id);
            if (IsStageUnlocked(stage_id)) return true;

            defer_save = true;
            var purchased = economy.TrySpendMeso(stage.UnlockMesoCost);
            defer_save = false;
            if (!purchased) return false;

            unlocked_stages.Add(stage_id);
            Save();
            return true;
        }

        public bool IsHuntingGroundUnlocked(string hunting_ground_id)
        {
            var ground = config.GetHuntingGround(hunting_ground_id);
            return ground.InitiallyUnlocked || unlocked_hunting_grounds.Contains(hunting_ground_id);
        }

        public bool CanUnlockHuntingGround(string hunting_ground_id)
        {
            var ground = config.GetHuntingGround(hunting_ground_id);
            return IsHuntingGroundUnlocked(hunting_ground_id)
                || economy.CanSpend(
                    ground.UnlockMesoCost,
                    ground.RequiredMaterial,
                    ground.RequiredAmount);
        }

        public bool TryUnlockHuntingGround(string hunting_ground_id)
        {
            var ground = config.GetHuntingGround(hunting_ground_id);
            if (IsHuntingGroundUnlocked(hunting_ground_id)) return true;

            defer_save = true;
            var purchased = economy.TrySpend(
                    ground.UnlockMesoCost,
                    ground.RequiredMaterial,
                    ground.RequiredAmount);
            defer_save = false;
            if (!purchased) return false;

            unlocked_hunting_grounds.Add(hunting_ground_id);
            Save();
            return true;
        }

        public bool IsMonsterUnlocked(string stage_id, RawMaterialType monster)
        {
            var stage = config.GetStage(stage_id);
            foreach (var ground in stage.HuntingGrounds)
            {
                if (ground.Monster == monster) return IsHuntingGroundUnlocked(ground.Id);
            }

            return false;
        }

        public void Save()
        {
            save_service.SaveProgress(new FactoryProgressData
            {
                unlocked_stages = new List<string>(unlocked_stages),
                unlocked_hunting_grounds = new List<string>(unlocked_hunting_grounds),
                economy = economy.ExportProgress()
            });
            Changed?.Invoke();
        }

        private void OnEconomyChanged()
        {
            if (!defer_save) Save();
        }
    }

    public static class FactoryStageSession
    {
        public static string SelectedStageId { get; private set; }

        public static void Select(string stage_id)
        {
            SelectedStageId = stage_id;
        }

        public static void Clear()
        {
            SelectedStageId = null;
        }
    }
}

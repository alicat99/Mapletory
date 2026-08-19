using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    public interface IFactoryProgressSave
    {
        void SaveProgress(FactoryProgressData progress);
    }

    public sealed class FactorySaveService : IFactoryProgressSave
    {
        private const string PROGRESS_KEY = "Maptory.Factory.Progress.v1";
        private const string SETTINGS_KEY = "Maptory.Factory.Settings.v1";

        public FactoryProgressData LoadProgress()
        {
            return PlayerPrefs.HasKey(PROGRESS_KEY)
                ? JsonUtility.FromJson<FactoryProgressData>(PlayerPrefs.GetString(PROGRESS_KEY))
                : new FactoryProgressData();
        }

        public FactorySettingsData LoadSettings()
        {
            return PlayerPrefs.HasKey(SETTINGS_KEY)
                ? JsonUtility.FromJson<FactorySettingsData>(PlayerPrefs.GetString(SETTINGS_KEY))
                : new FactorySettingsData();
        }

        public void SaveProgress(FactoryProgressData progress)
        {
            PlayerPrefs.SetString(PROGRESS_KEY, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }

        public void SaveSettings(FactorySettingsData settings)
        {
            PlayerPrefs.SetString(SETTINGS_KEY, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PROGRESS_KEY);
            PlayerPrefs.Save();
        }
    }

    [Serializable]
    public sealed class FactoryProgressData
    {
        public List<string> unlocked_stages = new();
        public List<string> unlocked_hunting_grounds = new();
        public PortalEconomyProgressData economy = new();
    }

    [Serializable]
    public sealed class FactorySettingsData
    {
        public List<StageSettingsData> stages = new();
        public PortalEconomySettingsData economy = new();

        public void Capture(FactoryContentConfig config, PortalEconomy portal_economy)
        {
            stages.Clear();
            foreach (var stage in config.Stages)
            {
                var stage_data = new StageSettingsData
                {
                    id = stage.Id,
                    unlock_meso_cost = stage.UnlockMesoCost
                };
                foreach (var ground in stage.HuntingGrounds)
                {
                    stage_data.hunting_grounds.Add(new HuntingGroundSettingsData
                    {
                        id = ground.Id,
                        unlock_meso_cost = ground.UnlockMesoCost,
                        required_material = ground.RequiredMaterial,
                        required_amount = ground.RequiredAmount
                    });
                }
                stages.Add(stage_data);
            }

            economy = portal_economy.ExportSettings();
        }

        public void Apply(FactoryContentConfig config, PortalEconomy portal_economy)
        {
            foreach (var stage_data in stages)
            {
                var stage = config.GetStage(stage_data.id);
                stage.SetUnlockMesoCost(stage_data.unlock_meso_cost);
                foreach (var ground_data in stage_data.hunting_grounds)
                {
                    var ground = config.GetHuntingGround(ground_data.id);
                    ground.SetUnlockMesoCost(ground_data.unlock_meso_cost);
                    ground.SetRequirement(ground_data.required_material, ground_data.required_amount);
                }
            }

            portal_economy.ImportSettings(economy);
        }
    }

    [Serializable]
    public sealed class StageSettingsData
    {
        public string id;
        public long unlock_meso_cost;
        public List<HuntingGroundSettingsData> hunting_grounds = new();
    }

    [Serializable]
    public sealed class HuntingGroundSettingsData
    {
        public string id;
        public long unlock_meso_cost;
        public RawMaterialType required_material;
        public long required_amount;
    }
}

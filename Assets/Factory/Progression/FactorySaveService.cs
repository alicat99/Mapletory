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
        private static FactoryProgressData session_progress;
        private static FactorySettingsData session_settings;
        private static FactoryStageCollectionData session_factories;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearSession()
        {
            session_progress = null;
            session_settings = null;
            session_factories = null;
            FactoryStageSession.Clear();
        }

        public FactoryProgressData LoadProgress()
        {
            return session_progress == null
                ? new FactoryProgressData()
                : Copy(session_progress);
        }

        public FactorySettingsData LoadSettings()
        {
            return session_settings == null
                ? new FactorySettingsData()
                : Copy(session_settings);
        }

        public void SaveProgress(FactoryProgressData progress)
        {
            session_progress = Copy(progress);
        }

        public void SaveSettings(FactorySettingsData settings)
        {
            session_settings = Copy(settings);
        }

        public FactoryStageCollectionData LoadFactories()
        {
            return session_factories == null
                ? new FactoryStageCollectionData()
                : Copy(session_factories);
        }

        public void SaveFactories(FactoryStageCollectionData factories)
        {
            session_factories = Copy(factories);
        }

        public void ResetProgress()
        {
            session_progress = null;
            session_factories = null;
        }

        private static T Copy<T>(T data)
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(data));
        }
    }

    [Serializable]
    public sealed class FactoryProgressData
    {
        public List<string> unlocked_stages = new();
        public List<string> unlocked_hunting_grounds = new();
        public PortalEconomyProgressData economy = new();
        public FactoryTutorialProgressData tutorial = new();
        public FactoryObjectiveProgressData objectives = new();
    }

    [Serializable]
    public sealed class FactorySettingsData
    {
        public List<StageSettingsData> stages = new();
        public List<FactoryMapSettingsData> maps = new();
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
                        unlock_meso_cost = ground.UnlockMesoCost
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
                }
            }

            portal_economy.ImportSettings(economy);
        }

        public FactoryMapSettingsData GetMap(string stage_id)
        {
            return maps.Find(map => map.stage_id == stage_id);
        }

        public void SetMap(FactoryMapSettingsData map)
        {
            maps.RemoveAll(saved => saved.stage_id == map.stage_id);
            maps.Add(map);
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
    }

    [Serializable]
    public sealed class FactoryMapSettingsData
    {
        public string stage_id;
        public int width;
        public int height;
        public List<int> grass_tiles = new();
        public List<DepositSettingsData> deposits = new();
    }

    [Serializable]
    public sealed class DepositSettingsData
    {
        public RawMaterialType material;
        public int x;
        public int y;
    }
}

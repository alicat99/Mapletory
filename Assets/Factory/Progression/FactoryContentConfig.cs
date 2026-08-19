using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    [CreateAssetMenu(fileName = "FactoryContentConfig", menuName = "Maptory/Factory Content Config")]
    public sealed class FactoryContentConfig : ScriptableObject
    {
        [SerializeField] private List<FactoryStageDefinition> stages = new();

        public IReadOnlyList<FactoryStageDefinition> Stages => stages;

        public FactoryStageDefinition GetStage(string stage_id)
        {
            return stages.Find(stage => stage.Id == stage_id)
                ?? throw new ArgumentException($"Unknown stage: {stage_id}");
        }

        public HuntingGroundDefinition GetHuntingGround(string hunting_ground_id)
        {
            foreach (var stage in stages)
            {
                var hunting_ground = stage.HuntingGrounds.Find(
                    candidate => candidate.Id == hunting_ground_id);
                if (hunting_ground != null) return hunting_ground;
            }

            throw new ArgumentException($"Unknown hunting ground: {hunting_ground_id}");
        }

        public FactoryContentConfig CreateRuntimeCopy()
        {
            return Instantiate(this);
        }

#if UNITY_EDITOR
        public void SetStagesForEditor(List<FactoryStageDefinition> definitions)
        {
            stages = definitions;
        }
#endif
    }

    [Serializable]
    public sealed class FactoryStageDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string display_name;
        [SerializeField] private long unlock_meso_cost;
        [SerializeField] private int grass_seed;
        [SerializeField] private List<HuntingGroundDefinition> hunting_grounds = new();

        public string Id => id;
        public string DisplayName => display_name;
        public long UnlockMesoCost => unlock_meso_cost;
        public int GrassSeed => grass_seed;
        public List<HuntingGroundDefinition> HuntingGrounds => hunting_grounds;

        public FactoryStageDefinition(
            string stage_id,
            string name,
            long meso_cost,
            int seed,
            params HuntingGroundDefinition[] grounds)
        {
            id = stage_id;
            display_name = name;
            unlock_meso_cost = meso_cost;
            grass_seed = seed;
            hunting_grounds = new List<HuntingGroundDefinition>(grounds);
        }

        public void SetUnlockMesoCost(long value)
        {
            unlock_meso_cost = Math.Max(0L, value);
        }
    }

    [Serializable]
    public sealed class HuntingGroundDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private RawMaterialType monster;
        [SerializeField] private bool initially_unlocked;
        [SerializeField] private long unlock_meso_cost;
        [SerializeField] private RawMaterialType required_material;
        [SerializeField] private long required_amount;

        public string Id => id;
        public RawMaterialType Monster => monster;
        public bool InitiallyUnlocked => initially_unlocked;
        public long UnlockMesoCost => unlock_meso_cost;
        public RawMaterialType RequiredMaterial => required_material;
        public long RequiredAmount => required_amount;
        public PortalSupplyOption SupplyOption => PortalSupplyCatalog.Get(monster);

        public HuntingGroundDefinition(
            string hunting_ground_id,
            RawMaterialType result_monster,
            bool unlocked,
            long meso_cost,
            RawMaterialType material,
            long amount)
        {
            id = hunting_ground_id;
            monster = result_monster;
            initially_unlocked = unlocked;
            unlock_meso_cost = meso_cost;
            required_material = material;
            required_amount = amount;
        }

        public void SetUnlockMesoCost(long value)
        {
            unlock_meso_cost = Math.Max(0L, value);
        }

        public void SetRequirement(RawMaterialType material, long amount)
        {
            required_material = material;
            required_amount = Math.Max(0L, amount);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Maptory.Factory
{
    public sealed class FactoryTileCatalog
    {
        private readonly Dictionary<string, Tile> conveyor_tiles = new();
        private readonly Dictionary<string, Sprite> raw_material_sprites = new();
        private readonly Dictionary<string, Sprite> building_sprites = new();
        private readonly Dictionary<string, Sprite> item_sprites = new();

        public Tile Grass01 { get; }
        public Tile Grass02 { get; }
        public Sprite ConveyorIcon { get; }
        public Sprite ExtractorIcon { get; }

        public FactoryTileCatalog()
        {
            Grass01 = CreateTile(Resources.Load<Sprite>("Factory/Grass/Grass01"));
            Grass02 = CreateTile(Resources.Load<Sprite>("Factory/Grass/Grass02"));

            foreach (var sprite in Resources.LoadAll<Sprite>("Factory/Conveyors"))
            {
                conveyor_tiles.Add(sprite.name, CreateTile(sprite));
            }

            AddSprites("Factory/RawMaterials", raw_material_sprites);
            AddSprites("Factory/Buildings", building_sprites);
            AddSprites("Factory/Items", item_sprites);

            ConveyorIcon = conveyor_tiles["ConveyorUU"].sprite;
            ExtractorIcon = building_sprites["ExtractorU"];
        }

        public Tile GetConveyorTile(string sprite_name)
        {
            return conveyor_tiles[sprite_name];
        }

        public Sprite GetConveyorSprite(string sprite_name)
        {
            return conveyor_tiles[sprite_name].sprite;
        }

        public Sprite GetRawMaterialSprite(RawMaterialType material)
        {
            return raw_material_sprites[material.ToResourceSpriteName()];
        }

        public Sprite GetExtractorSprite(GridDirection direction)
        {
            return building_sprites[$"Extractor{direction.ToSpriteCode()}"];
        }

        public Sprite GetExtractorLowerSprite(GridDirection direction)
        {
            return building_sprites[$"Extractor{direction.ToSpriteCode()}Lower"];
        }

        public Sprite GetExtractorUpperSprite(GridDirection direction)
        {
            return building_sprites[$"Extractor{direction.ToSpriteCode()}Upper"];
        }

        public Sprite GetItemSprite(RawMaterialType material)
        {
            return item_sprites[material.ToItemSpriteName()];
        }

        private static void AddSprites(string resource_path, IDictionary<string, Sprite> sprites)
        {
            foreach (var sprite in Resources.LoadAll<Sprite>(resource_path))
            {
                sprites.Add(sprite.name, sprite);
            }
        }

        private static Tile CreateTile(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            tile.hideFlags = HideFlags.DontSave;
            return tile;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Maptory.Factory
{
    public sealed class FactoryTileCatalog
    {
        private readonly Dictionary<string, Tile> conveyor_tiles = new();

        public Tile Grass01 { get; }
        public Tile Grass02 { get; }
        public Sprite ConveyorIcon { get; }

        public FactoryTileCatalog()
        {
            Grass01 = CreateTile(Resources.Load<Sprite>("Factory/Grass/Grass01"));
            Grass02 = CreateTile(Resources.Load<Sprite>("Factory/Grass/Grass02"));

            foreach (var sprite in Resources.LoadAll<Sprite>("Factory/Conveyors"))
            {
                conveyor_tiles.Add(sprite.name, CreateTile(sprite));
            }

            ConveyorIcon = conveyor_tiles["ConveyorRR"].sprite;
        }

        public Tile GetConveyorTile(string sprite_name)
        {
            return conveyor_tiles[sprite_name];
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

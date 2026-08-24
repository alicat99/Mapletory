using UnityEditor;
using UnityEngine;

namespace Maptory.Factory.Editor
{
    public sealed class FactorySpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Factory/Art/")
                && !assetPath.StartsWith("Assets/Factory/BuildingPorts/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            if (assetPath.StartsWith("Assets/Factory/BuildingPorts/"))
            {
                importer.spritePixelsPerUnit = 32f;
                return;
            }

            if (assetPath.EndsWith("/UI/RoundedRectangle.png"))
            {
                importer.spritePixelsPerUnit = 256f;
                importer.filterMode = FilterMode.Bilinear;
                importer.spriteBorder = new Vector4(127f, 127f, 127f, 127f);
                return;
            }

            if (assetPath.Contains("/Conveyors/")
                || assetPath.Contains("/Buildings/")
                || assetPath.Contains("/Items/")
                || assetPath.Contains("/Monsters/"))
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0.25f);
                importer.SetTextureSettings(settings);
            }
        }
    }
}

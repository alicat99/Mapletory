using UnityEditor;
using UnityEngine;

namespace Maptory.Factory.Editor
{
#pragma warning disable CS0618
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
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 16f;
                importer.spritesheet = CreatePortSprites(
                    assetPath.EndsWith("/InputIcon.png") ? "InputIcon" : "OutputIcon");
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

        private static SpriteMetaData[] CreatePortSprites(string prefix)
        {
            return new[]
            {
                new SpriteMetaData
                {
                    name = prefix + "U",
                    rect = new Rect(0f, 0f, 16f, 16f),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                },
                new SpriteMetaData
                {
                    name = prefix + "R",
                    rect = new Rect(0f, 16f, 16f, 16f),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                },
                new SpriteMetaData
                {
                    name = prefix + "D",
                    rect = new Rect(16f, 16f, 16f, 16f),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                },
                new SpriteMetaData
                {
                    name = prefix + "L",
                    rect = new Rect(16f, 0f, 16f, 16f),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                }
            };
        }
    }
#pragma warning restore CS0618
}

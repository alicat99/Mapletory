using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Maptory.Factory.Editor
{
    public sealed class BuildingSpriteLayerGenerator : AssetPostprocessor
    {
        private const string LARGE_MASK_PATH =
            "Assets/Factory/Art/BuildingProcessing/BuildingLowerMask.png";
        private const string SINGLE_CELL_MASK_PATH =
            "Assets/Factory/Art/BuildingProcessing/BuildingLowerMask1x1.png";
        private const string SOURCE_DIRECTORY =
            "Assets/Factory/Art/Resources/Factory/Buildings";
        private const string OUTPUT_DIRECTORY =
            SOURCE_DIRECTORY + "/Generated";

        [MenuItem("Tools/Maptory/Regenerate Building Layers")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(OUTPUT_DIRECTORY);
            var large_mask = LoadTexture(LARGE_MASK_PATH);
            var single_cell_mask = LoadTexture(SINGLE_CELL_MASK_PATH);

            foreach (var source_path in Directory.GetFiles(
                SOURCE_DIRECTORY,
                "*.png",
                SearchOption.TopDirectoryOnly))
            {
                GenerateLayers(
                    source_path.Replace('\\', '/'),
                    large_mask,
                    single_cell_mask);
            }

            Object.DestroyImmediate(large_mask);
            Object.DestroyImmediate(single_cell_mask);
        }

        private static void GenerateLayers(
            string source_path,
            Texture2D large_mask,
            Texture2D single_cell_mask)
        {
            var source = LoadTexture(source_path);
            var mask = source.width == single_cell_mask.width
                ? single_cell_mask
                : large_mask;
            if (source.width != mask.width || source.height != mask.height)
            {
                Object.DestroyImmediate(source);
                throw new InvalidDataException(
                    $"Building and mask sizes differ: {source_path}");
            }

            var source_pixels = source.GetPixels32();
            var mask_pixels = mask.GetPixels32();
            var lower_pixels = new Color32[source_pixels.Length];
            var upper_pixels = new Color32[source_pixels.Length];

            for (var index = 0; index < source_pixels.Length; index++)
            {
                var source_pixel = source_pixels[index];
                var lower_alpha = (byte)(source_pixel.a * mask_pixels[index].a / 255);
                lower_pixels[index] = new Color32(
                    source_pixel.r,
                    source_pixel.g,
                    source_pixel.b,
                    lower_alpha);
                upper_pixels[index] = new Color32(
                    source_pixel.r,
                    source_pixel.g,
                    source_pixel.b,
                    (byte)(source_pixel.a - lower_alpha));
            }

            var name = Path.GetFileNameWithoutExtension(source_path);
            WriteTexture($"{OUTPUT_DIRECTORY}/{name}Lower.png", source.width, source.height, lower_pixels);
            WriteTexture($"{OUTPUT_DIRECTORY}/{name}Upper.png", source.width, source.height, upper_pixels);
            Object.DestroyImmediate(source);
        }

        private static Texture2D LoadTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path), false);
            return texture;
        }

        private static void WriteTexture(
            string path,
            int width,
            int height,
            Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            var bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(bytes))
            {
                return;
            }

            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void OnPostprocessAllAssets(
            string[] imported_assets,
            string[] deleted_assets,
            string[] moved_assets,
            string[] moved_from_asset_paths)
        {
            if (imported_assets.Any(RequiresRegeneration))
            {
                GenerateAll();
            }
        }

        private static bool RequiresRegeneration(string path)
        {
            return path == LARGE_MASK_PATH
                || path == SINGLE_CELL_MASK_PATH
                || (path.StartsWith(SOURCE_DIRECTORY + "/")
                    && !path.StartsWith(OUTPUT_DIRECTORY + "/")
                    && path.EndsWith(".png"));
        }
    }
}

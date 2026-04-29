using UnityEditor;
using UnityEngine;

namespace VoxelRoad.EditorTools
{
    /// <summary>HUD용 9-slice 스프라이트의 임포트 설정을 일괄 적용. 텍스처 압축 차단.</summary>
    public sealed class HudSpriteImportProcessor : AssetPostprocessor
    {
        private const string CardPath = "Assets/Art/UI/round_card_r12.png";
        private const string StarPath = "Assets/Art/UI/star_white.png";
        private const int BorderPixels = 12;

        private void OnPreprocessTexture()
        {
            if (assetPath == CardPath) ApplyCardSettings();
            else if (assetPath == StarPath) ApplyStarSettings();
        }

        private void ApplyCardSettings()
        {
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = new Vector4(BorderPixels, BorderPixels, BorderPixels, BorderPixels);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }

        private void ApplyStarSettings()
        {
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = Vector4.zero;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}

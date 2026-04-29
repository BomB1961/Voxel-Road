using UnityEditor;
using UnityEngine;

namespace VoxelRoad.EditorTools
{
    /// <summary>HUD용 9-slice 스프라이트의 임포트 설정을 일괄 적용. 텍스처 압축 차단.</summary>
    public sealed class HudSpriteImportProcessor : AssetPostprocessor
    {
        private const string TargetPath = "Assets/Art/UI/round_card_r12.png";
        private const int BorderPixels = 12;

        private void OnPreprocessTexture()
        {
            if (assetPath != TargetPath) return;

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
    }
}

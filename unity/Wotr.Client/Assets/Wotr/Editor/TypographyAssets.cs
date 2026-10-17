using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace Wotr.Editor
{
    public static class TypographyAssets
    {
        private const string Folder = "Assets/Wotr/Resources/Wotr/Fonts/";

        [MenuItem("WoTR/Prepare typography")]
        public static void Prepare()
        {
            var regular = GetOrCreate("Alegreya-Regular");
            var emphasis = GetOrCreate("Alegreya-SemiBold");
            var heading = GetOrCreate("Cinzel-SemiBold");
            SetBold(regular, emphasis);
            SetBold(emphasis, emphasis);
            SetBold(heading, heading);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset("Assets/Wotr/Resources/Wotr/Styles/Typography.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Wotr/Resources/Wotr.uss", ImportAssetOptions.ForceUpdate);
            Debug.Log("WoTR typography: Alegreya Regular/SemiBold and Cinzel SemiBold prepared.");
        }

        private static FontAsset GetOrCreate(string name)
        {
            var path = Folder + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (existing != null)
                return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(Folder + name + ".ttf");
            if (source == null)
                throw new System.InvalidOperationException("Missing font: " + name);
            var asset = FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures)
                AssetDatabase.AddObjectToAsset(texture, asset);
            return asset;
        }

        private static void SetBold(FontAsset source, FontAsset bold)
        {
            var weights = source.fontWeightTable;
            weights[7].regularTypeface = bold;
            EditorUtility.SetDirty(source);
        }
    }
}

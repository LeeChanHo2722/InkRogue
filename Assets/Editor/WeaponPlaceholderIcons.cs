using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Reproducible, original temporary UI art. All five silhouettes also work in one color.
public static class WeaponPlaceholderIcons
{
    private const string Folder = "Assets/Art/UI/WeaponIcons";
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponDefinition"))
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (weapon.Type != WeaponType.Shooter && weapon.Type != WeaponType.SplashBomb &&
                weapon.Type != WeaponType.MachineGun && weapon.Type != WeaponType.Breach && weapon.Type != WeaponType.Blade) continue;
            var texture = new Texture2D(192, 192, TextureFormat.RGBA32, false);
            for (int y = 0; y < 192; y++) for (int x = 0; x < 192; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                    if (Shape(weapon.Type, new Vector2((x + .25f + sx * .5f) / 1.92f, (y + .25f + sy * .5f) / 1.92f))) covered++;
                texture.SetPixel(x, y, new Color(1, 1, 1, covered / 4f));
            }
            texture.Apply();
            string path = Folder + "/" + weapon.Type + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            var serialized = new SerializedObject(weapon);
            serialized.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("WEAPON_PLACEHOLDER_ICONS_GENERATED five distinct sprites assigned to WeaponDefinition.Icon");
    }
    private static bool Box(Vector2 p, float x, float y, float w, float h) => p.x >= x && p.x <= x + w && p.y >= y && p.y <= y + h;
    private static bool Circle(Vector2 p, float x, float y, float r) => (p - new Vector2(x, y)).sqrMagnitude <= r * r;
    private static bool Polygon(Vector2 p, params Vector2[] vertices)
    {
        bool inside = false;
        for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            if ((vertices[i].y > p.y) != (vertices[j].y > p.y) && p.x < (vertices[j].x - vertices[i].x) * (p.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x) inside = !inside;
        return inside;
    }
    private static bool Shape(WeaponType type, Vector2 p)
    {
        switch (type)
        {
            case WeaponType.Shooter:
                return Box(p, 15, 43, 44, 24) || Box(p, 56, 47, 13, 16) || Box(p, 23, 25, 13, 22) || Box(p, 78, 51, 13, 8);
            case WeaponType.SplashBomb:
                return (Circle(p, 45, 42, 25) && !Circle(p, 36, 51, 6)) || Box(p, 37, 65, 16, 9) ||
                    Polygon(p, new Vector2(43, 73), new Vector2(48, 82), new Vector2(60, 84), new Vector2(60, 78), new Vector2(52, 77), new Vector2(49, 70)) ||
                    Circle(p, 78, 64, 7) || Circle(p, 79, 31, 5) || Circle(p, 19, 78, 4);
            case WeaponType.MachineGun:
                return Box(p, 12, 40, 39, 22) || Circle(p, 31, 34, 12) || Box(p, 47, 44, 14, 6) || Box(p, 47, 54, 14, 6) ||
                    Box(p, 68, 43, 13, 7) || Box(p, 76, 55, 15, 7) || Box(p, 64, 69, 16, 7);
            case WeaponType.Breach:
                return Box(p, 60, 17, 8, 20) || Box(p, 60, 66, 8, 20) ||
                    Polygon(p, new Vector2(11, 43), new Vector2(55, 43), new Vector2(55, 31), new Vector2(90, 51), new Vector2(55, 73), new Vector2(55, 59), new Vector2(11, 59));
            case WeaponType.Blade:
                return Polygon(p, new Vector2(37, 35), new Vector2(67, 78), new Vector2(87, 87), new Vector2(82, 63), new Vector2(48, 29)) ||
                    Polygon(p, new Vector2(22, 41), new Vector2(28, 48), new Vector2(58, 19), new Vector2(51, 12)) ||
                    Polygon(p, new Vector2(13, 17), new Vector2(20, 10), new Vector2(36, 27), new Vector2(29, 34));
            default: return false;
        }
    }
}

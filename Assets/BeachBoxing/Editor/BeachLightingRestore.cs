using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BeachBoxing.Editor
{
    public static class BeachLightingRestore
    {
        private const string CubePath = "Assets/BeachBoxing/OriginalArt/World/OriginalSkyReflection.cubemap";
        [Serializable] private sealed class AmbientEvidence { public float[] Workout, Combat; }
        [Serializable] private sealed class MipImports { public MipImport[] textures; }
        [Serializable] private sealed class MipImport { public string path, pngSha256; public int mipsStripped; }

        [MenuItem("Beach Boxing/Restore shipped texture base mips")]
        public static void RestoreStrippedMipBases()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play before restoring texture imports.");
            var evidence = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/BeachBoxing/OriginalArt/World/NativeStrippedMipImports.json");
            var rows = JsonUtility.FromJson<MipImports>(evidence.text).textures;
            foreach (var row in rows)
            {
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    string hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(row.path))).Replace("-", "").ToLowerInvariant();
                    if (hash != row.pngSha256 || row.mipsStripped != 2)
                        throw new InvalidDataException("Verify the native stripped-mip evidence before reimporting " + row.path);
                }
            }
            foreach (var row in rows)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(row.path);
                var settings = new SerializedObject(importer);
                var exemption = settings.FindProperty("m_IgnoreMasterTextureLimit");
                if (exemption.intValue == 1) continue;
                exemption.intValue = 1;
                settings.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }
        }

        [MenuItem("Beach Boxing/Restore source reflection and lighting preview")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play before restoring saved lighting.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated gameplay scene first.");
            RestoreStrippedMipBases();
            RestoreTerrain(flow);
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(CubePath);
            if (!cube || cube.graphicsFormat != GraphicsFormat.RGBA_DXT1_SRGB)
            {
                var previous = cube;
                string rawPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/beach-boxing/evidence/original-reflection-cube.bin"));
                byte[] data = File.ReadAllBytes(rawPath);
                if (data.Length != 65616) throw new InvalidDataException("Expected the source six-face, eight-mip BC1 cubemap.");
                cube = new Cubemap(128, GraphicsFormat.RGBA_DXT1_SRGB, TextureCreationFlags.MipChain);
                cube.name = "Original sky reflection";
                int offset = 0;
                for (int face = 0; face < 6; face++)
                    for (int mip = 0; mip < 8; mip++)
                    {
                        int size = Mathf.Max(1, 128 >> mip);
                        int bytes = Mathf.Max(1, (size + 3) / 4) * Mathf.Max(1, (size + 3) / 4) * 8;
                        cube.SetPixelData(data, mip, (CubemapFace)face, offset);
                        offset += bytes;
                    }
                cube.filterMode = FilterMode.Trilinear;
                cube.wrapMode = TextureWrapMode.Clamp;
                cube.anisoLevel = 0;
                cube.Apply(false, false);
                var textureData = new SerializedObject(cube);
                var lightmapFormat = textureData.FindProperty("m_LightmapFormat");
                if (lightmapFormat != null) lightmapFormat.intValue = 7;
                textureData.ApplyModifiedPropertiesWithoutUndo();
                if (previous)
                {
                    EditorUtility.CopySerialized(cube, previous);
                    UnityEngine.Object.DestroyImmediate(cube);
                    cube = previous;
                    EditorUtility.SetDirty(cube);
                }
                else AssetDatabase.CreateAsset(cube, CubePath);
            }
            var serialized = new SerializedObject(flow);
            serialized.FindProperty("stage.sourceReflection").objectReferenceValue = cube;
            var ambient = JsonUtility.FromJson<AmbientEvidence>(File.ReadAllText(
                Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/beach-boxing/reference/original-ambient.json"))));
            WriteAmbient(serialized.FindProperty("stage.trainingAmbient"), ambient.Workout);
            WriteAmbient(serialized.FindProperty("stage.combatAmbient"), ambient.Combat);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var preview = flow.GetComponent<BeachLightingPreview>();
            if (!preview) preview = flow.gameObject.AddComponent<BeachLightingPreview>();
            preview.flow = flow;
            flow.PreviewSceneLighting();
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(preview);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Restored source 128px reflection cubemap: all six faces and eight prefiltered mip levels; source probe volume is empty.");
        }

        private static void WriteAmbient(SerializedProperty target, float[] values)
        {
            if (values == null || values.Length != 27) throw new InvalidDataException("Expected the observed original 27 SH coefficients.");
            target.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) target.GetArrayElementAtIndex(index).floatValue = values[index];
        }

        private static void RestoreTerrain(BeachGameFlow flow)
        {
            const string sandPath = "Assets/BeachBoxing/OriginalArt/Resources/BeachOriginal/Look/Sand.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(sandPath);
            // The shipped 2022 texture is already stripped by two mip levels (64px base).
            // Unity 2021 importing that base as a PNG would apply the global limit a second time.
            var textureSettings = new SerializedObject(importer);
            textureSettings.FindProperty("m_IgnoreMasterTextureLimit").intValue = 1;
            textureSettings.ApplyModifiedPropertiesWithoutUndo();
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // ASTC always reports an alpha channel, so Terrain ignores layer smoothness
            // and uses opaque PNG alpha as smoothness=1. Native BC1 RGB has no alpha.
            foreach (string platform in new[] { "Standalone", "WebGL", "Android" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                settings.overridden = true;
                settings.format = TextureImporterFormat.RGB24;
                settings.maxTextureSize = 64;
                importer.SetPlatformTextureSettings(settings);
            }
            importer.SaveAndReimport();
            const string materialPath = "Assets/BeachBoxing/OriginalArt/World/OriginalTerrain.mat";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/BeachBoxing/OriginalArt/Shader/OriginalTerrainBRDF3.shader");
            if (!shader) throw new InvalidOperationException("Import the source BRDF3 terrain shader first.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(shader) { name = "Original terrain" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            EditorUtility.SetDirty(material);
            foreach (var terrain in flow.GetComponentsInChildren<Terrain>(true))
            {
                terrain.materialTemplate = material;
                terrain.drawInstanced = false;
                terrain.terrainData.SetBaseMapDirty();
                EditorUtility.SetDirty(terrain);
            }
        }
    }
}

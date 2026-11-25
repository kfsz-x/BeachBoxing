using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class LightingParityTests
    {
        [Test]
        public void SandCannotTurnOpaqueAlphaIntoMirrorSmoothness()
        {
            const string path = "Assets/BeachBoxing/OriginalArt/Resources/BeachOriginal/Look/Sand.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(texture.graphicsFormat), Is.False,
                "RGBA ASTC makes Terrain use alpha=1 instead of the native layer smoothness=0.");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/BeachBoxing/OriginalArt/World/OriginalTerrain.mat");
            Assert.That(material.shader.name, Is.EqualTo("BeachBoxing/Original Terrain BRDF3"));
            Assert.That(ShaderUtil.GetShaderMessages(material.shader).Any(message => message.severity.ToString() == "Error"), Is.False);
        }

        [Test]
        public void ReflectionRetainsNativeSrgbStorageAndAllPrefilteredMips()
        {
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/BeachBoxing/OriginalArt/World/OriginalSkyReflection.cubemap");
            Assert.That(cube, Is.Not.Null);
            Assert.That(new SerializedObject(cube).FindProperty("m_ColorSpace").intValue, Is.EqualTo(1),
                "The shipped texture stores sRGB BC1. Treating its bytes as linear brightens every reflected surface.");
            Assert.That(cube.mipmapCount, Is.EqualTo(8));
            var expected = System.IO.File.ReadAllBytes("docs/beach-boxing/evidence/original-reflection-cube.bin");
            var actual = new System.Collections.Generic.List<byte>();
            for (int face = 0; face < 6; face++)
                for (int mip = 0; mip < 8; mip++)
                    actual.AddRange(cube.GetPixelData<byte>(mip, (CubemapFace)face).ToArray());
            Assert.That(actual.ToArray(), Is.EqualTo(expected), "Keep all source faces and prefiltered mip bytes.");
        }

        [Test]
        public void OriginalGymSurfaceRespondsToTheDirectionalLight()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cameraObject = new GameObject("Lighting parity camera");
            var lightObject = new GameObject("Lighting parity sun");
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/BeachBoxing/OriginalArt/Resources/BeachOriginal/Look/Gym Checkers.mat");
            var material = new Material(source);
            var target = new RenderTexture(32, 32, 24);
            var pixels = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
            var previousTarget = RenderTexture.active;
            var previousSun = RenderSettings.sun;
            int previousPixelLights = QualitySettings.pixelLightCount;
            var previousLights = Object.FindObjectsOfType<Light>();
            var enabled = previousLights.Select(light => light.enabled).ToArray();
            try
            {
                Assert.That(ShaderUtil.GetShaderMessages(material.shader).Any(message => message.severity.ToString() == "Error"), Is.False,
                    "The source forward pass must compile; a supported fallback is not a valid lighting restore.");
                cube.layer = 31;
                cube.transform.position = Vector3.one * 10000;
                material.SetFloat("_OverlayAmount", 0);
                cube.GetComponent<Renderer>().sharedMaterial = material;
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = cube.transform.position + Vector3.back * 4;
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.targetTexture = target;
                var sun = lightObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.renderMode = LightRenderMode.ForcePixel;
                sun.cullingMask = 1 << 31;
                for (int i = 0; i < previousLights.Length; i++)
                {
                    previousLights[i].enabled = false;
                }
                RenderSettings.sun = sun;
                QualitySettings.pixelLightCount = 4;
                sun.intensity = 0;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
                pixels.Apply();
                float unlit = pixels.GetPixel(16, 16).grayscale;
                sun.intensity = 2;
                camera.Render();
                pixels.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
                pixels.Apply();
                Assert.That(pixels.GetPixel(16, 16).grayscale - unlit, Is.GreaterThan(.01f),
                    "The native Prototype_Global is a lit PBR surface, not the previous unlit color approximation.");
            }
            finally
            {
                RenderTexture.active = previousTarget;
                RenderSettings.sun = previousSun;
                QualitySettings.pixelLightCount = previousPixelLights;
                for (int i = 0; i < previousLights.Length; i++) if (previousLights[i]) previousLights[i].enabled = enabled[i];
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(lightObject);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }
    }
}

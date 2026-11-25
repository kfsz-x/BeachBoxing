using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeDecalProjectionTests
    {
        [UnityTest]
        public IEnumerator OriginalDecalProjectsOntoDepthInsteadOfUsingCubeMeshUvs()
        {
            const int fixtureLayer = 30;
            var root = new GameObject("Native decal projection fixture");
            var pattern = new Texture2D(8, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var decalMaterial = new Material(Shader.Find("Tutorial/054_UnlitDynamicDecal"));
            var skinMaterial = new Material(Shader.Find("Standard"));
            var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Mesh cubeMesh = null;
            var previousTarget = RenderTexture.active;
            try
            {
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++) pattern.SetPixel(x, y, x < 4 ? Color.blue : Color.red);
                pattern.Apply();
                decalMaterial.SetTexture("_MainTex", pattern);
                decalMaterial.SetColor("_Color", Color.white);
                decalMaterial.renderQueue = 2601;
                skinMaterial.color = Color.white;
                skinMaterial.SetFloat("_Glossiness", 0);

                var skin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                skin.name = "Opaque skin depth receiver";
                skin.transform.SetParent(root.transform, false);
                skin.transform.localScale = new Vector3(4, 4, .1f);
                skin.layer = fixtureLayer;
                skin.GetComponent<Renderer>().sharedMaterial = skinMaterial;

                var projector = GameObject.CreatePrimitive(PrimitiveType.Cube);
                projector.name = "Original decal volume";
                projector.transform.SetParent(root.transform, false);
                projector.transform.SetPositionAndRotation(new Vector3(0, 0, -.1f), Quaternion.Euler(90, 0, 0));
                projector.transform.localScale = new Vector3(1, .4f, 1);
                projector.layer = fixtureLayer;
                projector.GetComponent<Renderer>().sharedMaterial = decalMaterial;
                cubeMesh = Object.Instantiate(projector.GetComponent<MeshFilter>().sharedMesh);
                // Native Shader315 has no mesh UV input. Collapsing UVs must not
                // collapse the projected pattern to one texel as the dummy shader does.
                cubeMesh.uv = new Vector2[cubeMesh.vertexCount];
                projector.GetComponent<MeshFilter>().sharedMesh = cubeMesh;

                var cameraObject = new GameObject("Depth test camera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.position = new Vector3(0, 0, -3);
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = 1 << fixtureLayer;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 10;
                camera.fieldOfView = 40;
                camera.depthTextureMode = DepthTextureMode.Depth;
                camera.targetTexture = target;
                yield return null;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                pixels.Apply();
                var left = camera.WorldToScreenPoint(new Vector3(-.25f, 0, -.05f));
                var right = camera.WorldToScreenPoint(new Vector3(.25f, 0, -.05f));
                var blue = pixels.GetPixel(Mathf.RoundToInt(left.x), Mathf.RoundToInt(left.y));
                var red = pixels.GetPixel(Mathf.RoundToInt(right.x), Mathf.RoundToInt(right.y));
                Assert.That(blue.b - blue.r, Is.GreaterThan(.5f), "Left skin must receive the blue half of the local-xz decal.");
                Assert.That(red.r - red.b, Is.GreaterThan(.5f), "Right skin must receive red despite every cube UV being zero.");

                // The volume still covers the same screen pixels, but this opaque
                // receiver now lies beyond its local half-unit depth boundary.
                skin.transform.position = new Vector3(0, 0, .5f);
                projector.SetActive(false);
                camera.Render();
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                pixels.Apply();
                var unpainted = pixels.GetPixel(Mathf.RoundToInt(right.x), Mathf.RoundToInt(right.y));
                projector.SetActive(true);
                camera.Render();
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                pixels.Apply();
                var outside = pixels.GetPixel(Mathf.RoundToInt(right.x), Mathf.RoundToInt(right.y));
                Assert.That(Vector4.Distance(unpainted, outside), Is.LessThan(.02f),
                    "A receiver beyond the decal volume must remain unchanged despite sharing its screen footprint.");
            }
            finally
            {
                RenderTexture.active = previousTarget;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cubeMesh);
                Object.DestroyImmediate(pattern);
                Object.DestroyImmediate(decalMaterial);
                Object.DestroyImmediate(skinMaterial);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }
    }
}

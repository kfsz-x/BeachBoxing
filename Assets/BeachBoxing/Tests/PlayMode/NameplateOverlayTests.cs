using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class NameplateOverlayTests
    {
        [UnityTest]
        public IEnumerator SceneGeometryCannotCoverThePlayerNameplate() =>
            NativePlayModeScene.WithScene(true, (flow, _) => CheckOverlay(flow));

        private static IEnumerator CheckOverlay(BeachGameFlow flow)
        {
            flow.GetComponent<BeachNativeTutorialView>().ObserveMove(Vector2.up);
            flow.enabled = false;
            flow.transform.Find("Player").GetComponent<Rigidbody>().isKinematic = true;
            var camera = NativePlayModeScene.SceneCamera(flow);
            var title = flow.transform.Find("Player/Original title canvas");
            var label = title.Find("NameText").GetComponent<Text>();
            label.text = "测试名字";
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            var baseline = ScreenCapture.CaptureScreenshotAsTexture();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
            Texture2D covered = null;
            try
            {
                var center = label.rectTransform.TransformPoint(label.rectTransform.rect.center);
                wall.transform.position = center - camera.transform.forward * .5f;
                wall.transform.rotation = camera.transform.rotation;
                wall.transform.localScale = new Vector3(10, 10, .05f);
                wall.GetComponent<Renderer>().sharedMaterial = material;
                yield return new WaitForEndOfFrame();
                covered = ScreenCapture.CaptureScreenshotAsTexture();
                System.IO.File.WriteAllBytes(Application.dataPath + "/../.superpowers/localization-1123/nameplate-overlay.png", covered.EncodeToPNG());
                var corners = new Vector3[4];
                label.rectTransform.GetWorldCorners(corners);
                Vector3 min = camera.WorldToScreenPoint(corners[0]);
                Vector3 max = camera.WorldToScreenPoint(corners[2]);
                int white = 0, retained = 0;
                for (int y = Mathf.Max(0, (int)min.y); y < Mathf.Min(baseline.height, (int)max.y); y++)
                for (int x = Mathf.Max(0, (int)min.x); x < Mathf.Min(baseline.width, (int)max.x); x++)
                {
                    var before = baseline.GetPixel(x, y);
                    if (before.r < .9f || before.g < .9f || before.b < .9f) continue;
                    white++;
                    // Allow a one-pixel antialiasing change against the solid red background.
                    bool visible = false;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var after = covered.GetPixel(Mathf.Clamp(x + dx, 0, covered.width - 1),
                            Mathf.Clamp(y + dy, 0, covered.height - 1));
                        visible |= after.r > .85f && after.g > .85f && after.b > .85f;
                    }
                    if (visible) retained++;
                }
                Assert.That(white, Is.GreaterThan(20), "The baseline must contain visible text.");
                Assert.That((float)retained / white, Is.GreaterThan(.98f), "Foreground geometry covered the nameplate.");
                foreach (var plate in flow.GetComponentsInChildren<Canvas>(true))
                    if (plate.name == "TitleCanvas" || plate.name == "Original title canvas")
                    {
                        Assert.That(plate.isRootCanvas || plate.overrideSorting, Is.True);
                        Assert.That(plate.sortingOrder, Is.GreaterThan(1000));
                        foreach (var text in plate.GetComponentsInChildren<Text>(true))
                            Assert.That(text.material.shader.name, Is.EqualTo(label.material.shader.name));
                    }
            }
            finally
            {
                Object.Destroy(baseline);
                if (covered) Object.Destroy(covered);
                Object.Destroy(wall);
                Object.Destroy(material);
            }
        }
    }
}

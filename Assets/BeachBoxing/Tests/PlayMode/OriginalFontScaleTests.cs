using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class OriginalFontScaleTests
    {
        [UnityTest]
        public IEnumerator ParentScaleAnimationRefreshesSdfScaleWithoutChangingText()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var source = Object.FindObjectOfType<OriginalSdfText>();
            Assert.That(source, Is.Not.Null, "The saved scene must bind its original font assets.");
            var root = new GameObject("SDF scale fixture", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal;
                var parent = new GameObject("Animated result parent", typeof(RectTransform)).transform;
                parent.SetParent(root.transform, false);
                parent.localScale = Vector3.zero;
                var label = new GameObject("Static caption", typeof(RectTransform), typeof(OriginalSdfText)).GetComponent<OriginalSdfText>();
                label.transform.SetParent(parent, false);
                label.rectTransform.sizeDelta = new Vector2(300, 100);
                label.font = source.font; label.material = source.material;
                label.sourceMetrics = source.sourceMetrics; label.sourceAtlas = source.sourceAtlas;
                label.fontSize = 70; label.text = "AA";
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                var probe = label.gameObject.AddComponent<OriginalFontMeshProbe>();
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(probe.VertexCount, Is.EqualTo(8));
                Assert.That(probe.SdfScale, Is.Zero.Within(0.0001f));

                // No text/font/rect property changes: this is the real countdown/result
                // ancestor-scale animation path, observed at the rendered Canvas mesh.
                parent.localScale = Vector3.one;
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(probe.VertexCount, Is.EqualTo(8));
                Assert.That(probe.SdfScale, Is.EqualTo(1f).Within(0.0001f),
                    "A revealed static caption must update the SDF scale carried to its shader.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

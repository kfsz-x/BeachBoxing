using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class OriginalFontParityTests
    {
        [Test]
        public void AdjacentGlyphsRetainTheNativeFractionalAdvance()
        {
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.OriginalSdfText");
            Assert.That(type, Is.Not.Null, "Native fractional SDF renderer is missing.");
            var go = new GameObject("Font parity fixture", typeof(RectTransform));
            try
            {
                var label = (Text)go.AddComponent(type);
                label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/BeachBoxing/OriginalFont/LilitaOneOriginal.fontsettings");
                type.GetField("sourceMetrics").SetValue(label, AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/BeachBoxing/OriginalFont/LilitaOneNativeMetrics.json"));
                label.fontSize = 70;
                label.text = "AA";
                label.alignment = TextAnchor.UpperLeft;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                using (var mesh = new VertexHelper())
                {
                    type.GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Invoke(label, new object[] { mesh });
                    Assert.That(mesh.currentVertCount, Is.EqualTo(8));
                    var first = UIVertex.simpleVert;
                    var second = UIVertex.simpleVert;
                    mesh.PopulateUIVertex(ref first, 0);
                    mesh.PopulateUIVertex(ref second, 4);
                    // Native glyph index36 (A): advance45.84375 at point size70.
                    Assert.That(second.position.x - first.position.x, Is.EqualTo(45.84375f).Within(0.0001f));
                    var right = UIVertex.simpleVert;
                    mesh.PopulateUIVertex(ref right, 3);
                    // Native ShaderUtilities.GetPadding (wasm table19682/function2616):
                    // plain material has zero face/outline/softness, padding1.25, not atlasPadding5.
                    Assert.That(right.position.x - first.position.x, Is.EqualTo(46.1875f).Within(0.0001f));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void BoldSpacingUsesTheNativeSevenPercentAdvance()
        {
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.OriginalSdfText");
            Assert.That(type, Is.Not.Null, "Native fractional SDF renderer is missing.");
            var go = new GameObject("Bold parity fixture", typeof(RectTransform));
            try
            {
                var label = (Text)go.AddComponent(type);
                label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/BeachBoxing/OriginalFont/LilitaOneOriginal.fontsettings");
                type.GetField("sourceMetrics").SetValue(label, AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/BeachBoxing/OriginalFont/LilitaOneNativeMetrics.json"));
                label.fontSize = 70;
                label.fontStyle = FontStyle.Bold;
                label.text = "AA";
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                using (var mesh = new VertexHelper())
                {
                    type.GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Invoke(label, new object[] { mesh });
                    var first = UIVertex.simpleVert;
                    var second = UIVertex.simpleVert;
                    mesh.PopulateUIVertex(ref first, 0);
                    mesh.PopulateUIVertex(ref second, 4);
                    Assert.That(second.position.x - first.position.x, Is.EqualTo(49.0528125f).Within(0.0001f));
                    Assert.That(first.uv1.y, Is.LessThan(0), "Bold glyphs must reach the SDF shader as bold.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void GlyphLookupRebuildsWhenDomainReloadRetainsOnlySerializableMetadata()
        {
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.OriginalSdfText");
            Assert.That(type, Is.Not.Null);
            var go = new GameObject("Reload parity fixture", typeof(RectTransform));
            try
            {
                var label = (Text)go.AddComponent(type);
                label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/BeachBoxing/OriginalFont/LilitaOneOriginal.fontsettings");
                type.GetField("sourceMetrics").SetValue(label, AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/BeachBoxing/OriginalFont/LilitaOneNativeMetrics.json"));
                label.fontSize = 70; label.text = "AA";
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                var populate = type.GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                using (var mesh = new VertexHelper())
                {
                    populate.Invoke(label, new object[] { mesh });
                    Assert.That(mesh.currentVertCount, Is.EqualTo(8));
                    // Unity hot reload retains serializable private metadata but cannot
                    // serialize Dictionary. Reproduce that partial-cache lifecycle exactly.
                    ((System.Collections.IDictionary)type.GetField("glyphs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(label)).Clear();
                    populate.Invoke(label, new object[] { mesh });
                    Assert.That(mesh.currentVertCount, Is.EqualTo(8), "Reloaded font metadata must repopulate the lost glyph lookup.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}

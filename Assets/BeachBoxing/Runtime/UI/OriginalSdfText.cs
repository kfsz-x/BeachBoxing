using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    /// <summary>
    /// UI.Text-compatible renderer for the shipped Lilita SDF. CharacterInfo truncates
    /// bearings/advances to integers; these quads retain the native TMP glyph metrics.
    /// All Unity objects are assigned in the saved scene by the explicit editor migration.
    /// </summary>
    [AddComponentMenu("Beach Boxing/Original SDF Text")]
    public sealed class OriginalSdfText : Text
    {
        public TextAsset sourceMetrics;
        public Texture2D sourceAtlas;
        [Tooltip("Optional original TMP floating point font size. Zero uses UI.Text.fontSize.")]
        public float nativePointSize;
        public bool useLocalizedFont;
        public bool translateText = true;

        public override string text
        {
            get => base.text;
            set => base.text = useLocalizedFont && translateText ? BeachChineseText.Translate(value) : value;
        }

        [Serializable] private sealed class Metrics
        {
            public float pointSize, scale, ascent, descent, lineHeight, padding;
            public float normalStyle, normalSpacingOffset, boldStyle, boldSpacing, gradientScale, scaleRatioA;
            public int atlasWidth, atlasHeight;
            public Glyph[] glyphs;
        }

        [Serializable] private sealed class Glyph
        {
            public int unicode, x, y, rectWidth, rectHeight;
            public float width, height, bearingX, bearingY, advance;
        }

        // Unity hot reload otherwise restores these serializable private fields while
        // dropping the unsupported Dictionary, leaving a falsely valid empty lookup.
        [NonSerialized] private TextAsset loadedMetrics;
        [NonSerialized] private Metrics metrics;
        [NonSerialized] private float meshLossyScale = float.NaN;
        private readonly Dictionary<int, Glyph> glyphs = new Dictionary<int, Glyph>();
        private readonly List<string> lines = new List<string>();

        public override Texture mainTexture => useLocalizedFont ? base.mainTexture : sourceAtlas ? sourceAtlas : base.mainTexture;

        private void LateUpdate()
        {
            if (useLocalizedFont) return;
            // Countdown/result tracks animate ancestor transforms without changing
            // text or RectTransform dimensions. SDF UV1 must follow the rendered scale.
            float currentScale = Mathf.Abs(rectTransform.lossyScale.y);
            if (!Mathf.Approximately(meshLossyScale, currentScale)) SetVerticesDirty();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            meshLossyScale = float.NaN;
            SetVerticesDirty();
        }

        private bool ReadMetrics()
        {
            if (!sourceMetrics) return false;
            if (loadedMetrics == sourceMetrics && metrics != null && metrics.glyphs != null && glyphs.Count == metrics.glyphs.Length) return true;
            metrics = JsonUtility.FromJson<Metrics>(sourceMetrics.text);
            loadedMetrics = sourceMetrics;
            glyphs.Clear();
            if (metrics == null || metrics.pointSize <= 0 || metrics.glyphs == null) return false;
            foreach (var glyph in metrics.glyphs) glyphs[glyph.unicode] = glyph;
            return true;
        }

        private bool Bold => fontStyle == FontStyle.Bold || fontStyle == FontStyle.BoldAndItalic;
        private float Advance(Glyph glyph) => glyph.advance * (Bold ? 1 + metrics.boldSpacing * 0.01f : 1) + metrics.normalSpacingOffset;
        private Glyph FindGlyph(char value)
        {
            if (value == '\u00a0') value = ' ';
            glyphs.TryGetValue(value, out var glyph);
            return glyph;
        }

        private float LineWidth(string value)
        {
            float width = 0;
            foreach (char character in value)
            {
                var glyph = FindGlyph(character);
                if (glyph != null) width += Advance(glyph);
            }
            return width;
        }

        private void SplitLines(float scale)
        {
            lines.Clear();
            var paragraphs = (text ?? string.Empty).Replace("\r", "").Split('\n');
            float limit = rectTransform.rect.width / Mathf.Max(scale, 0.00001f);
            foreach (var paragraph in paragraphs)
            {
                if (horizontalOverflow == HorizontalWrapMode.Overflow || LineWidth(paragraph) <= limit)
                {
                    lines.Add(paragraph);
                    continue;
                }
                int start = 0, lastSpace = -1;
                float width = 0;
                for (int index = 0; index < paragraph.Length; index++)
                {
                    if (paragraph[index] == ' ') lastSpace = index;
                    var glyph = FindGlyph(paragraph[index]);
                    float next = glyph == null ? 0 : Advance(glyph);
                    if (width + next > limit && index > start)
                    {
                        int end = lastSpace >= start ? lastSpace : index;
                        lines.Add(paragraph.Substring(start, end - start));
                        start = end == lastSpace ? end + 1 : end;
                        index = start - 1;
                        width = 0;
                        lastSpace = -1;
                    }
                    else width += next;
                }
                lines.Add(paragraph.Substring(start));
            }
        }

        private float FontScale()
        {
            float size = nativePointSize > 0 ? nativePointSize : fontSize;
            float scale = size / metrics.pointSize * metrics.scale;
            if (!resizeTextForBestFit) return scale;
            float min = resizeTextMinSize / metrics.pointSize * metrics.scale;
            float max = resizeTextMaxSize / metrics.pointSize * metrics.scale;
            // Preserve Text's existing best-fit bounds, using unrounded native advances.
            for (int step = 0; step < 16; step++)
            {
                float candidate = (min + max) * 0.5f;
                SplitLines(candidate);
                bool fits = lines.Count * metrics.lineHeight * lineSpacing * candidate <= rectTransform.rect.height;
                foreach (var line in lines) fits &= LineWidth(line) * candidate <= rectTransform.rect.width;
                if (fits) min = candidate; else max = candidate;
            }
            return min;
        }

        private float MeshPadding()
        {
            // Shipped ShaderUtilities.GetPadding, wasm table19682/function2616.
            // All recreated core-HUD samples disable extra padding, glow and underlay.
            // AtlasPadding5 describes atlas packing; it is not the rendered quad padding.
            var sourceMaterial = material;
            float face = sourceMaterial && sourceMaterial.HasProperty("_FaceDilate") ? sourceMaterial.GetFloat("_FaceDilate") : 0;
            float outline = sourceMaterial && sourceMaterial.HasProperty("_OutlineWidth") ? sourceMaterial.GetFloat("_OutlineWidth") : 0;
            float softness = sourceMaterial && sourceMaterial.HasProperty("_OutlineSoftness") ? sourceMaterial.GetFloat("_OutlineSoftness") : 0;
            return Mathf.Min((face + outline + softness) * metrics.scaleRatioA, 1) * metrics.gradientScale + 1.25f;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            if (useLocalizedFont)
            {
                base.OnPopulateMesh(mesh);
                return;
            }
            mesh.Clear();
            meshLossyScale = Mathf.Abs(rectTransform.lossyScale.y);
            if (!ReadMetrics()) return;
            float scale = FontScale();
            SplitLines(scale);
            var rect = rectTransform.rect;
            float lineAdvance = metrics.lineHeight * lineSpacing * scale;
            int horizontal = (int)alignment % 3, vertical = (int)alignment / 3;
            float ascent = metrics.ascent * scale;
            float descent = metrics.descent * scale - (lines.Count - 1) * lineAdvance;
            float baseline = vertical == 0 ? rect.yMax - ascent : vertical == 1 ? rect.center.y - (ascent + descent) * 0.5f : rect.yMin - descent;
            float padding = MeshPadding();
            float stylePadding = (Bold ? metrics.boldStyle : metrics.normalStyle) * 0.25f * metrics.gradientScale * metrics.scaleRatioA;
            float sdfScale = scale * meshLossyScale * (Bold ? -1 : 1);
            foreach (var line in lines)
            {
                float lineWidth = LineWidth(line) * scale;
                float pen = horizontal == 0 ? rect.xMin : horizontal == 1 ? rect.center.x - lineWidth * 0.5f : rect.xMax - lineWidth;
                foreach (char character in line)
                {
                    var glyph = FindGlyph(character);
                    if (glyph == null) continue;
                    if (glyph.rectWidth > 0 && glyph.rectHeight > 0)
                    {
                        float left = pen + (glyph.bearingX - padding - stylePadding) * scale;
                        float top = baseline + (glyph.bearingY + padding) * scale;
                        float right = left + (glyph.width + 2 * padding + 2 * stylePadding) * scale;
                        float bottom = top - (glyph.height + 2 * padding) * scale;
                        float u0 = (glyph.x - padding - stylePadding) / metrics.atlasWidth;
                        float v0 = (glyph.y - padding) / metrics.atlasHeight;
                        float u1 = (glyph.x + glyph.rectWidth + padding + stylePadding) / metrics.atlasWidth;
                        float v1 = (glyph.y + glyph.rectHeight + padding) / metrics.atlasHeight;
                        int first = mesh.currentVertCount;
                        AddVertex(mesh, left, bottom, u0, v0, sdfScale);
                        AddVertex(mesh, left, top, u0, v1, sdfScale);
                        AddVertex(mesh, right, top, u1, v1, sdfScale);
                        AddVertex(mesh, right, bottom, u1, v0, sdfScale);
                        mesh.AddTriangle(first, first + 1, first + 2);
                        mesh.AddTriangle(first + 2, first + 3, first);
                    }
                    pen += Advance(glyph) * scale;
                }
                baseline -= lineAdvance;
            }
        }

        private void AddVertex(VertexHelper mesh, float x, float y, float u, float v, float sdfScale)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(x, y);
            vertex.uv0 = new Vector2(u, v);
            vertex.uv1 = new Vector2(0, sdfScale);
            vertex.color = color;
            mesh.AddVert(vertex);
        }

        public override float preferredWidth
        {
            get
            {
                if (useLocalizedFont || !ReadMetrics()) return base.preferredWidth;
                float width = 0;
                foreach (var paragraph in (text ?? string.Empty).Split('\n')) width = Mathf.Max(width, LineWidth(paragraph));
                return width * (nativePointSize > 0 ? nativePointSize : fontSize) / metrics.pointSize * metrics.scale;
            }
        }

        public override float preferredHeight
        {
            get
            {
                if (useLocalizedFont || !ReadMetrics()) return base.preferredHeight;
                float scale = FontScale();
                SplitLines(scale);
                return lines.Count * metrics.lineHeight * lineSpacing * scale;
            }
        }
    }
}

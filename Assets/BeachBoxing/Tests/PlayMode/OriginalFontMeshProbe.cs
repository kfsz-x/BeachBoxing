using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    /// <summary>Observes the actual UI rebuild stream immediately before CanvasRenderer.SetMesh.</summary>
    public sealed class OriginalFontMeshProbe : BaseMeshEffect
    {
        public int VertexCount { get; private set; }
        public float SdfScale { get; private set; }

        public override void ModifyMesh(VertexHelper vertices)
        {
            VertexCount = vertices.currentVertCount;
            if (VertexCount == 0) return;
            var vertex = UIVertex.simpleVert;
            vertices.PopulateUIVertex(ref vertex, 0);
            SdfScale = vertex.uv1.y;
        }
    }
}

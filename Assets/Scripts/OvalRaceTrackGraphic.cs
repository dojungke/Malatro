using UnityEngine;
using UnityEngine.UI;

namespace Malatro
{
    [AddComponentMenu("Malatro/Oval Race Track Graphic")]
    public sealed class OvalRaceTrackGraphic : MaskableGraphic
    {
        [SerializeField, Range(24, 160)] private int segments = 96;
        [SerializeField, Range(2, 8)] private int laneCount = 5;
        [SerializeField, Range(0.35f, 0.9f)] private float innerRadius = 0.58f;
        [SerializeField] private Color turfColor = new(0.08f, 0.28f, 0.14f, 1f);
        [SerializeField] private Color trackColor = new(0.53f, 0.32f, 0.19f, 1f);
        [SerializeField] private Color trackShadeColor = new(0.39f, 0.2f, 0.12f, 1f);
        [SerializeField] private Color railColor = new(0.95f, 0.92f, 0.78f, 0.9f);
        [SerializeField] private Color laneColor = new(1f, 0.93f, 0.75f, 0.2f);

        public float InnerRadius => innerRadius;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var center = rect.center;
            var outer = new Vector2(rect.width * 0.48f, rect.height * 0.46f);
            var inner = outer * innerRadius;

            AddEllipseFan(vh, center, inner * 0.96f, turfColor);
            AddRing(vh, center, inner, outer, trackColor, trackShadeColor);

            for (var lane = 0; lane <= laneCount; lane++)
            {
                var t = lane / (float)laneCount;
                var radius = Vector2.Lerp(inner, outer, t);
                AddLineRing(vh, center, radius, lane == 0 || lane == laneCount ? railColor : laneColor, lane == 0 || lane == laneCount ? 2.4f : 1.1f);
            }

            AddFinishLine(vh, center, inner, outer);
        }

        private void AddEllipseFan(VertexHelper vh, Vector2 center, Vector2 radius, Color fill)
        {
            var centerIndex = vh.currentVertCount;
            vh.AddVert(center, fill, Vector2.zero);
            for (var i = 0; i <= segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                vh.AddVert(center + Ellipse(angle, radius), fill, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                vh.AddTriangle(centerIndex, centerIndex + i + 1, centerIndex + i + 2);
            }
        }

        private void AddRing(VertexHelper vh, Vector2 center, Vector2 inner, Vector2 outer, Color topColor, Color bottomColor)
        {
            var start = vh.currentVertCount;
            for (var i = 0; i <= segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                var shade = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(angle));
                var color = Color.Lerp(bottomColor, topColor, shade);
                vh.AddVert(center + Ellipse(angle, inner), color, Vector2.zero);
                vh.AddVert(center + Ellipse(angle, outer), color, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                var index = start + i * 2;
                vh.AddTriangle(index, index + 2, index + 1);
                vh.AddTriangle(index + 1, index + 2, index + 3);
            }
        }

        private void AddLineRing(VertexHelper vh, Vector2 center, Vector2 radius, Color lineColor, float thickness)
        {
            var start = vh.currentVertCount;
            for (var i = 0; i <= segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                var normal = Ellipse(angle, new Vector2(thickness, thickness));
                var point = center + Ellipse(angle, radius);
                vh.AddVert(point - normal, lineColor, Vector2.zero);
                vh.AddVert(point + normal, lineColor, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                var index = start + i * 2;
                vh.AddTriangle(index, index + 2, index + 1);
                vh.AddTriangle(index + 1, index + 2, index + 3);
            }
        }

        private void AddFinishLine(VertexHelper vh, Vector2 center, Vector2 inner, Vector2 outer)
        {
            const int blocks = 8;
            var x0 = center.x + inner.x;
            var x1 = center.x + outer.x;
            var blockWidth = (x1 - x0) / blocks;
            for (var i = 0; i < blocks; i++)
            {
                var x = x0 + blockWidth * i;
                AddQuad(vh, new Rect(x, center.y - 6f, blockWidth, 6f), i % 2 == 0 ? Color.white : Color.black);
                AddQuad(vh, new Rect(x, center.y, blockWidth, 6f), i % 2 == 0 ? Color.black : Color.white);
            }
        }

        private static Vector2 Ellipse(float angle, Vector2 radius)
        {
            return new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
        }

        private static void AddQuad(VertexHelper vh, Rect rect, Color color)
        {
            var start = vh.currentVertCount;
            vh.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}

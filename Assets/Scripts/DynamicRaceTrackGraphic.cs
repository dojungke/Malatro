using UnityEngine;
using UnityEngine.UI;

namespace Malatro
{
    [AddComponentMenu("Malatro/Dynamic Race Track Graphic")]
    public sealed class DynamicRaceTrackGraphic : MaskableGraphic
    {
        [SerializeField, Range(2, 8)] private int laneCount = 6;
        [SerializeField, Range(12, 96)] private int segments = 48;
        [SerializeField] private Color grassColor = new(0.06f, 0.25f, 0.12f, 1f);
        [SerializeField] private Color trackColor = new(0.48f, 0.29f, 0.17f, 1f);
        [SerializeField] private Color laneColor = new(1f, 0.93f, 0.75f, 0.22f);
        [SerializeField] private Color railColor = new(0.96f, 0.93f, 0.8f, 0.9f);
        [SerializeField] private Color routeColor = new(1f, 0.86f, 0.28f, 0.92f);
        [SerializeField] private Color cornerRouteColor = new(0.2f, 0.62f, 1f, 0.96f);

        private float bend;
        private float routeStartDistance;
        private float routeViewDistance = 34f;
        private float raceDistanceMeters = 1600f;
        private float[] cornerStarts = System.Array.Empty<float>();
        private float[] cornerEnds = System.Array.Empty<float>();

        public float Bend
        {
            get => bend;
            set
            {
                var next = Mathf.Clamp(value, -1.5f, 1.5f);
                if (Mathf.Approximately(bend, next))
                {
                    return;
                }

                bend = next;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            AddQuad(vh, rect, grassColor);

            var trackBottom = rect.yMin + rect.height * 0.18f;
            var trackTop = rect.yMax - rect.height * 0.18f;
            AddTrackStrip(vh, rect, trackBottom, trackTop);
            AddRoutePath(vh, rect, (trackBottom + trackTop) * 0.5f);

            for (var lane = 0; lane <= laneCount; lane++)
            {
                var t = lane / (float)laneCount;
                var baseY = Mathf.Lerp(trackBottom, trackTop, t);
                var strongRail = lane == 0 || lane == laneCount;
                AddCurveLine(vh, rect, baseY, strongRail ? railColor : laneColor, strongRail ? 3f : 1.2f);
            }

        }

        public void SetRoute(float focusDistance, float viewDistance, RaceData raceData)
        {
            routeStartDistance = focusDistance;
            routeViewDistance = Mathf.Max(1f, viewDistance);
            raceDistanceMeters = raceData != null ? Mathf.Max(1f, raceData.TotalDistanceMeters) : 1600f;
            if (raceData?.Corners == null || raceData.Corners.Count == 0)
            {
                cornerStarts = System.Array.Empty<float>();
                cornerEnds = System.Array.Empty<float>();
            }
            else
            {
                if (cornerStarts.Length != raceData.Corners.Count)
                {
                    cornerStarts = new float[raceData.Corners.Count];
                    cornerEnds = new float[raceData.Corners.Count];
                }

                for (var i = 0; i < raceData.Corners.Count; i++)
                {
                    var corner = raceData.Corners[i];
                    cornerStarts[i] = corner != null ? corner.StartDistanceMeters / 8f : 0f;
                    cornerEnds[i] = corner != null ? corner.EndDistanceMeters / 8f : 0f;
                }
            }

            SetVerticesDirty();
        }

        public float GetCurveOffset(float normalizedX)
        {
            // Monotonic turn curve with a small edge slope. This avoids the old
            // center-bulge parabola, but also avoids making the screen edges
            // perfectly horizontal at the end of a corner.
            const float edgeSlope = 1;
            var u = Mathf.Clamp01(normalizedX) * 2f - 1f;
            var a = (edgeSlope - 1f) * 0.5f;
            var b = (3f - edgeSlope) * 0.5f;
            return u * (b + a * u * u) * bend;
        }

        private void AddTrackStrip(VertexHelper vh, Rect rect, float bottom, float top)
        {
            var start = vh.currentVertCount;
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var offset = GetCurveOffset(t) * rect.height * 0.24f;
                vh.AddVert(new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, t), bottom + offset), trackColor, Vector2.zero);
                vh.AddVert(new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, t), top + offset), trackColor, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                var index = start + i * 2;
                vh.AddTriangle(index, index + 2, index + 1);
                vh.AddTriangle(index + 1, index + 2, index + 3);
            }
        }

        private void AddCurveLine(VertexHelper vh, Rect rect, float baseY, Color lineColor, float thickness)
        {
            var start = vh.currentVertCount;
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                var y = baseY + GetCurveOffset(t) * rect.height * 0.24f;
                vh.AddVert(new Vector2(x, y - thickness), lineColor, Vector2.zero);
                vh.AddVert(new Vector2(x, y + thickness), lineColor, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                var index = start + i * 2;
                vh.AddTriangle(index, index + 2, index + 1);
                vh.AddTriangle(index + 1, index + 2, index + 3);
            }
        }

        private void AddRoutePath(VertexHelper vh, Rect rect, float centerY)
        {
            AddRouteSegment(vh, rect, centerY, 0f, 1f, routeColor, 7f);

            for (var i = 0; i < cornerStarts.Length; i++)
            {
                var start = DistanceToViewportX(cornerStarts[i]);
                var end = DistanceToViewportX(cornerEnds[i]);
                if (end < 0f || start > 1f)
                {
                    continue;
                }

                AddRouteSegment(vh, rect, centerY, Mathf.Clamp01(start), Mathf.Clamp01(end), cornerRouteColor, 13f);
            }
        }

        private void AddRouteSegment(VertexHelper vh, Rect rect, float centerY, float startT, float endT, Color color, float thickness)
        {
            var segmentCount = Mathf.Max(2, Mathf.CeilToInt((endT - startT) * segments));
            var start = vh.currentVertCount;
            for (var i = 0; i <= segmentCount; i++)
            {
                var t = Mathf.Lerp(startT, endT, i / (float)segmentCount);
                var x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                var y = centerY + GetCurveOffset(t) * rect.height * 0.24f;
                vh.AddVert(new Vector2(x, y - thickness), color, Vector2.zero);
                vh.AddVert(new Vector2(x, y + thickness), color, Vector2.zero);
            }

            for (var i = 0; i < segmentCount; i++)
            {
                var index = start + i * 2;
                vh.AddTriangle(index, index + 2, index + 1);
                vh.AddTriangle(index + 1, index + 2, index + 3);
            }
        }

        private float DistanceToViewportX(float simulationDistance)
        {
            const float focusX = 0.5f;
            return focusX + (simulationDistance - routeStartDistance) / routeViewDistance;
        }

        private static void AddQuad(VertexHelper vh, Rect rect, Color fill)
        {
            var start = vh.currentVertCount;
            vh.AddVert(new Vector2(rect.xMin, rect.yMin), fill, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMin, rect.yMax), fill, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMax), fill, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMin), fill, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}

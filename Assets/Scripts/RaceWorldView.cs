using System.Collections.Generic;
using UnityEngine;

namespace Malatro
{
    public sealed class RaceWorldView : MonoBehaviour
    {
        private const int TrackSegments = 48;
        private const float CameraFocusX = 0.5f;
        private const float RaceHorseScale = 1.4f;

        private readonly List<LineRenderer> laneLines = new();
        private Transform horseLayer;
        private SpriteRenderer background;
        private SpriteRenderer pathOverlay;
        private SpriteRenderer podium;
        private LineRenderer trackSurface;
        private LineRenderer routeLine;
        private LineRenderer finishLine;
        private Camera raceCamera;
        private bool visible;

        public void Initialize(Camera targetCamera)
        {
            raceCamera = targetCamera;
            if (background != null)
            {
                return;
            }

            background = CreateSpriteRenderer(
                "Track Background",
                Resources.Load<Sprite>("RaceTrack/race-track-background"),
                -100);
            pathOverlay = CreateSpriteRenderer(
                "Track Path",
                Resources.Load<Sprite>("RaceTrack/race-path-overlay"),
                -70);
            pathOverlay.color = new Color(1f, 1f, 1f, 0.24f);
            podium = CreateSpriteRenderer(
                "Winners Podium",
                Resources.Load<Sprite>("RaceTrack/race-podium"),
                10);
            podium.gameObject.SetActive(false);

            trackSurface = CreateLine("Track Surface", new Color(0.34f, 0.2f, 0.11f, 1f), 5.4f, -50);
            routeLine = CreateLine("Route Highlight", new Color(1f, 0.72f, 0.16f, 0.88f), 0.12f, -35);
            finishLine = CreateLine("Finish Line", Color.white, 0.12f, -20);

            for (var i = 0; i <= 6; i++)
            {
                var line = CreateLine(
                    $"Lane Line {i + 1}",
                    i == 0 || i == 6
                        ? new Color(1f, 0.94f, 0.78f, 0.92f)
                        : new Color(1f, 0.94f, 0.78f, 0.24f),
                    i == 0 || i == 6 ? 0.08f : 0.035f,
                    -30);
                laneLines.Add(line);
            }

            horseLayer = new GameObject("Race Horses").transform;
            horseLayer.SetParent(transform, false);
            gameObject.SetActive(false);
        }

        public void SetVisible(bool visible, IReadOnlyList<Horse> horses)
        {
            if (this.visible == visible)
            {
                return;
            }

            this.visible = visible;
            gameObject.SetActive(visible);
            if (horses == null)
            {
                return;
            }

            if (visible && horseLayer != null)
            {
                for (var i = 0; i < horseLayer.childCount; i++)
                {
                    horseLayer.GetChild(i).gameObject.SetActive(false);
                }
            }

            foreach (var horse in horses)
            {
                if (horse?.Visual == null)
                {
                    continue;
                }

                horse.Visual.SetActive(visible);
                if (visible)
                {
                    horse.Visual.transform.SetParent(horseLayer, false);
                    if (horse.Renderer != null)
                    {
                        horse.Renderer.enabled = true;
                    }
                }
            }
        }

        public void UpdateView(
            IReadOnlyList<Horse> horses,
            Horse trackedHorse,
            float focusDistance,
            float viewDistance,
            float trackLength,
            float bend,
            float raceTime)
        {
            if (raceCamera == null || horses == null)
            {
                return;
            }

            var halfHeight = raceCamera.orthographicSize;
            var halfWidth = halfHeight * raceCamera.aspect;
            var left = -halfWidth;
            var right = halfWidth;
            var width = right - left;
            var trackCenterY = -0.45f;
            var trackHalfHeight = Mathf.Min(2.7f, halfHeight * 0.42f);
            if (trackedHorse != null)
            {
                var trackedLaneY = Mathf.Lerp(
                    trackHalfHeight * 0.82f,
                    -trackHalfHeight * 0.82f,
                    trackedHorse.LaneOffset);
                var trackedCurveY = GetCurveOffset(CameraFocusX, bend, halfHeight);
                trackCenterY = -trackedLaneY - trackedCurveY;
            }

            SetRaceCourseVisible(true);
            podium.gameObject.SetActive(false);
            ScaleSpriteToCamera(background, halfWidth, halfHeight);
            ScaleSpriteToCamera(pathOverlay, halfWidth, halfHeight);
            pathOverlay.transform.localPosition = new Vector3(0f, trackCenterY + 0.45f, 0f);

            UpdateCurveLine(trackSurface, left, right, trackCenterY, bend, halfHeight, 0f);
            UpdateCurveLine(routeLine, left, right, trackCenterY, bend, halfHeight, 0f);
            for (var i = 0; i < laneLines.Count; i++)
            {
                var laneT = i / (float)Mathf.Max(1, laneLines.Count - 1);
                var laneY = Mathf.Lerp(trackHalfHeight, -trackHalfHeight, laneT);
                var outerRail = i == 0 || i == laneLines.Count - 1;
                var laneAlpha = outerRail
                    ? 0.9f
                    : Mathf.Lerp(0.24f, 0.045f, Mathf.SmoothStep(0f, 1f, raceTime / 3f));
                var laneColor = new Color(1f, 0.94f, 0.78f, laneAlpha);
                laneLines[i].startColor = laneColor;
                laneLines[i].endColor = laneColor;
                UpdateCurveLine(laneLines[i], left, right, trackCenterY, bend, halfHeight, laneY);
            }

            foreach (var horse in horses)
            {
                if (horse?.Visual == null)
                {
                    continue;
                }

                var normalizedX = CameraFocusX + (horse.Distance - focusDistance) / Mathf.Max(1f, viewDistance);
                var x = left + normalizedX * width;
                var laneY = Mathf.Lerp(trackHalfHeight * 0.82f, -trackHalfHeight * 0.82f, horse.LaneOffset);
                var curveY = GetCurveOffset(normalizedX, bend, halfHeight);
                horse.Visual.transform.localPosition = new Vector3(x, trackCenterY + laneY + curveY, 0f);
                horse.Visual.transform.localRotation = Quaternion.Euler(0f, 0f, GetCurveSlope(normalizedX, bend) * 7f);

                var perspective = Mathf.Lerp(0.92f, 1.12f, horse.LaneOffset);
                horse.Visual.transform.localScale = Vector3.one * (RaceHorseScale * perspective);
                if (horse.Renderer != null)
                {
                    horse.Renderer.sortingOrder = 20 + Mathf.RoundToInt(horse.LaneOffset * 10f);
                }

            }

            foreach (var horse in horses)
            {
                if (horse?.Visual == null)
                {
                    continue;
                }

                if (horse.RideTarget?.Visual != null && horse.RideTimer > 0f)
                {
                    var targetTransform = horse.RideTarget.Visual.transform;
                    var targetSprite = horse.RideTarget.Renderer != null
                        ? horse.RideTarget.Renderer.sprite
                        : null;
                    var headOffset = targetSprite != null
                        ? targetSprite.bounds.max.y * targetTransform.localScale.y + 0.08f
                        : targetTransform.localScale.y * 0.55f;
                    horse.Visual.transform.localPosition = targetTransform.localPosition + Vector3.up * headOffset;
                    horse.Visual.transform.localRotation = targetTransform.localRotation;
                    horse.Visual.transform.localScale = targetTransform.localScale;
                    if (horse.Renderer != null)
                    {
                        var targetOrder = horse.RideTarget.Renderer != null
                            ? horse.RideTarget.Renderer.sortingOrder
                            : 20;
                        horse.Renderer.sortingOrder = targetOrder + 12;
                    }
                }

                var effects = horse.Visual.GetComponent<HorseWorldEffects>();
                if (effects != null)
                {
                    effects.UpdateEffects(horse);
                }
            }

            var finishNormalizedX = CameraFocusX + (trackLength - focusDistance) / Mathf.Max(1f, viewDistance);
            var finishX = left + finishNormalizedX * width;
            var finishVisible = finishNormalizedX >= -0.05f && finishNormalizedX <= 1.05f;
            finishLine.gameObject.SetActive(finishVisible);
            if (finishVisible)
            {
                var finishY = trackCenterY + GetCurveOffset(finishNormalizedX, bend, halfHeight);
                finishLine.positionCount = 2;
                finishLine.SetPosition(0, new Vector3(finishX, finishY - trackHalfHeight, 0f));
                finishLine.SetPosition(1, new Vector3(finishX, finishY + trackHalfHeight, 0f));
            }
        }

        public void ShowPodium(IReadOnlyList<Horse> standings)
        {
            if (raceCamera == null || standings == null)
            {
                return;
            }

            var halfHeight = raceCamera.orthographicSize;
            var halfWidth = halfHeight * raceCamera.aspect;
            ScaleSpriteToCamera(background, halfWidth, halfHeight);
            SetRaceCourseVisible(false);
            podium.gameObject.SetActive(podium.sprite != null);
            if (podium.sprite != null)
            {
                var size = podium.sprite.bounds.size;
                var targetWidth = halfWidth * 1.5f;
                var scale = targetWidth / Mathf.Max(0.01f, size.x);
                podium.transform.localScale = Vector3.one * scale;
                podium.transform.localPosition = new Vector3(0f, -1.15f, 0f);
            }

            var podiumPositions = new[]
            {
                new Vector3(0f, 2.35f, 0f),
                new Vector3(-4.05f, 1.15f, 0f),
                new Vector3(4.05f, 0.45f, 0f)
            };

            for (var i = 0; i < standings.Count; i++)
            {
                var horse = standings[i];
                if (horse?.Visual == null)
                {
                    continue;
                }

                var onPodium = i < podiumPositions.Length;
                horse.Visual.SetActive(onPodium);
                if (!onPodium)
                {
                    continue;
                }

                horse.Visual.transform.SetParent(horseLayer, false);
                horse.Visual.transform.localPosition = podiumPositions[i];
                horse.Visual.transform.localRotation = Quaternion.identity;
                horse.Visual.transform.localScale = Vector3.one * (i == 0 ? 1.55f : 1.4f);
                if (horse.Renderer != null)
                {
                    horse.Renderer.enabled = true;
                    horse.Renderer.sortingOrder = 30 + (3 - i);
                }

                var effects = horse.Visual.GetComponent<HorseWorldEffects>();
                if (effects != null)
                {
                    effects.UpdateEffects(horse);
                }
            }
        }

        private void SetRaceCourseVisible(bool raceVisible)
        {
            pathOverlay.gameObject.SetActive(raceVisible);
            trackSurface.gameObject.SetActive(raceVisible);
            routeLine.gameObject.SetActive(raceVisible);
            finishLine.gameObject.SetActive(raceVisible);
            foreach (var laneLine in laneLines)
            {
                laneLine.gameObject.SetActive(raceVisible);
            }
        }

        private SpriteRenderer CreateSpriteRenderer(string objectName, Sprite sprite, int order)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private LineRenderer CreateLine(string objectName, Color color, float width, int order)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 2;
            line.sortingOrder = order;
            return line;
        }

        private static void ScaleSpriteToCamera(SpriteRenderer renderer, float halfWidth, float halfHeight)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            var size = renderer.sprite.bounds.size;
            renderer.transform.localPosition = new Vector3(0f, 0f, 0f);
            renderer.transform.localScale = new Vector3(
                halfWidth * 2f / Mathf.Max(0.01f, size.x),
                halfHeight * 2f / Mathf.Max(0.01f, size.y),
                1f);
        }

        private static void UpdateCurveLine(
            LineRenderer line,
            float left,
            float right,
            float centerY,
            float bend,
            float halfHeight,
            float laneOffset)
        {
            line.positionCount = TrackSegments + 1;
            for (var i = 0; i <= TrackSegments; i++)
            {
                var t = i / (float)TrackSegments;
                var x = Mathf.Lerp(left, right, t);
                var y = centerY + laneOffset + GetCurveOffset(t, bend, halfHeight);
                line.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        private static float GetCurveOffset(float normalizedX, float bend, float halfHeight)
        {
            // Keep the curve monotonic so it reads as a turn, but do not flatten
            // the screen edges. If the edge slope is 0, the last part of the track
            // becomes horizontal and feels detached from the ongoing corner.
            var curve = GetTurnCurve(normalizedX);
            return curve * bend * halfHeight * 0.28f;
        }

        private static float GetCurveSlope(float normalizedX, float bend)
        {
            return GetTurnCurveSlope(normalizedX) * bend;
        }

        private static float GetTurnCurve(float normalizedX)
        {
            const float edgeSlope = 1;
            var u = Mathf.Clamp01(normalizedX) * 2f - 1f;
            var a = (edgeSlope - 1f) * 0.5f;
            var b = (3f - edgeSlope) * 0.5f;
            return u * (b + a * u * u);
        }

        private static float GetTurnCurveSlope(float normalizedX)
        {
            const float edgeSlope = 1f;
            var u = Mathf.Clamp01(normalizedX) * 2f - 1f;
            var a = (edgeSlope - 1f) * 0.5f;
            var b = (3f - edgeSlope) * 0.5f;
            return 2f * (b + 3f * a * u * u);
        }
    }
}

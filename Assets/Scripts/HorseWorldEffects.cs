using UnityEngine;

namespace Malatro
{
    public sealed class HorseWorldEffects : MonoBehaviour
    {
        private const int RingSegments = 32;

        private SpriteRenderer horseRenderer;
        private SpriteRenderer aura;
        private readonly SpriteRenderer[] trails = new SpriteRenderer[3];
        private readonly SpriteRenderer[] stunStars = new SpriteRenderer[3];
        private LineRenderer outerRing;
        private LineRenderer crosshair;
        private TextMesh statusText;
        private float previousSkillTimer;
        private float skillBurstClock;
        private Color baseHorseColor = Color.white;

        public void Initialize(SpriteRenderer renderer)
        {
            horseRenderer = renderer;
            if (horseRenderer != null)
            {
                baseHorseColor = horseRenderer.color;
            }

            aura = CreateSprite("Skill Aura", CreateCircleSprite(), 10);
            aura.color = Color.clear;

            for (var i = 0; i < trails.Length; i++)
            {
                trails[i] = CreateSprite($"Skill Trail {i + 1}", renderer != null ? renderer.sprite : null, -1 - i);
                trails[i].color = Color.clear;
            }

            for (var i = 0; i < stunStars.Length; i++)
            {
                stunStars[i] = CreateSprite($"Stun Star {i + 1}", CreateStarSprite(), 14);
                stunStars[i].color = Color.clear;
                stunStars[i].transform.localScale = Vector3.one * 0.18f;
            }

            outerRing = CreateLine("Status Ring", 16);
            crosshair = CreateLine("Skill Crosshair", 15);

            var textObject = new GameObject("Status Text");
            textObject.transform.SetParent(transform, false);
            statusText = textObject.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.MiddleCenter;
            statusText.alignment = TextAlignment.Center;
            statusText.fontSize = 48;
            statusText.characterSize = 0.035f;
            statusText.fontStyle = FontStyle.Bold;
            statusText.color = Color.clear;
            statusText.GetComponent<MeshRenderer>().sortingOrder = 18;
        }

        public void UpdateEffects(Horse horse)
        {
            if (horse == null || horseRenderer == null)
            {
                return;
            }

            if (horse.SkillEffectTimer > previousSkillTimer + 0.05f)
            {
                skillBurstClock = 0f;
            }
            previousSkillTimer = horse.SkillEffectTimer;
            skillBurstClock += Time.deltaTime;

            var stunned = horse.StunTimer > 0f;
            var timeStopped = horse.TimeStopTimer > 0f;
            var slowed = horse.SpeedMultiplierTimer > 0f && horse.SpeedMultiplier < 1f;
            var skillActive = horse.SkillEffectTimer > 0f && horse.Skill != null;

            horseRenderer.color = timeStopped
                ? new Color(0.58f, 0.45f, 1f, 0.82f)
                : stunned
                    ? new Color(1f, 0.72f, 0.25f, 0.86f)
                    : slowed
                        ? Color.Lerp(baseHorseColor, new Color(0.35f, 0.72f, 1f, 1f), 0.48f)
                        : baseHorseColor;

            UpdateStun(stunned);
            UpdateTimeStop(timeStopped);
            UpdateSkill(horse, skillActive, stunned || timeStopped);
            UpdateStatusText(horse, stunned, timeStopped, slowed, skillActive);
        }

        private void UpdateStun(bool active)
        {
            for (var i = 0; i < stunStars.Length; i++)
            {
                var star = stunStars[i];
                if (!active)
                {
                    star.color = Color.clear;
                    continue;
                }

                var angle = Time.time * 240f + i * 120f;
                var radians = angle * Mathf.Deg2Rad;
                star.transform.localPosition = new Vector3(
                    Mathf.Cos(radians) * 0.48f,
                    0.72f + Mathf.Sin(radians) * 0.16f,
                    0f);
                star.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
                star.color = new Color(1f, 0.78f, 0.12f, 0.95f);
            }
        }

        private void UpdateTimeStop(bool active)
        {
            if (!active)
            {
                if (!IsSkillUsingRing())
                {
                    outerRing.positionCount = 0;
                }
                return;
            }

            var pulse = 0.82f + Mathf.Sin(Time.time * 8f) * 0.08f;
            DrawRing(outerRing, pulse, new Color(0.55f, 0.34f, 1f, 0.96f));
            outerRing.transform.localRotation = Quaternion.Euler(62f, 0f, Time.time * 75f);
        }

        private void UpdateSkill(Horse horse, bool active, bool statusOverridesRing)
        {
            if (!active)
            {
                aura.color = Color.clear;
                crosshair.positionCount = 0;
                for (var i = 0; i < trails.Length; i++)
                {
                    trails[i].color = Color.clear;
                }
                if (!statusOverridesRing)
                {
                    outerRing.positionCount = 0;
                }
                return;
            }

            var skill = horse.Skill;
            var color = skill.EffectColor;
            var amount = horse.SkillEffectAmount;
            var pulse = 1f + Mathf.Sin(Time.time * 18f) * 0.12f;
            aura.color = new Color(color.r, color.g, color.b, 0.2f + amount * 0.24f);
            aura.transform.localScale = Vector3.one * (1.25f + amount * 0.4f) * pulse;

            UpdateTrails(skill, color, amount);
            if (!statusOverridesRing)
            {
                UpdateSkillShape(skill, color, amount);
            }
        }

        private void UpdateTrails(HorseSkillData skill, Color color, float amount)
        {
            var showTrails = skill.EffectType == HorseSkillEffectType.StandardBoost
                || skill.EffectType == HorseSkillEffectType.LateCharge
                || skill.EffectType == HorseSkillEffectType.KnightStrike
                || skill.EffectType == HorseSkillEffectType.Transfer
                || skill.EffectType == HorseSkillEffectType.StarStair
                || skill.EffectType == HorseSkillEffectType.Leap
                || skill.EffectType == HorseSkillEffectType.OvertakeTrip;

            for (var i = 0; i < trails.Length; i++)
            {
                var trail = trails[i];
                if (!showTrails || horseRenderer.sprite == null)
                {
                    trail.color = Color.clear;
                    continue;
                }

                trail.sprite = horseRenderer.sprite;
                trail.transform.localPosition = new Vector3(-0.22f * (i + 1), 0.03f * i, 0f);
                trail.transform.localScale = Vector3.one * (1f - i * 0.08f);
                var alpha = amount * (0.24f / (i + 1));
                if (skill.EffectType == HorseSkillEffectType.Transfer)
                {
                    trail.transform.localPosition += Vector3.up * Mathf.Sin(Time.time * 16f + i) * 0.12f;
                    alpha *= 1.5f;
                }
                trail.color = new Color(color.r, color.g, color.b, alpha);
            }
        }

        private void UpdateSkillShape(HorseSkillData skill, Color color, float amount)
        {
            crosshair.positionCount = 0;
            switch (skill.EffectType)
            {
                case HorseSkillEffectType.Sniper:
                    DrawCrosshair(color);
                    break;
                case HorseSkillEffectType.AreaStun:
                case HorseSkillEffectType.AreaSlow:
                    DrawRing(
                        outerRing,
                        0.55f + skillBurstClock * 1.35f,
                        new Color(color.r, color.g, color.b, Mathf.Clamp01(amount * 0.85f)));
                    outerRing.transform.localRotation = Quaternion.identity;
                    break;
                case HorseSkillEffectType.TimeStop:
                    DrawRing(outerRing, 0.78f, color);
                    outerRing.transform.localRotation = Quaternion.Euler(62f, 0f, Time.time * 75f);
                    break;
                case HorseSkillEffectType.StarStair:
                    DrawRing(outerRing, 0.62f + Mathf.PingPong(Time.time * 0.7f, 0.18f), color);
                    outerRing.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 90f);
                    break;
                case HorseSkillEffectType.KnightStrike:
                    DrawSlash(color);
                    outerRing.positionCount = 0;
                    break;
                default:
                    DrawRing(outerRing, 0.62f + amount * 0.12f, color);
                    outerRing.transform.localRotation = Quaternion.identity;
                    break;
            }
        }

        private void UpdateStatusText(Horse horse, bool stunned, bool timeStopped, bool slowed, bool skillActive)
        {
            statusText.transform.position = transform.position + Vector3.up * 1.05f;
            statusText.transform.rotation = Quaternion.identity;

            if (stunned)
            {
                statusText.text = "STUN";
                statusText.color = new Color(1f, 0.78f, 0.12f, 1f);
            }
            else if (timeStopped)
            {
                statusText.text = "TIME STOP";
                statusText.color = new Color(0.7f, 0.55f, 1f, 1f);
            }
            else if (slowed)
            {
                statusText.text = "SLOW 30%";
                statusText.color = new Color(0.4f, 0.78f, 1f, 1f);
            }
            else if (skillActive)
            {
                statusText.text = horse.SkillMessage;
                var color = horse.Skill.EffectColor;
                statusText.color = new Color(color.r, color.g, color.b, horse.SkillEffectAmount);
            }
            else
            {
                statusText.text = string.Empty;
                statusText.color = Color.clear;
            }
        }

        private void DrawCrosshair(Color color)
        {
            DrawRing(outerRing, 0.58f, new Color(color.r, color.g, color.b, 0.9f));
            outerRing.transform.localRotation = Quaternion.identity;
            crosshair.positionCount = 2;
            crosshair.startColor = color;
            crosshair.endColor = color;
            crosshair.SetPosition(0, new Vector3(-0.62f, 0f, 0f));
            crosshair.SetPosition(1, new Vector3(0.62f, 0f, 0f));
        }

        private void DrawSlash(Color color)
        {
            crosshair.positionCount = 2;
            crosshair.startColor = color;
            crosshair.endColor = new Color(color.r, color.g, color.b, 0.1f);
            var travel = Mathf.Repeat(skillBurstClock * 3.5f, 1.4f) - 0.7f;
            crosshair.SetPosition(0, new Vector3(travel - 0.55f, -0.52f, 0f));
            crosshair.SetPosition(1, new Vector3(travel + 0.55f, 0.52f, 0f));
        }

        private bool IsSkillUsingRing()
        {
            return previousSkillTimer > 0f;
        }

        private SpriteRenderer CreateSprite(string objectName, Sprite sprite, int sortingOrder)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private LineRenderer CreateLine(string objectName, int sortingOrder)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startWidth = 0.045f;
            line.endWidth = 0.045f;
            line.numCapVertices = 2;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private static void DrawRing(LineRenderer line, float radius, Color color)
        {
            line.positionCount = RingSegments + 1;
            line.startColor = color;
            line.endColor = color;
            for (var i = 0; i <= RingSegments; i++)
            {
                var angle = i / (float)RingSegments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Horse Effect Circle",
                filterMode = FilterMode.Bilinear
            };
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    var alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateStarSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Horse Effect Star",
                filterMode = FilterMode.Bilinear
            };
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x - center) / center;
                    var dy = Mathf.Abs(y - center) / center;
                    var alpha = Mathf.Clamp01(1f - Mathf.Min(dx + dy * 0.35f, dy + dx * 0.35f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}

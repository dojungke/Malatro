using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Malatro
{
    // 프로토타입의 게임 상태, 경주 시뮬레이션, 즉시 모드 UI를 한곳에서 관리한다.
    public sealed class MalatroPrototype : MonoBehaviour
    {
        private const int FieldSize = 6;
        private const int BaseTicketCount = 3;
        private const int RelicShopOfferCount = 3;
        private const int StartingRelicShopRefreshCost = 30;
        private const int RacesPerRound = 3;
        private const int StartingRoundTarget = 50;
        private const float DefaultManaCost = 100f;
        private const float CameraViewDistance = 34f;

        private readonly List<Horse> field = new();
        private readonly List<BetTicket> offeredTickets = new();
        private readonly List<RelicData> relicShopOffers = new();
        private readonly System.Random rng = new();

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonStyle;
        private GUIStyle navStyle;
        private GUIStyle centeredStyle;
        private Font uiFont;
        private Texture2D whiteTexture;
        private Camera mainCamera;
        private HorseDatabase horseDatabase;
        private RelicDatabase relicDatabase;
        private RaceDatabase raceDatabase;
        private RaceData currentRace;
        private readonly RelicInventory relicInventory = new RelicInventory();

        private GamePhase phase = GamePhase.Betting;
        private List<Horse> latestStandings = new();
        private int raceNumber = 1;
        private int roundNumber = 1;
        private int roundTargetGold = StartingRoundTarget;
        private int roundEarnedGold;
        private int relicShopRefreshCost = StartingRelicShopRefreshCost;
        private int gold = 100;
        private int hits;
        private int totalTicketsResolved;
        private float raceClock;
        private float resultDelay;
        private bool runComplete;
        private UiLanguage language = UiLanguage.Korean;
        private int cameraTargetLane = -1;
        private float cameraDistance;
        private string logKey = "choose_ticket";
        private object[] logArgs = Array.Empty<object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // 씬에 별도 오브젝트를 배치하지 않아도 플레이 모드에서 프로토타입을 시작한다.
            if (FindAnyObjectByType<MalatroPrototype>() != null)
            {
                return;
            }

            var gameObject = new GameObject("Malatro Prototype");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<MalatroPrototype>();
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 6.25f;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = new Color(0.055f, 0.07f, 0.075f);

            whiteTexture = Texture2D.whiteTexture;
            horseDatabase = HorseDatabase.LoadOrCreateRuntimeDefaults();
            relicDatabase = RelicDatabase.LoadOrCreateRuntimeDefaults();
            raceDatabase = RaceDatabase.LoadOrCreateRuntimeDefaults();
            StartNewRun();
        }

        private void Update()
        {
            if (phase == GamePhase.Racing)
            {
                TickRace(Time.deltaTime);
                UpdateRaceCamera(Time.deltaTime);
            }
            else if (phase == GamePhase.Results)
            {
                resultDelay = Mathf.Max(0f, resultDelay - Time.deltaTime);
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                ToggleLanguage();
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                CycleCameraTarget(-1);
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                CycleCameraTarget(1);
            }

            if (phase == GamePhase.Betting)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    StartRace();
                }
            }
            else if (phase == GamePhase.Results && Keyboard.current.spaceKey.wasPressedThisFrame && resultDelay <= 0f)
            {
                PrepareNextRace();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            DrawTopPanel();
            DrawTrack();

            switch (phase)
            {
                case GamePhase.Betting:
                    DrawBettingPanel();
                    break;
                case GamePhase.Racing:
                    DrawRacePanel();
                    break;
                case GamePhase.Results:
                    DrawResultsPanel();
                    break;
            }
        }

        private void StartNewRun()
        {
            foreach (var horse in field)
            {
                if (horse.Visual != null)
                {
                    Destroy(horse.Visual);
                }
            }

            field.Clear();
            offeredTickets.Clear();
            relicShopOffers.Clear();
            relicInventory.Clear();
            latestStandings.Clear();
            raceNumber = 1;
            roundNumber = 1;
            roundTargetGold = StartingRoundTarget;
            roundEarnedGold = 0;
            relicShopRefreshCost = StartingRelicShopRefreshCost;
            gold = 100;
            hits = 0;
            totalTicketsResolved = 0;
            raceClock = 0f;
            resultDelay = 0f;
            runComplete = false;
            phase = GamePhase.Betting;
            cameraTargetLane = -1;
            cameraDistance = 0f;

            var entries = horseDatabase.Horses
                .Where(data => data != null)
                .Take(FieldSize)
                .ToList();
            if (entries.Count < FieldSize)
            {
                horseDatabase = HorseDatabase.CreateRuntimeDefaults();
                entries = horseDatabase.Horses.Take(FieldSize).ToList();
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var data = entries[i];
                var horse = new Horse(
                    data,
                    data.EnglishName,
                    i,
                    data.Speed.Roll(rng),
                    data.Acceleration.Roll(rng),
                    data.Stamina.Roll(rng),
                    data.Magic.Roll(rng),
                    RollFloat(data.OpeningOddsRange.x, data.OpeningOddsRange.y),
                    data.SkillData,
                    data.UiColor);

                horse.Visual = CreateHorseVisual(horse);
                field.Add(horse);
            }

            NormalizeOpeningOdds();
            GenerateRaceData();
            GenerateTickets();
            SetLog("pick_ticket");
        }

        private GameObject CreateHorseVisual(Horse horse)
        {
            var visual = new GameObject(horse.Name);
            var renderer = visual.AddComponent<SpriteRenderer>();
            var texture = horse.Data != null ? horse.Data.RunSheet : null;

            if (texture != null)
            {
                var frameWidth = texture.width / 2f;
                var pixelsPerUnit = texture.height / 0.9f;
                var firstFrame = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, frameWidth, texture.height),
                    new Vector2(0.5f, 0.42f),
                    pixelsPerUnit);
                var secondFrame = Sprite.Create(
                    texture,
                    new Rect(frameWidth, 0f, frameWidth, texture.height),
                    new Vector2(0.5f, 0.42f),
                    pixelsPerUnit);

                horse.SetRunFrames(renderer, firstFrame, secondFrame);
                renderer.color = Color.white;
                renderer.enabled = false;
            }
            else
            {
                renderer.sprite = BuildBlockSprite();
                renderer.color = horse.Color;
            }

            renderer.sortingOrder = 5;
            return visual;
        }

        private Sprite BuildBlockSprite()
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }

        private void GenerateTickets()
        {
            offeredTickets.Clear();
            EnsureTicketCount();
            relicShopRefreshCost = StartingRelicShopRefreshCost;
            RollRelicShop();
        }

        private void GenerateRaceData()
        {
            var raceInRound = GetRaceInRound();
            var league = raceInRound switch
            {
                1 => RaceLeague.G3,
                2 => RaceLeague.G2,
                _ => RaceLeague.G1
            };

            var candidates = raceDatabase != null && raceDatabase.Races != null
                ? raceDatabase.Races
                    .Where(race => race != null && race.League == league)
                    .ToList()
                : new List<RaceData>();

            if (candidates.Count == 0)
            {
                raceDatabase = RaceDatabase.CreateRuntimeDefaults();
                candidates = raceDatabase.Races
                    .Where(race => race != null && race.League == league)
                    .ToList();
            }

            currentRace = candidates.Count > 0
                ? candidates[rng.Next(candidates.Count)]
                : null;
        }

        private float GetTrackLength()
        {
            return currentRace != null ? currentRace.SimulationLength : 100f;
        }

        private void RollRelicShop()
        {
            relicShopOffers.Clear();
            if (relicDatabase == null || relicDatabase.Relics == null)
            {
                return;
            }

            var available = relicDatabase.Relics
                .Where(relic => relic != null && !relicInventory.Contains(relic))
                .Distinct()
                .ToList();

            while (relicShopOffers.Count < RelicShopOfferCount && available.Count > 0)
            {
                var rarity = RollAvailableRelicRarity(available);
                var candidates = available
                    .Where(relic => relic.Rarity == rarity)
                    .ToList();
                var selected = candidates[rng.Next(candidates.Count)];
                relicShopOffers.Add(selected);
                available.Remove(selected);
            }
        }

        private RelicRarity RollAvailableRelicRarity(IReadOnlyCollection<RelicData> available)
        {
            var rarities = new[]
            {
                RelicRarity.Common,
                RelicRarity.Rare,
                RelicRarity.Epic,
                RelicRarity.Legendary
            };
            var availableRarities = rarities
                .Where(rarity => available.Any(relic => relic.Rarity == rarity))
                .ToList();
            var totalWeight = availableRarities.Sum(GetRelicRarityWeight);
            var roll = rng.NextDouble() * totalWeight;

            foreach (var rarity in availableRarities)
            {
                roll -= GetRelicRarityWeight(rarity);
                if (roll <= 0d)
                {
                    return rarity;
                }
            }

            return availableRarities[availableRarities.Count - 1];
        }

        private static int GetRelicRarityWeight(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => 60,
                RelicRarity.Rare => 25,
                RelicRarity.Epic => 10,
                RelicRarity.Legendary => 5,
                _ => 0
            };
        }

        private void RefreshRelicShop()
        {
            if (gold < relicShopRefreshCost)
            {
                SetLog("relic_refresh_need_gold", relicShopRefreshCost);
                return;
            }

            var paidCost = relicShopRefreshCost;
            gold -= paidCost;
            relicShopRefreshCost = relicShopRefreshCost > int.MaxValue / 2
                ? int.MaxValue
                : relicShopRefreshCost * 2;
            RollRelicShop();
            SetLog("relic_refreshed", paidCost, relicShopRefreshCost);
        }

        private void EnsureTicketCount()
        {
            var targetCount = BaseTicketCount + (relicInventory.Contains(RelicEffectType.ExtraTicket) ? 1 : 0);

            // 같은 종류와 대상 조합의 티켓이 반복되지 않도록 제한 횟수만큼 다시 뽑는다.
            var attempts = 0;
            while (offeredTickets.Count < targetCount && attempts < 80)
            {
                attempts++;
                var ticket = CreateRandomTicket();
                if (offeredTickets.All(existing => existing.Signature != ticket.Signature))
                {
                    offeredTickets.Add(ticket);
                }
            }

            while (offeredTickets.Count < targetCount)
            {
                offeredTickets.Add(CreateRandomTicket());
            }

            while (offeredTickets.Count > targetCount)
            {
                offeredTickets.RemoveAt(offeredTickets.Count - 1);
            }
        }

        private BetTicket CreateRandomTicket()
        {
            var type = (BetType)rng.Next(0, Enum.GetValues(typeof(BetType)).Length);
            var first = field[rng.Next(field.Count)];
            Horse second = null;

            if (type == BetType.Quinella || type == BetType.Exacta)
            {
                do
                {
                    second = field[rng.Next(field.Count)];
                }
                while (second == first);
            }

            return new BetTicket(type, first, second);
        }

        private void StartRace()
        {
            if (runComplete)
            {
                SetLog("season_complete");
                return;
            }

            raceClock = 0f;
            resultDelay = 0f;
            latestStandings.Clear();
            phase = GamePhase.Racing;
            cameraTargetLane = -1;
            cameraDistance = 0f;

            foreach (var horse in field)
            {
                horse.ResetForRace(UnityEngine.Random.Range(0f, GetManaCost(horse) * 0.38f));
            }
            ApplyPreRaceRelics();

            SetLog("race_started", roundNumber, GetRaceInRound(), offeredTickets.Count);
        }

        private void TickRace(float deltaTime)
        {
            var worldTimeStopped = field.Any(horse => horse.TimeStopTimer > 0f);
            if (!worldTimeStopped)
            {
                raceClock += deltaTime;
            }

            foreach (var horse in field)
            {
                horse.TickTimeStop(deltaTime);
                if (horse.Finished)
                {
                    continue;
                }

                if (horse.TimeStopTimer > 0f)
                {
                    horse.CurrentSpeed = 0f;
                    continue;
                }

                horse.TickSkillEffect(deltaTime);
                horse.SkillCooldown = Mathf.Max(0f, horse.SkillCooldown - deltaTime);
                if (horse.StunTimer > 0f)
                {
                    horse.CurrentSpeed = 0f;
                    continue;
                }

                var manaCost = GetManaCost(horse);
                horse.Mana = Mathf.Min(manaCost, horse.Mana + horse.Magic * deltaTime);
                if (horse.Mana >= manaCost)
                {
                    horse.Mana = CastSkill(horse);
                }

                var staminaPressure = Mathf.Max(0f, horse.Fatigue - horse.Stamina * 1.4f);
                // 피로가 지구력의 완충 범위를 넘은 뒤부터 목표 속도를 깎는다.
                var targetSpeed = horse.Speed
                    + horse.RelicSpeedBonus
                    + horse.TemporarySpeed
                    + horse.TimedSpeedBonus
                    - staminaPressure * 0.08f;
                targetSpeed += UnityEngine.Random.Range(-0.18f, 0.18f);

                var acceleration = (horse.Acceleration
                    + horse.TemporaryAcceleration
                    + horse.TimedAccelerationBonus) * 0.7f;
                horse.CurrentSpeed = Mathf.MoveTowards(horse.CurrentSpeed, targetSpeed, acceleration * deltaTime);
                horse.Distance += Mathf.Max(1.5f, horse.CurrentSpeed) * deltaTime;
                horse.Fatigue += deltaTime * Mathf.Lerp(0.85f, 1.45f, horse.Distance / GetTrackLength());

                horse.TemporarySpeed = Mathf.MoveTowards(horse.TemporarySpeed, 0f, deltaTime * 2.4f);
                horse.TemporaryAcceleration = Mathf.MoveTowards(horse.TemporaryAcceleration, 0f, deltaTime * 2.1f);
                horse.AnimateRun(deltaTime);

                if (horse.Distance >= GetTrackLength())
                {
                    horse.Distance = GetTrackLength();
                    horse.Finished = true;
                    horse.FinishTime = raceClock;
                }
            }

            PositionHorseVisuals();

            if (field.All(horse => horse.Finished))
            {
                FinishRace();
            }
        }

        private float CastSkill(Horse horse)
        {
            if (horse.SkillCooldown > 0f || horse.Skill == null)
            {
                return horse.Mana;
            }

            if (!horse.Skill.CanActivate(horse, field))
            {
                return horse.Mana;
            }

            return horse.Skill.Activate(horse, GetTrackLength(), field);
        }

        private void FinishRace()
        {
            phase = GamePhase.Results;
            resultDelay = 0.7f;
            latestStandings = field
                .OrderBy(horse => horse.FinishTime)
                .ThenByDescending(horse => horse.Distance)
                .ToList();

            var hitCount = offeredTickets.Count(ticket => ticket.Evaluate(latestStandings));
            var payout = offeredTickets
                .Where(ticket => ticket.Evaluate(latestStandings))
                .Sum(GetRelicAdjustedPayout);
            if (hitCount > 0 && relicInventory.Contains(RelicEffectType.ProphetReward))
            {
                // 예언가 유물은 적중 티켓 수가 늘수록 보상을 제곱으로 증폭한다.
                payout *= hitCount * hitCount;
            }
            gold += payout;
            roundEarnedGold += payout;
            hits += hitCount;
            totalTicketsResolved += offeredTickets.Count;
            SetLog("all_ticket_result", hitCount, offeredTickets.Count, payout);

            UpdateOddsAfterRace();
        }

        private void ApplyPreRaceRelics()
        {
            if (!relicInventory.Contains(RelicEffectType.HighestOddsSpeed))
            {
                return;
            }

            var highestOddsHorse = field
                .OrderByDescending(horse => horse.WinOdds)
                .ThenBy(horse => horse.Lane)
                .FirstOrDefault();
            if (highestOddsHorse != null)
            {
                highestOddsHorse.RelicSpeedBonus = 5f;
            }
        }

        private int GetRelicAdjustedPayout(BetTicket ticket)
        {
            var baseGoldBonus = 0;
            if (ticket.Type == BetType.Place && relicInventory.Contains(RelicEffectType.PlaceBaseGoldBonus))
            {
                baseGoldBonus += 30;
            }
            if (ticket.Type == BetType.Win && relicInventory.Contains(RelicEffectType.WinBaseGoldBonus))
            {
                baseGoldBonus += 30;
            }
            if ((ticket.Type == BetType.Exacta || ticket.Type == BetType.Quinella)
                && relicInventory.Contains(RelicEffectType.CombinationBaseGoldBonus))
            {
                baseGoldBonus += 30;
            }

            var payout = ticket.GetExpectedPayout(baseGoldBonus);
            if (ticket.Type == BetType.Win && relicInventory.Contains(RelicEffectType.WinPayoutBonus))
            {
                payout = Mathf.RoundToInt(payout * 1.5f);
            }
            else if (ticket.Type == BetType.Exacta && relicInventory.Contains(RelicEffectType.ExactaPayoutBonus))
            {
                payout = Mathf.RoundToInt(payout * 1.5f);
            }

            return payout;
        }

        private void UpdateOddsAfterRace()
        {
            for (var i = 0; i < latestStandings.Count; i++)
            {
                var horse = latestStandings[i];
                var oldOdds = horse.WinOdds;

                // 상위권 말은 다음 배당을 낮추고, 하위권 말은 위험도에 맞춰 배당을 높인다.
                if (i == 0)
                {
                    horse.WinOdds *= 0.78f;
                }
                else if (i <= 2)
                {
                    horse.WinOdds *= 0.92f;
                }
                else
                {
                    horse.WinOdds *= 1.16f + i * 0.04f;
                }

                horse.WinOdds = Mathf.Clamp(horse.WinOdds, 1.2f, 18f);
                horse.LastOddsDelta = horse.WinOdds - oldOdds;
            }
        }

        private void PrepareNextRace()
        {
            if (raceNumber % RacesPerRound == 0)
            {
                if (roundEarnedGold < roundTargetGold)
                {
                    runComplete = true;
                    phase = GamePhase.Betting;
                    SetLog("round_failed", roundNumber, roundEarnedGold, roundTargetGold);
                    return;
                }

                var clearedRound = roundNumber;
                var clearedGold = roundEarnedGold;
                roundNumber++;
                roundTargetGold = roundTargetGold > int.MaxValue / 2
                    ? int.MaxValue
                    : roundTargetGold * 2;
                roundEarnedGold = 0;
                SetLog("round_cleared", clearedRound, clearedGold, roundTargetGold);
            }

            raceNumber++;
            DriftStatsBetweenRaces();
            GenerateRaceData();
            GenerateTickets();
            phase = GamePhase.Betting;
            if (raceNumber % RacesPerRound != 1)
            {
                SetLog("next_race", GetRaceInRound());
            }
        }

        private int GetRaceInRound()
        {
            return (raceNumber - 1) % RacesPerRound + 1;
        }

        private void DriftStatsBetweenRaces()
        {
            foreach (var horse in field)
            {
                var change = rng.Next(4);
                if (change == 0)
                {
                    horse.Speed = Mathf.Clamp(horse.Speed + rng.Next(-1, 2), 4, 18);
                }
                else if (change == 1)
                {
                    horse.Acceleration = Mathf.Clamp(horse.Acceleration + rng.Next(-1, 2), 3, 18);
                }
                else if (change == 2)
                {
                    horse.Stamina = Mathf.Clamp(horse.Stamina + rng.Next(-1, 2), 4, 18);
                }
                else
                {
                    horse.Magic = Mathf.Clamp(horse.Magic + rng.Next(-1, 2), 5, 24);
                }
            }
        }

        private void NormalizeOpeningOdds()
        {
            foreach (var horse in field)
            {
                // 주요 능력치를 가중 합산한 평점을 배당으로 뒤집어 강한 말일수록 낮은 배당을 갖게 한다.
                var rating = horse.Speed * 0.38f + horse.Acceleration * 0.22f + horse.Stamina * 0.22f + horse.Magic * 0.08f;
                var oddsRange = horse.Data != null ? horse.Data.OpeningOddsRange : new Vector2(1.6f, 9.8f);
                horse.WinOdds = Mathf.Clamp(14f - rating + RollFloat(-0.65f, 0.65f), oddsRange.x, oddsRange.y);
            }
        }

        private void UpdateRaceCamera(float deltaTime)
        {
            var target = GetCameraTarget();
            var targetDistance = target != null ? target.Distance : 0f;
            // 프레임률과 무관한 지수 보간으로 선두 또는 선택한 말을 부드럽게 추적한다.
            cameraDistance = Mathf.Lerp(cameraDistance, targetDistance, 1f - Mathf.Exp(-5f * deltaTime));
        }

        private Horse GetCameraTarget()
        {
            if (cameraTargetLane >= 0 && cameraTargetLane < field.Count)
            {
                return field[cameraTargetLane];
            }

            return field
                .OrderByDescending(horse => horse.Distance)
                .ThenBy(horse => horse.Lane)
                .FirstOrDefault();
        }

        private void CycleCameraTarget(int direction)
        {
            cameraTargetLane += direction;
            if (cameraTargetLane < -1)
            {
                cameraTargetLane = field.Count - 1;
            }
            else if (cameraTargetLane >= field.Count)
            {
                cameraTargetLane = -1;
            }
        }

        private void ToggleLanguage()
        {
            language = language == UiLanguage.Korean ? UiLanguage.English : UiLanguage.Korean;
        }

        private void DrawTrack()
        {
            var trackRect = new Rect(32f, 142f, Screen.width - 64f, 306f);
            var trackColor = currentRace != null && currentRace.Surface == TrackSurface.Dirt
                ? new Color(0.24f, 0.16f, 0.1f, 0.98f)
                : new Color(0.12f, 0.2f, 0.12f, 0.98f);
            DrawRect(trackRect, trackColor);
            GUI.Box(trackRect, GUIContent.none, cardStyle);

            var infoWidth = Mathf.Clamp(trackRect.width * 0.24f, 245f, 360f);
            var raceViewport = new Rect(trackRect.x + infoWidth, trackRect.y + 8f, trackRect.width - infoWidth - 8f, trackRect.height - 16f);
            DrawRect(new Rect(trackRect.x + infoWidth - 2f, trackRect.y + 8f, 2f, trackRect.height - 16f), new Color(0.42f, 0.49f, 0.42f, 0.55f));

            for (var i = 0; i < FieldSize; i++)
            {
                var horse = field[i];
                var y = trackRect.y + 18f + i * 46f;
                var isTracked = GetCameraTarget() == horse;
                if (isTracked)
                {
                    DrawRect(new Rect(trackRect.x + 8f, y - 5f, infoWidth - 18f, 42f), new Color(0.2f, 0.29f, 0.23f, 0.95f));
                }

                DrawRect(new Rect(raceViewport.x, y + 31f, raceViewport.width, 2f), new Color(0.48f, 0.56f, 0.48f, 0.38f));
                GUI.Label(new Rect(trackRect.x + 16f, y, infoWidth - 105f, 20f), GetHorseName(horse), bodyStyle);
                GUI.Label(new Rect(trackRect.x + infoWidth - 92f, y, 70f, 20f), $"{horse.WinOdds:0.0}x", smallStyle);
                var manaCost = GetManaCost(horse);
                DrawManaBar(
                    new Rect(trackRect.x + 16f, y + 25f, infoWidth - 122f, 7f),
                    horse.Mana,
                    manaCost);
                GUI.Label(
                    new Rect(trackRect.x + infoWidth - 101f, y + 17f, 80f, 20f),
                    $"{horse.Mana:0}/{manaCost:0}",
                    centeredStyle);
            }

            DrawHorseCharacters(raceViewport);
            PositionHorseVisuals();
        }

        private void DrawHorseCharacters(Rect viewport)
        {
            GUI.BeginGroup(viewport);
            var pixelsPerDistance = viewport.width / CameraViewDistance;
            var centerX = viewport.width * 0.48f;
            var finishX = centerX + (GetTrackLength() - cameraDistance) * pixelsPerDistance;
            if (finishX > -10f && finishX < viewport.width + 10f)
            {
                DrawRect(new Rect(finishX, 0f, 4f, viewport.height), new Color(1f, 0.87f, 0.38f, 0.95f));
                GUI.Label(new Rect(finishX - 34f, 4f, 70f, 22f), L("finish"), centeredStyle);
            }

            foreach (var horse in field)
            {
                if (horse.RunSheet == null)
                {
                    continue;
                }

                var x = centerX + (horse.Distance - cameraDistance) * pixelsPerDistance;
                var y = -5f + horse.Lane * 46f;
                var frameRect = new Rect(horse.AnimationFrame * 0.5f, 0f, 0.5f, 1f);
                var baseRect = new Rect(x - 36f, y, 72f, 92f);
                var effect = horse.SkillEffectAmount;
                if (horse.TimeStopTimer > 0f)
                {
                    var stopPulse = 0.22f + Mathf.PingPong(Time.time * 2f, 0.18f);
                    DrawRect(
                        ScaleRect(baseRect, 1.16f),
                        new Color(0.42f, 0.28f, 1f, stopPulse));
                    GUI.Label(
                        new Rect(baseRect.x - 28f, baseRect.y - 15f, baseRect.width + 56f, 20f),
                        $"{L("time_stopped")} {horse.TimeStopTimer:0.0}s",
                        centeredStyle);
                }
                if (horse.StunTimer > 0f)
                {
                    var stunPulse = 0.35f + Mathf.PingPong(Time.time * 5f, 0.45f);
                    DrawRect(
                        ScaleRect(baseRect, 1.12f),
                        new Color(1f, 0.78f, 0.12f, stunPulse));
                    GUI.Label(
                        new Rect(baseRect.x - 20f, baseRect.y - 15f, baseRect.width + 40f, 20f),
                        $"{L("stunned")} {horse.StunTimer:0.0}s",
                        centeredStyle);
                }
                if (effect > 0f)
                {
                    var effectColor = GetSkillEffectColor(horse.Skill);
                    var pulse = 1f + Mathf.Sin(Time.time * 22f) * 0.06f * effect;
                    var glowRect = ScaleRect(baseRect, 1.18f + effect * 0.12f);
                    DrawRect(glowRect, new Color(effectColor.r, effectColor.g, effectColor.b, 0.16f * effect));

                    for (var trail = 1; trail <= 2; trail++)
                    {
                        var trailRect = baseRect;
                        trailRect.x -= trail * (10f + horse.CurrentSpeed * 0.5f);
                        DrawTintedSprite(
                            trailRect,
                            horse.RunSheet,
                            frameRect,
                            new Color(effectColor.r, effectColor.g, effectColor.b, effect * (0.22f / trail)));
                    }

                    baseRect = ScaleRect(baseRect, pulse);
                    GUI.Label(
                        new Rect(baseRect.x - 30f, baseRect.y - 15f, baseRect.width + 60f, 22f),
                        GetSkillName(horse.Skill),
                        centeredStyle);
                }

                GUI.DrawTextureWithTexCoords(baseRect, horse.RunSheet, frameRect, true);
                if (horse.TimeStopTimer > 0f)
                {
                    DrawTintedSprite(
                        baseRect,
                        horse.RunSheet,
                        frameRect,
                        new Color(0.48f, 0.34f, 1f, 0.38f));
                }
            }

            GUI.EndGroup();
        }

        private void PositionHorseVisuals()
        {
            if (mainCamera == null)
            {
                return;
            }

            foreach (var horse in field)
            {
                if (horse.Visual == null)
                {
                    continue;
                }

                var x = Mathf.Lerp(-7.0f, 6.8f, horse.Distance / GetTrackLength());
                var y = 3.05f - horse.Lane * 0.72f;
                horse.Visual.transform.position = new Vector3(x, y, 0f);
            }
        }

        private void DrawTopPanel()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, 132f), new Color(0.07f, 0.08f, 0.085f, 0.99f));
            GUI.Label(new Rect(22f, 10f, 470f, 34f), L("game_title"), titleStyle);
            GUI.Label(new Rect(22f, 48f, 230f, 28f), $"{L("gold")}  {gold:N0}", navStyle);
            GUI.Label(
                new Rect(264f, 48f, 520f, 26f),
                $"{L("round")} {roundNumber}  |  {L("race")} {GetRaceInRound()}/{RacesPerRound}  |  {L("round_goal")} {roundEarnedGold:N0}/{roundTargetGold:N0}",
                bodyStyle);
            if (currentRace != null)
            {
                GUI.Label(
                    new Rect(800f, 46f, Screen.width - 990f, 42f),
                    $"{currentRace.GetName(language == UiLanguage.Korean)}  |  {currentRace.League}\n{currentRace.GetSurfaceName(language == UiLanguage.Korean)}  |  {currentRace.TotalDistanceMeters}m",
                    bodyStyle);
            }

            if (GUI.Button(new Rect(Screen.width - 170f, 12f, 142f, 32f), language == UiLanguage.Korean ? "한국어 | EN" : "English | KR", buttonStyle))
            {
                ToggleLanguage();
            }

            GUI.Label(new Rect(22f, 90f, 124f, 24f), L("camera"), smallStyle);
            var autoSelected = cameraTargetLane == -1;
            if (GUI.Button(new Rect(116f, 84f, 92f, 32f), autoSelected ? $"[*] {L("leader")}" : L("leader"), buttonStyle))
            {
                cameraTargetLane = -1;
            }

            var availableWidth = Screen.width - 236f;
            var buttonWidth = Mathf.Clamp(availableWidth / FieldSize - 8f, 92f, 170f);
            for (var i = 0; i < field.Count; i++)
            {
                var horse = field[i];
                var x = 220f + i * (buttonWidth + 6f);
                if (x + buttonWidth > Screen.width - 20f)
                {
                    break;
                }

                var selected = cameraTargetLane == i;
                if (GUI.Button(new Rect(x, 82f, buttonWidth, 36f), selected ? $"[*] {GetShortHorseName(horse)}" : GetShortHorseName(horse), buttonStyle))
                {
                    cameraTargetLane = i;
                }
            }

            DrawProgressNavigation();
        }

        private void DrawProgressNavigation()
        {
            if (field.Count == 0)
            {
                return;
            }

            const float iconWidth = 26f;
            const float iconHeight = 34f;
            const float minimumIconSpacing = 24f;
            var startX = 500f;
            var endX = Screen.width - 205f;
            var width = Mathf.Max(180f, endX - startX);
            var lineY = 58f;
            var leaderProgress = field.Max(horse => Mathf.Clamp01(horse.Distance / GetTrackLength()));

            GUI.Label(new Rect(startX - 42f, lineY - 8f, 38f, 20f), "0%", smallStyle);
            GUI.Label(new Rect(startX + width + 5f, lineY - 8f, 46f, 20f), "100%", smallStyle);
            DrawRect(new Rect(startX, lineY, width, 6f), new Color(0.15f, 0.17f, 0.17f, 1f));
            DrawRect(new Rect(startX, lineY, width * leaderProgress, 6f), new Color(0.78f, 0.61f, 0.22f, 0.9f));
            DrawRect(new Rect(startX, lineY - 4f, 2f, 14f), new Color(0.82f, 0.86f, 0.82f, 0.8f));
            DrawRect(new Rect(startX + width - 2f, lineY - 5f, 3f, 16f), new Color(1f, 0.86f, 0.36f, 1f));

            var leader = field
                .OrderByDescending(horse => horse.Distance)
                .ThenBy(horse => horse.Lane)
                .First();
            var tracked = GetCameraTarget();

            var orderedHorses = field
                .OrderBy(horse => horse.Distance)
                .ThenBy(horse => horse.Lane)
                .ToList();
            var iconCenters = orderedHorses
                .Select(horse => startX + width * Mathf.Clamp01(horse.Distance / GetTrackLength()))
                .ToArray();

            for (var i = 1; i < iconCenters.Length; i++)
            {
                iconCenters[i] = Mathf.Max(iconCenters[i], iconCenters[i - 1] + minimumIconSpacing);
            }

            var overflow = iconCenters[iconCenters.Length - 1] - (startX + width);
            if (overflow > 0f)
            {
                for (var i = 0; i < iconCenters.Length; i++)
                {
                    iconCenters[i] -= overflow;
                }
            }

            if (iconCenters[0] < startX)
            {
                var correction = startX - iconCenters[0];
                for (var i = 0; i < iconCenters.Length; i++)
                {
                    iconCenters[i] += correction;
                }
            }

            for (var i = 0; i < orderedHorses.Count; i++)
            {
                var horse = orderedHorses[i];
                var progress = Mathf.Clamp01(horse.Distance / GetTrackLength());
                var actualX = startX + width * progress;
                var iconX = iconCenters[i];
                var connectorStart = Mathf.Min(actualX, iconX);
                var connectorWidth = Mathf.Abs(actualX - iconX);
                if (connectorWidth > 1f)
                {
                    DrawRect(new Rect(connectorStart, lineY - 1f, connectorWidth, 2f), new Color(horse.Color.r, horse.Color.g, horse.Color.b, 0.55f));
                }

                var iconRect = new Rect(iconX - iconWidth * 0.5f, lineY - 32f, iconWidth, iconHeight);
                var effect = horse.SkillEffectAmount;
                if (effect > 0f)
                {
                    var effectColor = GetSkillEffectColor(horse.Skill);
                    var pulse = 1.1f + Mathf.Sin(Time.time * 24f) * 0.1f;
                    var auraRect = ScaleRect(iconRect, 1.45f);
                    DrawRect(auraRect, new Color(effectColor.r, effectColor.g, effectColor.b, 0.45f * effect));
                    iconRect = ScaleRect(iconRect, pulse);
                }

                if (horse.StunTimer > 0f)
                {
                    DrawRect(
                        new Rect(iconRect.x - 3f, iconRect.y - 3f, iconRect.width + 6f, iconRect.height + 6f),
                        new Color(1f, 0.78f, 0.12f, 0.95f));
                }
                if (horse.TimeStopTimer > 0f)
                {
                    DrawRect(
                        new Rect(iconRect.x - 4f, iconRect.y - 4f, iconRect.width + 8f, iconRect.height + 8f),
                        new Color(0.48f, 0.34f, 1f, 0.95f));
                }

                if (horse == tracked)
                {
                    DrawRect(new Rect(iconRect.x - 3f, iconRect.y - 3f, iconRect.width + 6f, iconRect.height + 6f), new Color(1f, 0.84f, 0.28f, 1f));
                }
                else if (horse == leader)
                {
                    DrawRect(new Rect(iconRect.x - 2f, iconRect.y - 2f, iconRect.width + 4f, iconRect.height + 4f), new Color(0.92f, 0.94f, 0.9f, 0.9f));
                }

                DrawRect(iconRect, new Color(horse.Color.r * 0.32f, horse.Color.g * 0.32f, horse.Color.b * 0.32f, 0.96f));
                if (horse.RunSheet != null)
                {
                    GUI.DrawTextureWithTexCoords(iconRect, horse.RunSheet, new Rect(0f, 0f, 0.5f, 1f), true);
                }

                if (effect > 0f)
                {
                    var effectColor = GetSkillEffectColor(horse.Skill);
                    DrawRect(
                        new Rect(iconRect.x, iconRect.y + iconRect.height - 3f, iconRect.width * effect, 3f),
                        new Color(effectColor.r, effectColor.g, effectColor.b, 1f));
                }

                GUI.Label(
                    new Rect(iconRect.x - 9f, lineY + 7f, iconRect.width + 18f, 18f),
                    $"{progress:P0}",
                    centeredStyle);
            }
        }

        private void DrawBettingPanel()
        {
            var panel = GetLowerPanel();
            DrawRect(panel, new Color(0.09f, 0.105f, 0.11f, 0.98f));

            GUI.Label(new Rect(panel.x + 24f, panel.y + 14f, 500f, 28f), runComplete ? L("run_failed_title") : L("ticket_board"), titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 48f, panel.width - 48f, 26f), GetLog(), bodyStyle);

            var tableY = panel.y + 82f;
            for (var i = 0; i < field.Count; i++)
            {
                var horse = field[i];
                var x = panel.x + 24f + i % 3 * 245f;
                var y = tableY + i / 3 * 74f;
                GUI.Label(new Rect(x, y, 235f, 20f), $"{GetHorseName(horse)}  {horse.WinOdds:0.0}x", bodyStyle);
                GUI.Label(new Rect(x, y + 22f, 235f, 38f), StatLine(horse), smallStyle);
            }

            if (!runComplete)
            {
                DrawTickets(panel);
                DrawRelicShop(panel);
            }

            GUI.enabled = !runComplete;
            if (GUI.Button(new Rect(panel.x + panel.width - 220f, panel.y + 30f, 180f, 42f), L("start_race"), buttonStyle))
            {
                StartRace();
            }

            GUI.enabled = true;
            if (GUI.Button(new Rect(panel.x + panel.width - 220f, panel.y + 84f, 180f, 34f), L("new_run"), buttonStyle))
            {
                StartNewRun();
            }
        }

        private void DrawTickets(Rect panel)
        {
            var ticketY = panel.y + 228f;
            var gap = 12f;
            var cardWidth = Mathf.Min(285f, (panel.width - 48f - gap * (offeredTickets.Count - 1)) / offeredTickets.Count);
            for (var i = 0; i < offeredTickets.Count; i++)
            {
                var ticket = offeredTickets[i];
                var rect = new Rect(panel.x + 24f + i * (cardWidth + gap), ticketY, cardWidth, 122f);
                DrawRect(rect, new Color(0.19f, 0.22f, 0.19f, 1f));
                GUI.Box(rect, GUIContent.none, cardStyle);

                if (GUI.Button(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 30f), $"{i + 1}. {ticket.GetTypeName(language == UiLanguage.Korean)}", buttonStyle))
                {
                    CycleTicketType(ticket);
                }

                var targetWidth = ticket.NeedsSecondHorse ? (rect.width - 32f) * 0.5f : rect.width - 24f;
                if (GUI.Button(new Rect(rect.x + 12f, rect.y + 46f, targetWidth, 30f), GetShortHorseName(ticket.First), buttonStyle))
                {
                    CycleTicketHorse(ticket, false);
                }

                if (ticket.NeedsSecondHorse && GUI.Button(new Rect(rect.x + 20f + targetWidth, rect.y + 46f, targetWidth, 30f), GetShortHorseName(ticket.Second), buttonStyle))
                {
                    CycleTicketHorse(ticket, true);
                }

                GUI.Label(new Rect(rect.x + 12f, rect.y + 80f, rect.width - 24f, 22f), $"{ticket.Odds:0.0}x  |  {L("payout")} {GetRelicAdjustedPayout(ticket)}", centeredStyle);
            }

            GUI.Label(
                new Rect(panel.x + panel.width - 220f, panel.y + 132f, 180f, 44f),
                $"{L("all_tickets")}  {offeredTickets.Count}\n{L("race_entry_free")}",
                centeredStyle);
        }

        private void DrawRelicShop(Rect panel)
        {
            var shopY = panel.y + 364f;
            GUI.Label(
                new Rect(panel.x + 24f, shopY, panel.width - 230f, 24f),
                L("relic_shop"),
                bodyStyle);
            if (GUI.Button(
                    new Rect(panel.x + panel.width - 196f, shopY - 3f, 172f, 27f),
                    $"{L("refresh_shop")} {relicShopRefreshCost}",
                    buttonStyle))
            {
                RefreshRelicShop();
            }

            var gap = 10f;
            var cardWidth = (panel.width - 48f - gap * (RelicShopOfferCount - 1)) / RelicShopOfferCount;
            var cardY = shopY + 28f;
            for (var i = 0; i < relicShopOffers.Count; i++)
            {
                var relic = relicShopOffers[i];
                var owned = relicInventory.Contains(relic);
                var rect = new Rect(
                    panel.x + 24f + i * (cardWidth + gap),
                    cardY,
                    cardWidth,
                    86f);
                var tint = relic.Color;
                DrawRect(rect, new Color(tint.r * 0.25f, tint.g * 0.25f, tint.b * 0.25f, 1f));
                GUI.Box(rect, GUIContent.none, cardStyle);

                GUI.Label(
                    new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 20f),
                    $"{relic.GetName(language == UiLanguage.Korean)}  [{GetRarityName(relic.Rarity)}]",
                    centeredStyle);
                GUI.Label(
                    new Rect(rect.x + 8f, rect.y + 27f, rect.width - 16f, 30f),
                    relic.GetDescription(language == UiLanguage.Korean),
                    smallStyle);

                var buttonLabel = owned ? L("owned") : $"{L("buy")} {relic.Price}";
                GUI.enabled = !owned;
                if (GUI.Button(new Rect(rect.x + 8f, rect.y + 58f, rect.width - 16f, 23f), buttonLabel, buttonStyle))
                {
                    BuyRelic(relic);
                }
                GUI.enabled = true;
            }

            DrawRelicInventory(panel, shopY + 120f);
        }

        private void DrawRelicInventory(Rect panel, float inventoryY)
        {
            GUI.Label(
                new Rect(panel.x + 24f, inventoryY, panel.width - 48f, 22f),
                $"{L("relic_inventory")} {relicInventory.Count}/{RelicInventory.MaximumCapacity}",
                bodyStyle);

            const int columns = RelicInventory.MaximumCapacity;
            var gap = 10f;
            var slotWidth = (panel.width - 48f - gap * (columns - 1)) / columns;
            for (var i = 0; i < columns; i++)
            {
                var rect = new Rect(
                    panel.x + 24f + i * (slotWidth + gap),
                    inventoryY + 24f,
                    slotWidth,
                    34f);
                if (i >= relicInventory.Count)
                {
                    GUI.Box(rect, L("empty_relic_slot"), cardStyle);
                    continue;
                }

                var relic = relicInventory.Relics[i];
                var label = $"{relic.GetName(language == UiLanguage.Korean)}  |  {L("sell")} +{relic.SellPrice}";
                if (GUI.Button(rect, label, buttonStyle))
                {
                    SellRelic(relic);
                }
            }
        }

        private void BuyRelic(RelicData relic)
        {
            if (relic == null || relicInventory.Contains(relic))
            {
                return;
            }

            if (relicInventory.IsFull)
            {
                SetLog("relic_full");
                return;
            }

            if (gold < relic.Price)
            {
                SetLog("relic_need_gold", relic.Price);
                return;
            }

            gold -= relic.Price;
            relicInventory.Add(relic);
            EnsureTicketCount();
            SetLog("relic_bought", relic.GetName(language == UiLanguage.Korean));
        }

        private void SellRelic(RelicData relic)
        {
            if (!relicInventory.Remove(relic))
            {
                return;
            }

            gold += relic.SellPrice;
            EnsureTicketCount();
            SetLog("relic_sold", relic.GetName(language == UiLanguage.Korean), relic.SellPrice);
        }

        private string GetRarityName(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => L("rarity_common"),
                RelicRarity.Rare => L("rarity_rare"),
                RelicRarity.Epic => L("rarity_epic"),
                RelicRarity.Legendary => L("rarity_legendary"),
                _ => rarity.ToString()
            };
        }

        private void CycleTicketType(BetTicket ticket)
        {
            var values = (BetType[])Enum.GetValues(typeof(BetType));
            var index = (Array.IndexOf(values, ticket.Type) + 1) % values.Length;
            ticket.SetType(values[index]);
            if (ticket.NeedsSecondHorse && ticket.Second == null)
            {
                ticket.SetSecond(field.First(horse => horse != ticket.First));
            }
            SetLog("customize_all");
        }

        private void CycleTicketHorse(BetTicket ticket, bool secondTarget)
        {
            var current = secondTarget ? ticket.Second : ticket.First;
            var index = current != null ? field.IndexOf(current) : -1;
            for (var attempts = 0; attempts < field.Count; attempts++)
            {
                index = (index + 1) % field.Count;
                var candidate = field[index];
                if (secondTarget && candidate == ticket.First)
                {
                    continue;
                }

                if (!secondTarget && ticket.NeedsSecondHorse && candidate == ticket.Second)
                {
                    continue;
                }

                if (secondTarget)
                {
                    ticket.SetSecond(candidate);
                }
                else
                {
                    ticket.SetFirst(candidate);
                }

                break;
            }
            SetLog("customize_all");
        }

        private void DrawRacePanel()
        {
            var panel = new Rect(32f, 464f, Screen.width - 64f, 132f);
            DrawRect(panel, new Color(0.09f, 0.105f, 0.11f, 0.98f));
            var target = GetCameraTarget();
            GUI.Label(new Rect(panel.x + 24f, panel.y + 16f, 420f, 28f), $"{L("race_clock")}: {raceClock:0.0}s", titleStyle);
            GUI.Label(new Rect(panel.x + 460f, panel.y + 18f, panel.width - 500f, 26f), $"{L("following")}: {(cameraTargetLane == -1 ? L("leader") : GetHorseName(target))}  (Q / E)", bodyStyle);
            var ticketSummary = string.Join("   |   ", offeredTickets.Select((ticket, index) =>
                $"{index + 1}. {ticket.GetLabel(language == UiLanguage.Korean, GetHorseName)}"));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 50f, panel.width - 48f, 42f), ticketSummary, smallStyle);

            var messages = field
                .Where(horse => !string.IsNullOrEmpty(horse.SkillMessage))
                .Select(horse => $"{GetHorseName(horse)}: {GetSkillName(horse.Skill)}");
            GUI.Label(new Rect(panel.x + 24f, panel.y + 82f, panel.width - 48f, 24f), string.Join("   ", messages), smallStyle);
        }

        private void DrawResultsPanel()
        {
            var panel = GetLowerPanel();
            DrawRect(panel, new Color(0.09f, 0.105f, 0.11f, 0.98f));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 18f, 280f, 28f), L("results"), titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 54f, panel.width - 48f, 28f), GetLog(), bodyStyle);

            for (var i = 0; i < latestStandings.Count; i++)
            {
                var horse = latestStandings[i];
                var delta = horse.LastOddsDelta >= 0f ? $"+{horse.LastOddsDelta:0.0}" : $"{horse.LastOddsDelta:0.0}";
                GUI.Label(new Rect(panel.x + 24f, panel.y + 92f + i * 25f, 720f, 24f), $"{i + 1}. {GetHorseName(horse)} - {horse.FinishTime:0.00}s   {L("odds")} {horse.WinOdds:0.0}x ({delta})", bodyStyle);
            }

            var resultButtonLabel = raceNumber % RacesPerRound == 0
                ? L("check_round_button")
                : L("next_race_button");
            if (resultDelay <= 0f && GUI.Button(new Rect(panel.x + panel.width - 230f, panel.y + 30f, 190f, 42f), resultButtonLabel, buttonStyle))
            {
                PrepareNextRace();
            }
        }

        private string StatLine(Horse horse)
        {
            return $"{L("speed_short")} {horse.Speed}  {L("accel_short")} {horse.Acceleration}  {L("stamina_short")} {horse.Stamina}  {L("magic_short")} {horse.Magic}  {GetSkillName(horse.Skill)}";
        }

        private Rect GetLowerPanel()
        {
            return new Rect(32f, 464f, Screen.width - 64f, Mathf.Max(180f, Screen.height - 486f));
        }

        private string GetHorseName(Horse horse)
        {
            return horse?.Data != null
                ? horse.Data.GetName(language == UiLanguage.Korean)
                : horse?.Name ?? string.Empty;
        }

        private string GetShortHorseName(Horse horse)
        {
            if (horse == null)
            {
                return string.Empty;
            }

            return horse.Data != null
                ? horse.Data.GetShortName(language == UiLanguage.Korean)
                : horse.Name.Split(' ')[0];
        }

        private string GetSkillName(HorseSkillData skill)
        {
            return skill != null ? skill.GetName(language == UiLanguage.Korean) : L("skill");
        }

        private void SetLog(string key, params object[] args)
        {
            logKey = key;
            logArgs = args ?? Array.Empty<object>();
        }

        private string GetLog()
        {
            return string.Format(L(logKey), logArgs);
        }

        private string L(string key)
        {
            var korean = language == UiLanguage.Korean;
            switch (key)
            {
                case "game_title": return korean ? "말라트로: 승부 예측 경마" : "Malatro: Prediction Horse Racing";
                case "gold": return korean ? "골드" : "Gold";
                case "race": return korean ? "레이스" : "Race";
                case "round": return korean ? "라운드" : "Round";
                case "round_goal": return korean ? "목표" : "Goal";
                case "camera": return korean ? "카메라" : "Camera";
                case "leader": return korean ? "선두 자동" : "Auto Leader";
                case "finish": return korean ? "결승" : "FINISH";
                case "ticket_board": return korean ? "마권 선택" : "Ticket Board";
                case "meet_complete_title": return korean ? "시즌 종료" : "Meet Complete";
                case "start_race": return korean ? "레이스 시작 (Space)" : "Start Race (Space)";
                case "new_run": return korean ? "새 게임" : "New Run";
                case "stake": return korean ? "베팅" : "Stake";
                case "payout": return korean ? "예상 지급" : "Payout";
                case "all_tickets": return korean ? "전체 마권 적용" : "All Tickets Active";
                case "total_cost": return korean ? "총 비용" : "Total Cost";
                case "selected": return korean ? "선택됨" : "Selected";
                case "select": return korean ? "선택" : "Select";
                case "race_clock": return korean ? "경기 시간" : "Race clock";
                case "following": return korean ? "추적 중" : "Following";
                case "results": return korean ? "경기 결과" : "Results";
                case "odds": return korean ? "배당" : "odds";
                case "next_race_button": return korean ? "다음 레이스 (Space)" : "Next Race (Space)";
                case "check_round_button": return korean ? "라운드 결과 확인 (Space)" : "Check Round (Space)";
                case "speed_short": return korean ? "속도" : "SPD";
                case "accel_short": return korean ? "가속" : "ACC";
                case "stamina_short": return korean ? "지구력" : "STA";
                case "magic_short": return korean ? "마력" : "MAG";
                case "skill": return korean ? "스킬" : "Skill";
                case "stunned": return korean ? "기절" : "STUN";
                case "time_stopped": return korean ? "시간 정지" : "TIME STOP";
                case "choose_ticket": return korean ? "세 장의 마권 중 하나를 선택하세요." : "Choose one of three tickets, then read the race.";
                case "pick_ticket": return korean ? "세 장의 마권을 커스텀하고 Space로 출발하세요. 세 장 모두 적용됩니다." : "Customize all three tickets. Every ticket is active when the race starts.";
                case "customize_all": return korean ? "마권 종류와 대상 말을 조정하세요. 세 장 모두 자동 적용됩니다." : "Customize the type and horses. All three tickets are automatically active.";
                case "ticket_selected": return korean ? "{0} 선택. Space로 레이스를 시작하세요." : "Selected {0}. Press Space to race.";
                case "season_complete": return korean ? "시즌이 끝났습니다. 새 게임을 시작하세요." : "Season complete. Start a new run.";
                case "pick_first": return korean ? "먼저 1, 2, 3번 마권 중 하나를 선택하세요." : "Pick a ticket first: 1, 2, or 3.";
                case "need_gold_all": return korean ? "세 장의 마권을 적용하려면 골드 {0}이 필요합니다." : "You need {0} gold to activate all three tickets.";
                case "race_started": return korean ? "{0}라운드 {1}경기 시작. 마권 {2}장이 모두 적용됩니다." : "Round {0}, race {1}: all {2} tickets are active.";
                case "all_ticket_result": return korean ? "마권 {0}/{1}장 적중, 총 골드 {2} 획득." : "{0}/{1} tickets hit. Total payout: {2} gold.";
                case "ticket_hit": return korean ? "적중! {0} / 골드 {1} 획득." : "Hit! {0} paid {1} gold.";
                case "ticket_miss": return korean ? "미적중: {0}" : "Missed {0}. The track keeps the stake.";
                case "meet_complete": return korean ? "시즌 종료. 마권 적중 {0}/{1}, 최종 골드 {2}." : "Meet complete. Ticket hits {0}/{1}, final gold {2}.";
                case "next_race": return korean ? "{0}경주: 이전 결과에 따라 배당이 변동했습니다." : "Race {0}: odds moved after the last result.";
                case "race_entry_free": return korean ? "레이스 참가비 무료" : "Free race entry";
                case "round_cleared": return korean ? "{0}라운드 통과! {1}골드를 획득했습니다. 다음 목표는 {2}골드입니다." : "Round {0} cleared with {1} gold. The next goal is {2} gold.";
                case "round_failed": return korean ? "{0}라운드 실패. 획득 골드 {1}/{2}. 새로운 런을 시작하세요." : "Round {0} failed. Earned {1}/{2} gold. Start a new run.";
                case "run_failed_title": return korean ? "라운드 실패" : "Round Failed";
                case "relic_shop": return korean ? "유물 상점" : "Relic Shop";
                case "relic_inventory": return korean ? "보유" : "Owned";
                case "refresh_shop": return korean ? "새로고침" : "Refresh";
                case "owned": return korean ? "보유 중" : "Owned";
                case "empty_relic_slot": return korean ? "빈 유물 칸" : "Empty relic slot";
                case "buy": return korean ? "구매" : "Buy";
                case "sell": return korean ? "판매" : "Sell";
                case "rarity_common": return korean ? "일반" : "Common";
                case "rarity_rare": return korean ? "희귀" : "Rare";
                case "rarity_epic": return korean ? "영웅" : "Epic";
                case "rarity_legendary": return korean ? "전설" : "Legendary";
                case "relic_bought": return korean ? "{0} 구매 완료." : "Bought {0}.";
                case "relic_sold": return korean ? "{0} 판매. 골드 {1} 획득." : "Sold {0} for {1} gold.";
                case "relic_full": return korean ? "유물은 최대 4개까지 보유할 수 있습니다." : "You can hold up to four relics.";
                case "relic_need_gold": return korean ? "이 유물을 구매하려면 골드 {0}이 필요합니다." : "You need {0} gold to buy this relic.";
                case "relic_refresh_need_gold": return korean ? "상점을 새로고침하려면 골드 {0}이 필요합니다." : "You need {0} gold to refresh the shop.";
                case "relic_refreshed": return korean ? "골드 {0}을 사용해 상점을 갱신했습니다. 다음 비용은 {1}골드입니다." : "Spent {0} gold to refresh the shop. The next refresh costs {1}.";
                default: return key;
            }
        }

        private int Roll(int minInclusive, int maxInclusive)
        {
            return rng.Next(minInclusive, maxInclusive + 1);
        }

        private float RollFloat(float minInclusive, float maxInclusive)
        {
            return (float)(minInclusive + rng.NextDouble() * (maxInclusive - minInclusive));
        }

        private float GetManaCost(Horse horse)
        {
            return horse != null && horse.Skill != null
                ? Mathf.Max(1f, horse.Skill.ManaCost)
                : DefaultManaCost;
        }

        private void DrawManaBar(Rect rect, float amount, float maximum)
        {
            DrawRect(rect, new Color(0.06f, 0.07f, 0.08f, 1f));
            var fill = Mathf.Clamp01(amount / Mathf.Max(1f, maximum));
            DrawRect(new Rect(rect.x, rect.y, rect.width * fill, rect.height), new Color(0.37f, 0.72f, 0.92f, 1f));
        }

        private Color GetSkillEffectColor(HorseSkillData skill)
        {
            return skill != null ? skill.EffectColor : Color.white;
        }

        private Rect ScaleRect(Rect rect, float scale)
        {
            var width = rect.width * scale;
            var height = rect.height * scale;
            return new Rect(
                rect.center.x - width * 0.5f,
                rect.center.y - height * 0.5f,
                width,
                height);
        }

        private void DrawTintedSprite(Rect rect, Texture texture, Rect textureCoordinates, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(rect, texture, textureCoordinates, true);
            GUI.color = previous;
        }

        private void DrawRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.93f, 0.86f) }
            };
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 18);
            titleStyle.font = uiFont;

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.86f, 0.82f) }
            };
            bodyStyle.font = uiFont;

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.72f, 0.77f, 0.72f) }
            };
            smallStyle.font = uiFont;

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = Texture2D.grayTexture },
                padding = new RectOffset(12, 12, 12, 12)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                font = uiFont
            };

            navStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.84f, 0.34f) }
            };

            centeredStyle = new GUIStyle(smallStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.9f, 0.52f) }
            };
        }

        private enum GamePhase
        {
            Betting,
            Racing,
            Results
        }

        private enum UiLanguage
        {
            Korean,
            English
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Malatro
{
    // ??ш끽維곩ㅇ??????怨룰틖 ?濡ろ뜐??????ㅺ컼?? ?濡ろ뜑??댁낄???????源낇꼧?? 癲ル슣鍮뽳쭕??癲ル슢?꾤땟???UI????癰궽블뀤????????굿?域밸Ŧ肉ョ뵳??
    public sealed partial class MalatroPrototype : MonoBehaviour
    {
        private const int DefaultRaceEntrantCount = 6;
        private const int BaseTicketCount = 3;
        private const int RelicShopOfferCount = 3;
        private const double HorseStatOfferChance = 0.3d;
        private const int StartingRelicShopRefreshCost = 30;
        private const int RacesPerRound = 3;
        private const int StartingRoundTarget = 50;
        private const float DefaultManaCost = 100f;
        private const float CameraViewDistance = 34f;
        private const float FinalDuelCameraViewDistance = 24f;

        private readonly List<Horse> roster = new();
        private readonly List<Horse> field = new();
        private readonly List<BetTicket> offeredTickets = new();
        private readonly List<RelicData> relicShopOffers = new();
        private readonly List<HorseStatShopOffer> horseStatShopOffers = new();
        private readonly List<ShopOfferEntry> shopOffers = new();
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
        private RaceWorldView raceWorldView;
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
        private float podiumTransitionTimer;
        private bool podiumTransitionPending;
        private float racePlaybackSpeed = 1f;
        private bool runComplete;
        private UiLanguage language = UiLanguage.Korean;
        private int cameraTargetLane = -1;
        private float cameraDistance;
        private float raceCameraViewDistance = CameraViewDistance;
        private int finishedCameraTargetLane = -1;
        private float finishedCameraTargetDelay;
        private int skillCameraTargetLane = -1;
        private int cameraTargetBeforeSkill = -1;
        private float skillCameraTargetTimer;
        private Horse selectedHorseInfo;
        private RelicData selectedRelicInfo;
        private HorseStatShopOffer pendingHorseStatOffer;
        private BetTicket editingTicket;
        private TicketSelectionMode ticketSelectionMode;
        private string logKey = "choose_ticket";
        private object[] logArgs = Array.Empty<object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // ?????怨뚮옓????????щ빘???됰씭肄???袁⑸즲????? ????깅떋????????癲ル슢?꾤땟?????????ш끽維곩ㅇ??????怨룰도 ??筌믨퀣援??筌먲퐢??
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

            var raceWorldObject = new GameObject("Race World View");
            raceWorldObject.transform.SetParent(transform, false);
            raceWorldView = raceWorldObject.AddComponent<RaceWorldView>();
            raceWorldView.Initialize(mainCamera);

            whiteTexture = Texture2D.whiteTexture;
            horseDatabase = HorseDatabase.LoadOrCreateRuntimeDefaults();
            relicDatabase = RelicDatabase.LoadOrCreateRuntimeDefaults();
            raceDatabase = RaceDatabase.LoadOrCreateRuntimeDefaults();
            StartNewRun();
            BuildCanvasUi();
        }

        private void Update()
        {
            if (phase == GamePhase.Racing)
            {
                if (podiumTransitionPending)
                {
                    podiumTransitionTimer = Mathf.Max(0f, podiumTransitionTimer - Time.deltaTime);
                    if (podiumTransitionTimer <= 0f)
                    {
                        podiumTransitionPending = false;
                        TransitionTo(GamePhase.Results);
                    }
                }
                else
                {
                    var scaledDeltaTime = Time.deltaTime * racePlaybackSpeed;
                    TickRace(scaledDeltaTime);
                    UpdateRaceCamera(scaledDeltaTime, Time.deltaTime);
                }
                UpdateRaceWorldPresentation();
            }
            else if (phase == GamePhase.Results)
            {
                resultDelay = Mathf.Max(0f, resultDelay - Time.deltaTime);
                UpdateRaceWorldPresentation();
            }
            else
            {
                raceWorldView?.SetVisible(false, field);
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                ToggleLanguage();
            }

            if (phase == GamePhase.Racing)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    CycleCameraTarget(-1);
                }
                else if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    CycleCameraTarget(1);
                }

                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                {
                    racePlaybackSpeed = 1f;
                }
                else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                {
                    racePlaybackSpeed = 1.5f;
                }
                else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                {
                    racePlaybackSpeed = 2f;
                }
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
            if (canvasUiReady)
            {
                return;
            }

            EnsureStyles();

            DrawTopPanel();
            if (phase == GamePhase.Racing || phase == GamePhase.Results)
            {
                DrawTrack();
            }

            switch (phase)
            {
                case GamePhase.Betting:
                    DrawBettingPanel();
                    break;
                case GamePhase.Shop:
                    DrawShopPanel();
                    break;
                case GamePhase.Racing:
                    DrawRacePanel();
                    break;
                case GamePhase.Results:
                    DrawResultsPanel();
                    break;
            }

            DrawHorseInfoPopup();
            DrawRelicInfoPopup();
            DrawTicketSelectionPopup();
            DrawHorseStatOfferSelectionPopup();
        }

        private void StartNewRun()
        {
            foreach (var horse in roster)
            {
                if (horse.Visual != null)
                {
                    Destroy(horse.Visual);
                }
            }

            roster.Clear();
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
            podiumTransitionTimer = 0f;
            podiumTransitionPending = false;
            racePlaybackSpeed = 1f;
            runComplete = false;
            TransitionTo(GamePhase.Betting, false);
            cameraTargetLane = -1;
            cameraDistance = 0f;

            var entries = horseDatabase.Horses
                .Where(data => data != null)
                .ToList();
            if (entries.Count < 2)
            {
                horseDatabase = HorseDatabase.CreateRuntimeDefaults();
                entries = horseDatabase.Horses
                    .Where(data => data != null)
                    .ToList();
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
                roster.Add(horse);
            }

            NormalizeOpeningOdds();
            GenerateRaceData();
            SelectRaceEntrants();
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
                var pixelsPerUnit = texture.height / 0.9f;
                var firstFrame = CreateHorseFrameSprite(texture, false, pixelsPerUnit);
                var secondFrame = CreateHorseFrameSprite(texture, true, pixelsPerUnit);

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
            visual.AddComponent<HorseWorldEffects>().Initialize(renderer);
            visual.SetActive(false);
            return visual;
        }

        private static Sprite CreateHorseFrameSprite(Texture2D texture, bool secondFrame, float pixelsPerUnit = 100f)
        {
            if (texture == null)
            {
                return null;
            }

            var isTwoFrameSheet = texture.width >= texture.height * 1.9f;
            var frameWidth = isTwoFrameSheet ? texture.width * 0.5f : texture.width;
            var x = isTwoFrameSheet && secondFrame ? frameWidth : 0f;
            return Sprite.Create(
                texture,
                new Rect(x, 0f, frameWidth, texture.height),
                new Vector2(0.5f, 0.42f),
                Mathf.Max(1f, pixelsPerUnit));
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

            currentRace = candidates.Count > 0
                ? candidates[rng.Next(candidates.Count)]
                : null;
        }

        private void SelectRaceEntrants()
        {
            foreach (var horse in roster)
            {
                if (horse.Renderer != null)
                {
                    horse.Renderer.enabled = false;
                }
            }

            field.Clear();
            var requestedCount = currentRace != null
                ? currentRace.EntrantCount
                : DefaultRaceEntrantCount;
            var entrantCount = Mathf.Clamp(requestedCount, 2, roster.Count);
            var candidates = roster.ToList();

            for (var lane = 0; lane < entrantCount; lane++)
            {
                var selectedIndex = rng.Next(candidates.Count);
                var horse = candidates[selectedIndex];
                candidates.RemoveAt(selectedIndex);
                horse.Lane = lane;
                field.Add(horse);
                if (horse.Renderer != null)
                {
                    horse.Renderer.enabled = horse.RunSheet == null;
                }
            }

            selectedHorseInfo = null;
            selectedRelicInfo = null;
            pendingHorseStatOffer = null;
            editingTicket = null;
            cameraTargetLane = -1;
        }

        private float GetTrackLength()
        {
            return currentRace != null ? currentRace.SimulationLength : 100f;
        }

        private void RollRelicShop()
        {
            relicShopOffers.Clear();
            horseStatShopOffers.Clear();
            shopOffers.Clear();
            if (relicDatabase == null || relicDatabase.Relics == null)
            {
                return;
            }

            var available = relicDatabase.Relics
                .Where(relic => relic != null && !relicInventory.Contains(relic))
                .Distinct()
                .ToList();

            while (shopOffers.Count < RelicShopOfferCount)
            {
                var rarity = RollShopRarity();
                var rollStatOffer = rng.NextDouble() < HorseStatOfferChance;
                if (rollStatOffer || available.Count == 0)
                {
                    AddHorseStatShopOffer(rarity);
                    continue;
                }

                var candidates = available.Where(relic => relic.Rarity == rarity).ToList();
                if (candidates.Count == 0)
                {
                    candidates = available;
                }

                var selected = candidates[rng.Next(candidates.Count)];
                relicShopOffers.Add(selected);
                shopOffers.Add(new ShopOfferEntry { Relic = selected });
                available.Remove(selected);
            }
        }

        private void AddHorseStatShopOffer(RelicRarity rarity)
        {
            if (roster.Count == 0)
            {
                return;
            }

            var requiresSelection = rng.NextDouble() < 0.5d;
            var offer = CreateHorseStatShopOffer(rarity, requiresSelection);
            horseStatShopOffers.Add(offer);
            shopOffers.Add(new ShopOfferEntry { StatOffer = offer });
        }

        private HorseStatShopOffer CreateHorseStatShopOffer(RelicRarity rarity, bool requiresSelection)
        {
            var statCount = Enum.GetValues(typeof(HorseStatType)).Length;
            var amount = GetHorseStatOfferAmount(rarity);
            var signedAmount = rng.NextDouble() < 0.5d ? amount : -amount;
            return new HorseStatShopOffer
            {
                Stat = (HorseStatType)rng.Next(statCount),
                Rarity = rarity,
                Amount = signedAmount,
                Price = amount * 15 + (requiresSelection ? 20 : 0),
                RequiresHorseSelection = requiresSelection,
                RandomTarget = requiresSelection ? null : roster[rng.Next(roster.Count)]
            };
        }

        private RelicRarity RollShopRarity()
        {
            var rarities = new[]
            {
                RelicRarity.Common,
                RelicRarity.Rare,
                RelicRarity.Epic,
                RelicRarity.Legendary
            };
            var totalWeight = rarities.Sum(GetRelicRarityWeight);
            var roll = rng.NextDouble() * totalWeight;
            foreach (var rarity in rarities)
            {
                roll -= GetRelicRarityWeight(rarity);
                if (roll <= 0d)
                {
                    return rarity;
                }
            }

            return RelicRarity.Common;
        }

        private static int GetHorseStatOfferAmount(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => 2,
                RelicRarity.Rare => 3,
                RelicRarity.Epic => 5,
                RelicRarity.Legendary => 10,
                _ => 2
            };
        }

        private void BuyHorseStatOffer(HorseStatShopOffer offer, Horse target)
        {
            if (offer == null || offer.Purchased || target == null)
            {
                return;
            }

            if (gold < offer.Price)
            {
                SetLog("stat_offer_need_gold", offer.Price);
                return;
            }

            gold -= offer.Price;
            ApplyHorseStatChange(target, offer.Stat, offer.Amount);
            offer.Purchased = true;
            SetLog(
                "stat_offer_bought",
                GetHorseName(target),
                GetHorseStatName(offer.Stat),
                GetSignedStatAmount(offer.Amount));
        }

        private static void ApplyHorseStatChange(Horse horse, HorseStatType stat, int amount)
        {
            switch (stat)
            {
                case HorseStatType.Speed:
                    horse.Speed = Mathf.Clamp(horse.Speed + amount, 1, 30);
                    break;
                case HorseStatType.Acceleration:
                    horse.Acceleration = Mathf.Clamp(horse.Acceleration + amount, 1, 30);
                    break;
                case HorseStatType.Stamina:
                    horse.Stamina = Mathf.Clamp(horse.Stamina + amount, 1, 30);
                    break;
                case HorseStatType.Magic:
                    horse.Magic = Mathf.Clamp(horse.Magic + amount, 1, 30);
                    break;
            }
        }

        private string GetHorseStatName(HorseStatType stat)
        {
            return stat switch
            {
                HorseStatType.Speed => L("speed_short"),
                HorseStatType.Acceleration => L("accel_short"),
                HorseStatType.Stamina => L("stamina_short"),
                HorseStatType.Magic => L("magic_short"),
                _ => stat.ToString()
            };
        }

        private static string GetSignedStatAmount(int amount)
        {
            return amount >= 0 ? $"+{amount}" : amount.ToString();
        }

        private static int GetHorseStatValue(Horse horse, HorseStatType stat)
        {
            if (horse == null)
            {
                return 0;
            }

            return stat switch
            {
                HorseStatType.Speed => horse.Speed,
                HorseStatType.Acceleration => horse.Acceleration,
                HorseStatType.Stamina => horse.Stamina,
                HorseStatType.Magic => horse.Magic,
                _ => 0
            };
        }

        private static int GetAdjustedHorseStatValue(Horse horse, HorseStatShopOffer offer)
        {
            return offer == null
                ? 0
                : Mathf.Clamp(GetHorseStatValue(horse, offer.Stat) + offer.Amount, 1, 30);
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

            // ??좊즵?? ???ろ꼤嶺?? ?????釉뚰?????????Β?利???袁⑸즵????? ????녳뵣??????モ뵲 ????낅묄癲ル슢???移????怨뺣빰 癲ル슢?筌???
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
            if (phase != GamePhase.Betting && phase != GamePhase.Shop)
            {
                return;
            }

            if (runComplete)
            {
                SetLog("season_complete");
                return;
            }

            raceClock = 0f;
            resultDelay = 0f;
            latestStandings.Clear();
            TransitionTo(GamePhase.Racing);
            cameraTargetLane = -1;
            cameraDistance = 0f;
            raceCameraViewDistance = CameraViewDistance;
            finishedCameraTargetLane = -1;
            finishedCameraTargetDelay = 0f;
            skillCameraTargetLane = -1;
            cameraTargetBeforeSkill = -1;
            skillCameraTargetTimer = 0f;

            foreach (var horse in field)
            {
                horse.ResetForRace(UnityEngine.Random.Range(0f, GetManaCost(horse) * 0.38f));
            }
            InitializeRaceLanes();
            ApplyPreRaceRelics();

            SetLog("race_started", roundNumber, GetRaceInRound(), offeredTickets.Count);
        }

        private void OpenShop()
        {
            if (runComplete || phase != GamePhase.Betting)
            {
                return;
            }

            TransitionTo(GamePhase.Shop);
            SetLog("shop_before_race");
        }

        private void TickRace(float deltaTime)
        {
            var worldTimeStopped = field.Any(horse => horse.TimeStopTimer > 0f);
            if (!worldTimeStopped)
            {
                raceClock += deltaTime;
            }

            UpdateHorseLaneTargets(deltaTime);

            foreach (var horse in field)
            {
                horse.PreviousDistance = horse.Distance;
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

                var aptitudeMultiplier = GetSurfaceAptitudeMultiplier(horse);
                var manaCost = GetManaCost(horse);
                var magic = Mathf.Max(0f, horse.Magic * aptitudeMultiplier + horse.RelicMagicBonus);
                horse.Mana = Mathf.Min(manaCost, horse.Mana + magic * deltaTime);
                if (horse.Mana >= manaCost
                    && (horse.Skill == null || !horse.Skill.IsReactive))
                {
                    horse.Mana = CastSkill(horse);
                }

                var stamina = Mathf.Max(0f, horse.Stamina * aptitudeMultiplier + horse.RelicStaminaBonus);
                var staminaPressure = Mathf.Max(0f, horse.Fatigue - stamina * 1.4f);
                // ???⑥???좊읈? 癲ル슣??????㎥?????ш끽維뽭뇡??類?????끹걫???? ?????癲ル슢?꾤땟?룹춻?????뽦뵣??嚥싲갭큔????
                var targetSpeed = horse.Speed * aptitudeMultiplier
                    + horse.RelicSpeedBonus
                    + horse.TemporarySpeed
                    + horse.TimedSpeedBonus
                    - staminaPressure * 0.08f;
                targetSpeed *= horse.SpeedMultiplier;
                targetSpeed -= GetCornerLanePenalty(horse);
                targetSpeed = Mathf.Min(targetSpeed, GetTrafficSpeedLimit(horse, targetSpeed));
                targetSpeed += UnityEngine.Random.Range(-0.18f, 0.18f);

                var acceleration = Mathf.Max(0.1f, horse.Acceleration * aptitudeMultiplier
                    + horse.RelicAccelerationBonus
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

            ResolveOvertakeSkills();

            if (field.All(horse => horse.Finished))
            {
                FinishRace();
            }
        }

        private void InitializeRaceLanes()
        {
            RaceMovementSystem.Initialize(field);
        }

        private void UpdateHorseLaneTargets(float deltaTime)
        {
            RaceMovementSystem.Update(field, currentRace, raceClock, deltaTime);
        }

        private float GetCornerLanePenalty(Horse horse)
        {
            return RaceMovementSystem.GetCornerLanePenalty(horse, currentRace);
        }

        private float GetTrafficSpeedLimit(Horse horse, float unrestrictedSpeed)
        {
            return RaceMovementSystem.GetTrafficSpeedLimit(horse, field, unrestrictedSpeed);
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

            var remainingMana = horse.Skill.Activate(horse, GetTrackLength(), field);
            if (horse.Skill.EffectType == HorseSkillEffectType.TimeStop)
            {
                BeginSkillCameraTracking(horse, horse.Skill.TimeStopDuration);
            }

            return remainingMana;
        }

        private void ResolveOvertakeSkills()
        {
            const float crossingEpsilon = 0.001f;

            foreach (var horse in field)
            {
                if (horse.Finished
                    || horse.Skill == null
                    || horse.Skill.EffectType != HorseSkillEffectType.OvertakeTrip
                    || horse.SkillCooldown > 0f
                    || horse.Mana < GetManaCost(horse))
                {
                    continue;
                }

                foreach (var overtaker in field)
                {
                    if (overtaker == horse || overtaker.Finished)
                    {
                        continue;
                    }

                    var wasBehind = overtaker.PreviousDistance < horse.PreviousDistance - crossingEpsilon;
                    var isNowAhead = overtaker.Distance > horse.Distance + crossingEpsilon;
                    if (wasBehind && isNowAhead)
                    {
                        horse.Mana = horse.Skill.ActivateOnOvertaken(horse, overtaker);
                        break;
                    }
                }
            }
        }

        private void FinishRace()
        {
            if (podiumTransitionPending)
            {
                return;
            }

            resultDelay = 0f;
            podiumTransitionTimer = 1f;
            podiumTransitionPending = true;
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
                payout *= 1 << hitCount;
            }
            if (payout > 0 && relicInventory.Contains(RelicEffectType.TicketTypeVarietyReward))
            {
                var ticketTypeCount = offeredTickets
                    .Select(ticket => ticket.Type)
                    .Distinct()
                    .Count();
                payout *= 1 << ticketTypeCount;
            }
            payout = Mathf.RoundToInt(payout * GetLeaguePayoutMultiplier());
            gold += payout;
            roundEarnedGold += payout;
            hits += hitCount;
            totalTicketsResolved += offeredTickets.Count;
            SetLog("all_ticket_result", hitCount, offeredTickets.Count, payout);

            UpdateOddsAfterRace();
        }

        private float GetLeaguePayoutMultiplier()
        {
            if (currentRace == null)
            {
                return 1f;
            }

            return currentRace.League switch
            {
                RaceLeague.G2 => 1.5f,
                RaceLeague.G1 => 2f,
                _ => 1f
            };
        }

        private void ApplyPreRaceRelics()
        {
            if (currentRace == null)
            {
                SetLog("race_data_missing", GetRequiredLeague());
                return;
            }

            if (relicInventory.Contains(RelicEffectType.HighestOddsSpeed))
            {
                var highestOddsHorse = field
                    .OrderByDescending(horse => horse.WinOdds)
                    .ThenBy(horse => horse.Lane)
                    .FirstOrDefault();
                if (highestOddsHorse != null)
                {
                    highestOddsHorse.RelicSpeedBonus += 5f;
                }
            }

            if (relicInventory.Contains(RelicEffectType.MageTagSpeedPenalty))
            {
                foreach (var horse in field.Where(IsMageTaggedHorse))
                {
                    horse.RelicSpeedBonus -= 3f;
                }
            }

            if (relicInventory.Contains(RelicEffectType.AssassinTagAccelerationPenalty))
            {
                foreach (var horse in field.Where(IsAssassinTaggedHorse))
                {
                    horse.RelicAccelerationBonus -= 3f;
                }
            }

            if (relicInventory.Contains(RelicEffectType.KnightTagSpeedPenalty))
            {
                foreach (var horse in field.Where(IsKnightTaggedHorse))
                {
                    horse.RelicSpeedBonus -= 3f;
                }
            }

            if (relicInventory.Contains(RelicEffectType.MageTagMagicBonus))
            {
                foreach (var horse in field.Where(IsMageTaggedHorse))
                {
                    horse.RelicMagicBonus += 2f;
                }
            }

            if (relicInventory.Contains(RelicEffectType.KingdomTagStaminaBonus))
            {
                foreach (var horse in field.Where(IsKingdomTaggedHorse))
                {
                    horse.RelicStaminaBonus += 2f;
                }
            }
        }

        private static bool IsMageTaggedHorse(Horse horse)
        {
            return horse.HasAnyTag(new[] { "\uB9C8\uBC95\uC0AC", "Mage", "Wizard" });
        }

        private static bool IsAssassinTaggedHorse(Horse horse)
        {
            return horse.HasAnyTag(new[] { "\uC554\uC0B4\uC790", "Assassin" });
        }

        private static bool IsKnightTaggedHorse(Horse horse)
        {
            return horse.HasAnyTag(new[] { "\uAE30\uC0AC", "Knight" });
        }

        private static bool IsKingdomTaggedHorse(Horse horse)
        {
            return horse.HasAnyTag(new[] { "\uC655\uAD6D\uC778", "Kingdom", "Royal" });
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

        private float GetSurfaceAptitudeMultiplier(Horse horse)
        {
            if (horse == null || horse.Data == null || currentRace == null)
            {
                return 1f;
            }

            return horse.Data.GetAptitudeMultiplier(currentRace.Surface);
        }

        private void UpdateOddsAfterRace()
        {
            for (var i = 0; i < latestStandings.Count; i++)
            {
                var horse = latestStandings[i];
                var oldOdds = horse.WinOdds;

                // ???ㅼ굣筌띻쐴踰?癲ル슢??씙? ???源낆쓱 ?袁⑸즲?????????㉱? ???쒓낮彛딃썒?癲ル슢??씙? ??ш낄援???ш끽維??癲ル슢?????袁⑸즲?????亦껋꼨援???
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
                    TransitionTo(GamePhase.Betting);
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
            SelectRaceEntrants();
            GenerateTickets();
            TransitionTo(GamePhase.Betting);
            if (raceNumber % RacesPerRound != 1)
            {
                SetLog("next_race", GetRaceInRound());
            }
        }

        private int GetRaceInRound()
        {
            return (raceNumber - 1) % RacesPerRound + 1;
        }

        private RaceLeague GetRequiredLeague()
        {
            return GetRaceInRound() switch
            {
                1 => RaceLeague.G3,
                2 => RaceLeague.G2,
                _ => RaceLeague.G1
            };
        }

        private void DriftStatsBetweenRaces()
        {
            foreach (var horse in roster)
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
            foreach (var horse in roster)
            {
                // ??낆뒩????潁??④낄??? ??좊읈?濚???獄쏅똾?????繹먮냱????袁⑸즲?????⑥?????源녿뾼????좊즴甕겹끃??癲ル슢??씙???嚥?흮 ??? ?袁⑸즲??????좊즴?怨꾨뼀???筌먲퐢??
                var rating = horse.Speed * 0.38f + horse.Acceleration * 0.22f + horse.Stamina * 0.22f + horse.Magic * 0.08f;
                var oddsRange = horse.Data != null ? horse.Data.OpeningOddsRange : new Vector2(1.6f, 9.8f);
                horse.WinOdds = Mathf.Clamp(14f - rating + RollFloat(-0.65f, 0.65f), oddsRange.x, oddsRange.y);
            }
        }

        private void UpdateRaceCamera(float deltaTime, float realDeltaTime)
        {
            UpdateSkillCameraTracking(deltaTime);
            var target = GetCameraTarget();
            if (skillCameraTargetTimer <= 0f)
            {
                target = UpdateFinishedCameraTarget(target, realDeltaTime);
            }
            var targetDistance = target != null ? target.Distance : 0f;
            // ??ш끽維???ш끽維??????뺣섕???癲ル슣?????怨뚮옖??????⑥?????ル늅?????獒????ャ뀕???癲ル슢??씙????딅텑???筌먦끉踰????⑤베毓???筌먲퐢??
            cameraDistance = Mathf.Lerp(cameraDistance, targetDistance, 1f - Mathf.Exp(-5f * deltaTime));
            var targetViewDistance = IsFinalDuel()
                ? FinalDuelCameraViewDistance
                : CameraViewDistance;
            raceCameraViewDistance = Mathf.Lerp(
                raceCameraViewDistance,
                targetViewDistance,
                1f - Mathf.Exp(-2.8f * deltaTime));
        }

        private void BeginSkillCameraTracking(Horse caster, float duration)
        {
            if (caster == null || duration <= 0f)
            {
                return;
            }

            if (skillCameraTargetTimer <= 0f)
            {
                cameraTargetBeforeSkill = cameraTargetLane;
            }

            skillCameraTargetLane = caster.Lane;
            skillCameraTargetTimer = duration;
            finishedCameraTargetLane = -1;
            finishedCameraTargetDelay = 0f;
        }

        private void UpdateSkillCameraTracking(float deltaTime)
        {
            if (skillCameraTargetTimer <= 0f)
            {
                return;
            }

            skillCameraTargetTimer = Mathf.Max(0f, skillCameraTargetTimer - deltaTime);
            if (skillCameraTargetTimer > 0f)
            {
                return;
            }

            cameraTargetLane = cameraTargetBeforeSkill;
            skillCameraTargetLane = -1;
            cameraTargetBeforeSkill = -1;
        }

        private Horse UpdateFinishedCameraTarget(Horse target, float realDeltaTime)
        {
            if (target == null || !target.Finished)
            {
                finishedCameraTargetLane = -1;
                finishedCameraTargetDelay = 0f;
                return target;
            }

            var unfinishedLeader = field
                .Where(horse => !horse.Finished)
                .OrderByDescending(horse => horse.Distance)
                .ThenByDescending(horse => horse.CurrentSpeed)
                .ThenBy(horse => horse.Lane)
                .FirstOrDefault();
            if (unfinishedLeader == null)
            {
                return target;
            }

            if (finishedCameraTargetLane != target.Lane)
            {
                finishedCameraTargetLane = target.Lane;
                finishedCameraTargetDelay = 1f;
                cameraTargetLane = target.Lane;
            }

            finishedCameraTargetDelay = Mathf.Max(0f, finishedCameraTargetDelay - realDeltaTime);
            if (finishedCameraTargetDelay > 0f)
            {
                return target;
            }

            cameraTargetLane = unfinishedLeader.Lane;
            finishedCameraTargetLane = -1;
            finishedCameraTargetDelay = 0f;
            return unfinishedLeader;
        }

        private bool IsFinalDuel()
        {
            if (currentRace == null || field.Count < 2)
            {
                return false;
            }

            var leaders = field
                .Where(horse => !horse.Finished)
                .OrderByDescending(horse => horse.Distance)
                .Take(2)
                .ToList();
            if (leaders.Count < 2)
            {
                return false;
            }

            var remainingMeters = (GetTrackLength() - leaders[0].Distance) * 8f;
            var gapMeters = Mathf.Abs(leaders[0].Distance - leaders[1].Distance) * 8f;
            return remainingMeters <= 200f && gapMeters <= 12f;
        }

        private Horse GetCameraTarget()
        {
            if (skillCameraTargetTimer > 0f && skillCameraTargetLane >= 0)
            {
                var caster = field.FirstOrDefault(horse => horse.Lane == skillCameraTargetLane);
                if (caster != null)
                {
                    return caster;
                }
            }

            if (cameraTargetLane >= 0)
            {
                var selected = field.FirstOrDefault(horse => horse.Lane == cameraTargetLane);
                if (selected != null)
                {
                    return selected;
                }
            }

            return field
                .OrderByDescending(horse => horse.Distance)
                .ThenBy(horse => horse.Lane)
                .FirstOrDefault();
        }

        private void CycleCameraTarget(int direction)
        {
            if (skillCameraTargetTimer > 0f)
            {
                return;
            }

            finishedCameraTargetLane = -1;
            finishedCameraTargetDelay = 0f;
            if (field.Count == 0)
            {
                cameraTargetLane = -1;
                return;
            }

            var ordered = field.OrderBy(horse => horse.Lane).ToList();
            var currentIndex = cameraTargetLane < 0
                ? 0
                : ordered.FindIndex(horse => horse.Lane == cameraTargetLane) + 1;
            var optionCount = ordered.Count + 1;
            var nextIndex = (currentIndex + direction) % optionCount;
            if (nextIndex < 0)
            {
                nextIndex += optionCount;
            }

            cameraTargetLane = nextIndex == 0 ? -1 : ordered[nextIndex - 1].Lane;
        }

        private void ToggleLanguage()
        {
            language = language == UiLanguage.Korean ? UiLanguage.English : UiLanguage.Korean;
            MarkUiDirty();
        }

        private void PositionHorseVisuals()
        {
            if (raceWorldView == null)
            {
                return;
            }

            UpdateRaceWorldPresentation();
        }

        private void UpdateRaceWorldPresentation()
        {
            if (raceWorldView == null || field.Count == 0)
            {
                return;
            }

            var visible = phase == GamePhase.Racing || phase == GamePhase.Results;
            raceWorldView.SetVisible(visible, field);
            if (!visible)
            {
                return;
            }

            if (phase == GamePhase.Results)
            {
                raceWorldView.ShowPodium(latestStandings);
                return;
            }

            var target = GetCameraTarget();
            var focusDistance = target != null
                ? target.Distance
                : field.Max(horse => horse.Distance);
            raceWorldView.UpdateView(
                field,
                focusDistance,
                raceCameraViewDistance,
                GetTrackLength(),
                GetCurrentCourseBend(),
                raceClock);
        }

        private string GetBetTypeName(BetType type)
        {
            var korean = language == UiLanguage.Korean;
            return type switch
            {
                BetType.Win => korean ? "\uB2E8\uC2B9\uC2DD" : "Win",
                BetType.Place => korean ? "\uC5F0\uC2B9\uC2DD" : "Place",
                BetType.Quinella => korean ? "\uBCF5\uC2B9\uC2DD" : "Quinella",
                BetType.Exacta => korean ? "\uC30D\uC2B9\uC2DD" : "Exacta",
                _ => type.ToString()
            };
        }

        private string StatLine(Horse horse)
        {
            var aptitudeText = string.Empty;
            if (horse.Data != null && currentRace != null)
            {
                var grade = horse.Data.GetAptitude(currentRace.Surface);
                var percent = Mathf.RoundToInt(GetSurfaceAptitudeMultiplier(horse) * 100f);
                aptitudeText = $"  {L("aptitude_short")} {grade} {percent}%";
            }

            return $"{L("speed_short")} {horse.Speed}  {L("accel_short")} {horse.Acceleration}  {L("stamina_short")} {horse.Stamina}  {L("magic_short")} {horse.Magic}{aptitudeText}  {GetSkillName(horse.Skill)}";
        }

        private Rect GetLowerPanel()
        {
            if (phase == GamePhase.Betting || phase == GamePhase.Shop)
            {
                return new Rect(32f, 148f, Screen.width - 64f, Mathf.Max(360f, Screen.height - 170f));
            }

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

    }
}

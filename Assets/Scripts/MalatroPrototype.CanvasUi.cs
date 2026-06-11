using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Malatro
{
    public sealed partial class MalatroPrototype
    {
        private static readonly Color UiBackground = new Color(0.035f, 0.045f, 0.05f, 1f);
        private static readonly Color UiSurface = new Color(0.075f, 0.09f, 0.095f, 1f);
        private static readonly Color UiSurfaceRaised = new Color(0.11f, 0.125f, 0.13f, 1f);
        private static readonly Color UiBorder = new Color(0.24f, 0.28f, 0.27f, 1f);
        private static readonly Color UiText = new Color(0.93f, 0.94f, 0.9f, 1f);
        private static readonly Color UiMuted = new Color(0.64f, 0.68f, 0.65f, 1f);
        private static readonly Color UiGold = new Color(0.96f, 0.73f, 0.24f, 1f);
        private static readonly Color UiGreen = new Color(0.22f, 0.62f, 0.42f, 1f);
        private static readonly Color UiRed = new Color(0.72f, 0.28f, 0.25f, 1f);
        private static readonly Color BoardBlue = new Color(0.13f, 0.25f, 0.41f, 1f);
        private static readonly Color BoardBlueDark = new Color(0.08f, 0.16f, 0.27f, 1f);
        private static readonly Color CardPaper = new Color(0.96f, 0.95f, 0.9f, 1f);
        private static readonly Color CardPaperMuted = new Color(0.88f, 0.87f, 0.81f, 1f);
        private static readonly Color Ink = new Color(0.09f, 0.085f, 0.075f, 1f);

        private bool canvasUiReady;
        private Canvas productionCanvas;
        private RectTransform productionRoot;
        private RectTransform headerRoot;
        private RectTransform screenRoot;
        private RectTransform modalRoot;
        private TextMeshProUGUI headerGold;
        private TextMeshProUGUI headerProgress;
        private TextMeshProUGUI raceClockText;
        private TextMeshProUGUI raceMessageText;
        private TextMeshProUGUI raceStandingText;
        private TextMeshProUGUI raceTargetText;
        private TextMeshProUGUI raceInfoText;
        private RectTransform raceCourseRect;
        private RectTransform raceFinishMarker;
        private RectTransform raceRouteBar;
        private RectTransform raceRouteMarker;
        private RectTransform raceProgressFill;
        private RectTransform raceProgressMarkers;
        private TextMeshProUGUI raceProgressText;
        private TextMeshProUGUI podiumRecordsText;
        private DynamicRaceTrackGraphic dynamicRaceTrack;
        private TMP_FontAsset uiFontAsset;
        private GameObject horseInfoCardPrefab;
        private GameObject betTicketCardPrefab;
        private GameObject relicSlotPrefab;
        private GameObject relicShopOfferPrefab;
        private readonly Dictionary<Horse, RectTransform> raceHorseIcons = new();
        private readonly Dictionary<Horse, RectTransform> raceProgressHorseMarkers = new();
        private readonly Dictionary<BetTicket, float> raceTicketHitProbabilities = new();
        private float nextRaceTicketProbabilityRefresh;
        private GamePhase renderedPhase = (GamePhase)(-1);
        private bool uiDirty;
        private bool usingEditableSceneUi;

        private void BuildCanvasUi()
        {
            productionCanvas = FindAnyObjectByType<Canvas>();
            if (productionCanvas == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                productionCanvas = canvasObject.GetComponent<Canvas>();
                productionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = productionCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = productionCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (productionCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                productionCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();
            LoadUiFont();

            var editableRoot = productionCanvas.transform.Find("Malatro Editable UI") as RectTransform;
            if (editableRoot != null)
            {
                var generatedRoot = productionCanvas.transform.Find("Malatro Production UI");
                if (generatedRoot != null)
                {
                    Destroy(generatedRoot.gameObject);
                }

                for (var i = 0; i < productionCanvas.transform.childCount; i++)
                {
                    var child = productionCanvas.transform.GetChild(i);
                    child.gameObject.SetActive(child == editableRoot);
                }

                usingEditableSceneUi = true;
                productionRoot = editableRoot;
                modalRoot = FindDeep(productionRoot, "Modal Layer") as RectTransform;
                if (modalRoot == null)
                {
                    modalRoot = CreateRect("Modal Layer", productionRoot);
                    Stretch(modalRoot);
                }

                modalRoot.SetAsLastSibling();
                canvasUiReady = true;
                uiDirty = true;
                RefreshCanvasUi();
                return;
            }

            var oldRoot = productionCanvas.transform.Find("Malatro Production UI");
            if (oldRoot != null)
            {
                Destroy(oldRoot.gameObject);
            }

            for (var i = 0; i < productionCanvas.transform.childCount; i++)
            {
                var child = productionCanvas.transform.GetChild(i);
                if (child.name != "Malatro Production UI")
                {
                    child.gameObject.SetActive(false);
                }
            }

            productionRoot = CreateRect("Malatro Production UI", productionCanvas.transform);
            Stretch(productionRoot);
            var background = CreateImage("Background", productionRoot, UiBackground, true);
            Stretch(background.rectTransform);

            BuildHeader();
            screenRoot = CreateRect("Screen", productionRoot);
            Stretch(screenRoot);

            modalRoot = CreateRect("Modal Layer", productionRoot);
            Stretch(modalRoot);
            modalRoot.SetAsLastSibling();

            canvasUiReady = true;
            uiDirty = true;
            RefreshCanvasUi();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            EventSystem.current = eventSystemObject.GetComponent<EventSystem>();
        }

        private void LoadUiFont()
        {
            var font = Resources.Load<Font>("Fonts/NanumGothic-Bold");
            if (font == null)
            {
                return;
            }

            uiFontAsset = TMP_FontAsset.CreateFontAsset(font);
            uiFontAsset.name = "NanumGothic Bold Runtime SDF";
            uiFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            uiFontAsset.isMultiAtlasTexturesEnabled = true;
        }

        private void BuildHeader()
        {
            headerRoot = CreateImage("Header", productionRoot, new Color(0.055f, 0.065f, 0.07f, 1f), true).rectTransform;
            SetAnchors(headerRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 96f));
            headerRoot.pivot = new Vector2(0.5f, 1f);
            headerRoot.anchoredPosition = Vector2.zero;

            var accent = CreateImage("Accent", headerRoot, UiGold, true).rectTransform;
            SetAnchors(accent, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 3f));

            var brand = CreateText("Brand", headerRoot, "MALATRO", 30, FontStyles.Bold, UiText, TextAlignmentOptions.MidlineLeft);
            SetAnchors(brand.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(28f, 0f), new Vector2(300f, 0f));

            headerGold = CreateText("Gold", headerRoot, string.Empty, 22, FontStyles.Bold, UiGold, TextAlignmentOptions.Center);
            SetAnchors(headerGold.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(260f, 0f));

            headerProgress = CreateText("Progress", headerRoot, string.Empty, 18, FontStyles.Normal, UiMuted, TextAlignmentOptions.MidlineRight);
            SetAnchors(headerProgress.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-430f, 0f), new Vector2(300f, 0f));

            var languageButton = CreateButton("Language", headerRoot, "KR / EN", () =>
            {
                ToggleLanguage();
                MarkUiDirty();
            }, UiSurfaceRaised, 16);
            SetAnchors(languageButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-114f, 0f), new Vector2(132f, 42f));
        }

        private void LateUpdate()
        {
            if (!canvasUiReady)
            {
                return;
            }

            if (renderedPhase != phase || uiDirty)
            {
                RefreshCanvasUi();
            }

            if (phase == GamePhase.Racing)
            {
                UpdateRaceCanvas();
            }
        }

        private void MarkUiDirty()
        {
            uiDirty = true;
        }

        private void RefreshCanvasUi()
        {
            uiDirty = false;
            renderedPhase = phase;
            if (usingEditableSceneUi)
            {
                RefreshEditableSceneUi();
                return;
            }

            if (headerRoot != null)
            {
                headerRoot.gameObject.SetActive(phase == GamePhase.Racing || phase == GamePhase.Results);
            }
            headerGold.text = $"{L("gold")}  {gold:N0}";
            headerProgress.text = phase == GamePhase.Racing || phase == GamePhase.Results
                ? $"{L("round")} {roundNumber}   {L("race")} {GetRaceInRound()}/{RacesPerRound}   {roundEarnedGold:N0}/{roundTargetGold:N0}"
                : $"{L("relic_inventory")} {relicInventory.Count}/{RelicInventory.MaximumCapacity}";

            ClearChildren(screenRoot);
            ClearChildren(modalRoot);
            raceHorseIcons.Clear();
            raceClockText = null;
            raceMessageText = null;
            raceStandingText = null;
            raceTargetText = null;
            raceInfoText = null;
            raceCourseRect = null;
            raceFinishMarker = null;
            raceRouteBar = null;
            raceRouteMarker = null;
            raceProgressFill = null;
            raceProgressMarkers = null;
            raceProgressText = null;
            podiumRecordsText = null;
            raceProgressHorseMarkers.Clear();
            dynamicRaceTrack = null;

            switch (phase)
            {
                case GamePhase.Betting:
                    BuildPredictionScreen();
                    break;
                case GamePhase.Shop:
                    BuildShopScreen();
                    break;
                case GamePhase.Racing:
                    BuildRaceScreen();
                    break;
                case GamePhase.Results:
                    BuildResultsScreen();
                    break;
            }
        }

        private void RefreshEditableSceneUi()
        {
            ClearChildren(modalRoot);
            raceHorseIcons.Clear();
            raceClockText = null;
            raceMessageText = null;
            raceStandingText = null;
            raceTargetText = null;
            raceInfoText = null;
            raceCourseRect = null;
            raceFinishMarker = null;
            raceRouteBar = null;
            raceRouteMarker = null;
            raceProgressFill = null;
            raceProgressMarkers = null;
            raceProgressText = null;
            podiumRecordsText = null;
            raceProgressHorseMarkers.Clear();
            dynamicRaceTrack = null;

            var bettingScreen = FindDirect(productionRoot, "BettingScreen");
            var shopScreen = FindDirect(productionRoot, "ShopScreen");
            var raceScreen = FindDirect(productionRoot, "RaceScreen");

            var showBetting = phase == GamePhase.Betting;
            var showShop = phase == GamePhase.Shop;
            var showRace = phase == GamePhase.Racing || phase == GamePhase.Results;

            SetActive(bettingScreen, showBetting);
            SetActive(shopScreen, showShop);
            SetActive(raceScreen, showRace);

            if (bettingScreen != null)
            {
                BindEditableBoardScreen(bettingScreen, false);
            }

            if (shopScreen != null)
            {
                BindEditableShopScreen(shopScreen);
            }

            if (raceScreen != null)
            {
                BindEditableRaceScreen(raceScreen);
            }

            ApplyUiFontToHierarchy(productionRoot);
        }

        private void BindEditableBoardScreen(Transform screen, bool shopOpen)
        {
            SetEditableRaceInfo(screen, true);
            BindEditableRelicSlots(screen, false);
            BindEditableHorseSlots(screen);
            BindEditableTicketSlots(screen);

            BindEditableButton(screen, "ShopButton", L("shop_screen"), () =>
            {
                OpenShop();
                MarkUiDirty();
            });

            BindEditableButton(screen, "RaceStartButton", L("start_race"), () =>
            {
                StartRace();
                MarkUiDirty();
            });
        }

        private void BindEditableShopScreen(Transform screen)
        {
            SetEditableRaceInfo(screen, true);
            BindEditableRelicSlots(screen, false);
            EnsureEditableShopHorseScroll(screen);
            BindEditableHorseSlots(screen);
            BindEditableShopOffers(screen);

            BindEditableButton(screen, "BackToBettingButton", L("back_prediction"), () =>
            {
                TransitionTo(GamePhase.Betting);
                SetLog("pick_ticket");
            });

            BindEditableButton(screen, "RefreshShopButton", $"{L("refresh_shop")} {relicShopRefreshCost}", () =>
            {
                RefreshRelicShop();
                MarkUiDirty();
            });
        }

        private void EnsureEditableShopHorseScroll(Transform screen)
        {
            if (screen == null || FindDeep(screen, "HorseScroll") != null)
            {
                return;
            }

            CreateHorizontalScrollView(
                "HorseScroll",
                screen,
                620f,
                300f,
                910f,
                220f,
                50f,
                0f,
                0f);
        }

        private void BindEditableRaceScreen(Transform screen)
        {
            raceInfoText = FindDeep(screen, "RaceInfoText")?.GetComponent<TextMeshProUGUI>();
            if (raceInfoText != null)
            {
                raceInfoText.color = Color.black;
            }
            raceClockText = FindDeep(screen, "RaceClock")?.GetComponent<TextMeshProUGUI>();
            raceMessageText = FindDeep(screen, "RaceMessage")?.GetComponent<TextMeshProUGUI>();
            raceStandingText = FindDeep(screen, "RaceStandings")?.GetComponent<TextMeshProUGUI>();
            raceTargetText = FindDeep(screen, "RaceTarget")?.GetComponent<TextMeshProUGUI>();
            raceCourseRect = FindDeep(screen, "RaceCourse") as RectTransform;
            raceFinishMarker = FindDeep(screen, "FinishMarker") as RectTransform;
            raceRouteBar = FindDeep(screen, "RaceRouteBar") as RectTransform;
            raceRouteMarker = FindDeep(screen, "RaceRouteMarker") as RectTransform;
            raceProgressFill = FindDeep(screen, "RaceProgressFill") as RectTransform;
            raceProgressMarkers = FindDeep(screen, "RaceProgressMarkers") as RectTransform;
            raceProgressText = FindDeep(screen, "RaceProgressText")?.GetComponent<TextMeshProUGUI>();
            podiumRecordsText = FindDeep(screen, "PodiumRecordsText")?.GetComponent<TextMeshProUGUI>();
            dynamicRaceTrack = raceCourseRect != null ? raceCourseRect.GetComponent<DynamicRaceTrackGraphic>() : null;
            BuildRaceRouteDisplay();
            ConfigureRaceScreenAsWorldHud(screen);
            SetEditableRaceInfo(screen, true, true);
            BuildRaceProgressMarkers();

            var showingPodium = phase == GamePhase.Results;
            var recordsPanel = FindDeep(screen, "PodiumRecordsPanel");
            SetActive(recordsPanel, showingPodium);
            var progressPanel = FindDeep(screen, "RaceProgressPanel");
            SetActive(progressPanel, !showingPodium);
            var resultButton = FindDeep(screen, "ResultButton");
            SetActive(resultButton, showingPodium);
            SetEditableText(screen, "RaceTitle", showingPodium
                ? (language == UiLanguage.Korean ? "포디움" : "PODIUM")
                : currentRace?.GetName(language == UiLanguage.Korean) ?? "RACE");
            SetEditableText(screen, "RaceSubtitle", showingPodium
                ? (language == UiLanguage.Korean ? "최종 경기 결과" : "Final race results")
                : currentRace != null
                    ? $"{currentRace.League} / {currentRace.GetSurfaceName(language == UiLanguage.Korean)} / {currentRace.TotalDistanceMeters}m"
                    : string.Empty);
            if (podiumRecordsText != null)
            {
                podiumRecordsText.fontSize = latestStandings.Count > 8 ? 14f : 18f;
                podiumRecordsText.text = BuildPodiumRecordsText();
            }

            BindEditableButton(screen, "ResultButton", L("next_race_button"), () =>
            {
                if (phase == GamePhase.Results && resultDelay <= 0f)
                {
                    PrepareNextRace();
                    MarkUiDirty();
                }
            });

            UpdateRaceCanvas();
        }

        private string BuildPodiumRecordsText()
        {
            if (latestStandings.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n\n", latestStandings.Select((horse, index) =>
                $"{index + 1}. {GetShortHorseName(horse)}\n   {horse.FinishTime:0.00}s"));
        }

        private void ConfigureRaceScreenAsWorldHud(Transform screen)
        {
            var background = FindDeep(screen, "Background")?.GetComponent<Image>();
            if (background != null)
            {
                background.color = Color.clear;
                background.raycastTarget = false;
            }

            if (dynamicRaceTrack != null)
            {
                dynamicRaceTrack.enabled = false;
            }

            var finishMarker = FindDeep(screen, "FinishMarker");
            if (finishMarker != null)
            {
                finishMarker.gameObject.SetActive(false);
            }

            var iconLayer = FindDeep(screen, "HorseIconLayer");
            if (iconLayer != null)
            {
                iconLayer.gameObject.SetActive(false);
            }

            if (raceCourseRect == null)
            {
                return;
            }

            for (var i = 0; i < raceCourseRect.childCount; i++)
            {
                var child = raceCourseRect.GetChild(i);
                if (child.name.StartsWith("HorseIcon_", StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void SetEditableRaceInfo(Transform screen, bool showMoney, bool showRaceTime = false)
        {
            var title = currentRace != null ? currentRace.GetName(language == UiLanguage.Korean) : "Race Name";
            var subtitle = currentRace != null
                ? $"{currentRace.League} {currentRace.GetSurfaceName(language == UiLanguage.Korean)}: {currentRace.TotalDistanceMeters}m"
                : "G1 dirt: 2000m";
            var text = $"{title}\n\n{subtitle}\n\n\n{L("round")} {roundNumber} ({GetRaceInRound()}/{RacesPerRound})\n{L("round_goal")}\n{roundEarnedGold:N0}/{roundTargetGold:N0}";
            if (showMoney)
            {
                text += $"\n\n{L("gold")}: {gold:N0}";
            }
            if (showRaceTime)
            {
                text += $"\n{L("race_clock")}: {raceClock:0.0}s";
            }

            SetEditableText(screen, "RaceInfoText", text);
        }

        private void BindEditableHorseSlots(Transform screen)
        {
            var content = GetEditableScrollContent(screen, "HorseScroll");
            if (content == null)
            {
                return;
            }

            horseInfoCardPrefab ??= Resources.Load<GameObject>("UI/HorseInfoCard");
            if (horseInfoCardPrefab == null)
            {
                return;
            }

            ClearSpawnedUi(content);
            for (var i = 0; i < field.Count; i++)
            {
                var horse = field[i];
                var slot = InstantiateEditablePrefab(horseInfoCardPrefab, content, $"HorseSlot_{i + 1:00}");
                if (slot == null)
                {
                    continue;
                }

                SetEditableText(slot, "TitleText", GetShortHorseName(horse));
                SetEditableText(slot, "OddsText", $"{horse.WinOdds:0.0}x");
                SetHorseImage(slot, horse, "HorseImage", 35f, 52f, 130f, 84f);
                var image = slot.GetComponent<Image>();
                if (image != null)
                {
                    image.color = CardPaper;
                }

                BindEditableButton(slot, null, null, () => ShowHorseModal(horse));
            }

            FitHorizontalContent(content);
        }

        private void BindEditableTicketSlots(Transform screen)
        {
            var content = GetEditableScrollContent(screen, "BetTicketScroll");
            if (content == null)
            {
                return;
            }

            betTicketCardPrefab ??= Resources.Load<GameObject>("UI/BetTicketCard");
            if (betTicketCardPrefab == null)
            {
                return;
            }

            ClearSpawnedUi(content);
            for (var i = 0; i < offeredTickets.Count; i++)
            {
                var ticket = offeredTickets[i];
                var slot = InstantiateEditablePrefab(betTicketCardPrefab, content, $"BetTicket_{i + 1:00}");
                if (slot == null)
                {
                    continue;
                }

                SetEditableText(slot, "TitleText", $"BetTicket {i + 1}");
                BindEditableButton(slot, "TicketTypeButton", ticket.GetTypeName(language == UiLanguage.Korean), () => ShowTicketTypeModal(ticket));
                BindEditableButton(slot, "FirstHorseButton", GetShortHorseName(ticket.First), () => ShowTicketHorseModal(ticket, false));
                var second = FindDeep(slot, "SecondHorseButton");
                if (second != null)
                {
                    second.gameObject.SetActive(ticket.NeedsSecondHorse);
                    BindEditableButton(slot, "SecondHorseButton", ticket.NeedsSecondHorse ? GetShortHorseName(ticket.Second) : string.Empty, () => ShowTicketHorseModal(ticket, true));
                }

                BindEditableButton(slot, "BuyTicketButton", $"{L("payout")} {GetRelicAdjustedPayout(ticket):N0}", () => ShowTicketTypeModal(ticket));
            }

            FitHorizontalContent(content);
        }

        private void BindEditableRelicSlots(Transform screen, bool shopOffers)
        {
            var content = GetEditableScrollContent(screen, "OwnedRelicsScroll");
            if (content == null)
            {
                return;
            }

            relicSlotPrefab ??= Resources.Load<GameObject>("UI/RelicSlot");
            if (relicSlotPrefab == null)
            {
                return;
            }

            ClearSpawnedUi(content);
            for (var i = 0; i < RelicInventory.MaximumCapacity; i++)
            {
                var slot = InstantiateEditablePrefab(relicSlotPrefab, content, $"RelicSlot_{i + 1:00}");
                if (slot == null)
                {
                    continue;
                }

                var hasRelic = i < relicInventory.Count;
                var relic = hasRelic ? relicInventory.Relics[i] : null;
                SetEditableText(slot, "TitleText", hasRelic
                    ? relic.GetName(language == UiLanguage.Korean)
                    : L("empty_relic_slot"));
                BindEditableButton(slot, null, null, () =>
                {
                    if (relic != null && relicInventory.Contains(relic))
                    {
                        ShowRelicModal(relic);
                    }
                });
            }

            FitHorizontalContent(content);
        }

        private void BindEditableShopOffers(Transform screen)
        {
            var content = GetEditableScrollContent(screen, "ShopOfferScroll");
            if (content == null)
            {
                return;
            }

            relicShopOfferPrefab ??= Resources.Load<GameObject>("UI/RelicShopOfferCard");
            if (relicShopOfferPrefab == null)
            {
                return;
            }

            ClearSpawnedUi(content);
            for (var i = 0; i < shopOffers.Count; i++)
            {
                var entry = shopOffers[i];
                var slot = InstantiateEditablePrefab(relicShopOfferPrefab, content, $"ShopOffer_{i + 1:00}");
                if (slot == null)
                {
                    continue;
                }

                if (entry.IsRelic)
                {
                    var relic = entry.Relic;
                    var owned = relicInventory.Contains(relic);
                    SetEditableText(
                        slot,
                        "TitleText",
                        owned
                            ? $"[{L("owned")}] {relic.GetName(language == UiLanguage.Korean)}"
                            : relic.GetName(language == UiLanguage.Korean));
                    SetEditableText(slot, "DescriptionText", relic.GetDescription(language == UiLanguage.Korean));
                    SetEditableText(
                        slot,
                        "PriceText",
                        owned ? L("owned") : $"{L("buy")} {relic.Price:N0}");
                    BindEditableButton(slot, "BuyButton", owned ? L("owned") : L("buy"), () =>
                    {
                        if (relicInventory.Contains(relic))
                        {
                            return;
                        }

                        BuyRelic(relic);
                        MarkUiDirty();
                    });
                    SetEditableShopOfferOwnedState(slot, owned, relic.Color);
                    continue;
                }

                var offer = entry.StatOffer;
                SetEditableText(slot, "TitleText", GetHorseStatOfferTitle(offer));
                SetEditableText(slot, "DescriptionText", GetHorseStatOfferDescription(offer));
                SetEditableText(
                    slot,
                    "PriceText",
                    offer.Purchased ? L("purchase_complete") : $"{L("buy")} {offer.Price:N0}");
                BindEditableButton(
                    slot,
                    "BuyButton",
                    offer.Purchased ? L("purchase_complete") : L("buy"),
                    () => BeginHorseStatOfferPurchase(offer));
                SetEditableShopOfferOwnedState(slot, offer.Purchased, GetShopRarityColor(offer.Rarity));
            }

            FitHorizontalContent(content);
        }

        private void BuildPredictionScreen()
        {
            BuildMainBoard(false);
        }

        private void BuildMainBoard(bool shopOpen)
        {
            var background = CreateImage("Board Background", screenRoot, BoardBlue, true);
            Stretch(background.rectTransform);
            AddBoardBands(screenRoot);

            BuildRaceInfoPanel(screenRoot, true);
            BuildRelicSlots(screenRoot, shopOpen);
            BuildBoardHorseSlots(screenRoot);
            BuildBoardTicketSlots(screenRoot);

            var raceButton = CreateWhiteButton("Race Start", screenRoot, "race Start", () =>
            {
                StartRace();
                MarkUiDirty();
            }, 170f, 19);
            SetFixed(raceButton.GetComponent<RectTransform>(), 1662f, 815f, 200f, 150f);
        }

        private void BuildRaceInfoPanel(Transform parent, bool showMoney)
        {
            var panel = CreateImage("Race Info", parent, CardPaper, true);
            SetFixed(panel.rectTransform, 100f, 50f, 400f, 980f);
            DecorateCard(panel.gameObject, UiGold, true);
            AddAccentBar(panel.transform, UiGold, 8f);

            var title = currentRace != null ? currentRace.GetName(language == UiLanguage.Korean) : "Race Name";
            var subtitle = currentRace != null
                ? $"{currentRace.League} {currentRace.GetSurfaceName(language == UiLanguage.Korean)}: {currentRace.TotalDistanceMeters}m"
                : "G1 dirt: 2000m";
            var text = $"{title}\n\n{subtitle}\n\n\n{L("round")} {roundNumber} ({GetRaceInRound()}/{RacesPerRound})\n{L("round_goal")}\n{roundEarnedGold:N0}/{roundTargetGold:N0}";
            if (showMoney)
            {
                text += $"\n\nmoney: {gold:N0}";
            }

            var label = CreateText("Race Info Text", panel.transform, text, 26, FontStyles.Normal, Ink, TextAlignmentOptions.Top);
            label.lineSpacing = 4f;
            label.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-48f, -56f));
        }

        private void BuildRelicSlots(Transform parent, bool shopOpen)
        {
            var slotCount = Mathf.Max(4, RelicInventory.MaximumCapacity);
            var scroll = CreateHorizontalScrollView("Relic Scroll", parent, 686f, 26f, 820f, 132f, 22f, 0f, 0f);
            for (var i = 0; i < slotCount; i++)
            {
                if (shopOpen && i < relicShopOffers.Count)
                {
                    var relic = relicShopOffers[i];
                    var owned = relicInventory.Contains(relic);
                    var label = owned
                        ? $"[{L("owned")}]\n{relic.GetName(language == UiLanguage.Korean)}"
                        : relic.GetName(language == UiLanguage.Korean);
                    var button = CreateWhiteButton($"Relic Offer {i}", scroll, label, () =>
                    {
                        if (relicInventory.Contains(relic))
                        {
                            return;
                        }

                        BuyRelic(relic);
                        MarkUiDirty();
                    }, 112f, owned ? 16 : 20);
                    button.GetComponent<Button>().interactable = !owned;
                    DecorateCard(button, owned ? UiMuted : relic.Color, true);
                    AddAccentBar(button.transform, relic.Color, 6f);
                    AddLayoutElement(button, 150f, 112f, 0f);
                    continue;
                }

                var ownedText = i < relicInventory.Count
                    ? relicInventory.Relics[i].GetName(language == UiLanguage.Korean)
                    : "Relic";
                var ownedRelic = i < relicInventory.Count ? relicInventory.Relics[i] : null;
                var slot = CreateWhiteButton($"Relic {i}", scroll, ownedText, () =>
                {
                    if (ownedRelic != null && relicInventory.Contains(ownedRelic))
                    {
                        ShowRelicModal(ownedRelic);
                    }
                }, 112f, 20);
                var accent = i < relicInventory.Count ? relicInventory.Relics[i].Color : CardPaperMuted;
                DecorateCard(slot, accent, true);
                AddAccentBar(slot.transform, accent, 6f);
                AddLayoutElement(slot, 150f, 112f, 0f);
            }

            FitHorizontalContent(scroll);
        }

        private void BuildBoardHorseSlots(Transform parent)
        {
            var scroll = CreateHorizontalScrollView("Horse Scroll", parent, 620f, 300f, 910f, 220f, 50f, 0f, 0f);
            for (var i = 0; i < field.Count; i++)
            {
                var horse = field[i];
                var capturedHorse = horse;
                var button = CreateWhiteButton($"Horse {i}", scroll, GetShortHorseName(horse), () => ShowHorseModal(capturedHorse), 160f, 20);
                DecorateCard(button, horse.Color, true);
                AddAccentBar(button.transform, horse.Color, 10f);
                var title = button.GetComponentInChildren<TextMeshProUGUI>();
                if (title != null)
                {
                    SetFixed(title.rectTransform, 16f, 18f, 168f, 34f);
                    title.alignment = TextAlignmentOptions.Center;
                }
                SetHorseImage(button.transform, horse, "HorseImage", 35f, 52f, 130f, 84f);
                var odds = CreateText("Odds", button.transform, $"{horse.WinOdds:0.0}x", 18, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
                SetFixed(odds.rectTransform, 16f, 150f, 168f, 32f);
                AddLayoutElement(button, 200f, 200f, 0f);
            }

            FitHorizontalContent(scroll);
        }

        private void BuildBoardTicketSlots(Transform parent)
        {
            var scroll = CreateHorizontalScrollView("Ticket Scroll", parent, 590f, 660f, 1040f, 420f, 50f, 0f, 0f);
            for (var i = 0; i < offeredTickets.Count; i++)
            {
                BuildBoardTicket(scroll, offeredTickets[i], i, 0f, 0f, 300f, 400f);
            }

            FitHorizontalContent(scroll);
        }

        private void BuildBoardTicket(Transform parent, BetTicket ticket, int index, float x, float y, float width, float height)
        {
            var panel = CreateImage($"BetTicket {index + 1}", parent, CardPaper, true);
            if (parent is RectTransform parentRect && parentRect.GetComponent<HorizontalLayoutGroup>() != null)
            {
                AddLayoutElement(panel.gameObject, width, height, 0f);
            }
            else
            {
                SetFixed(panel.rectTransform, x, y, width, height);
            }
            DecorateCard(panel.gameObject, UiGold, true);
            AddAccentBar(panel.transform, UiGold, 9f);

            var compact = height < 260f;
            var innerWidth = width - 44f;
            var titleY = compact ? 18f : 56f;
            var buttonHeight = compact ? 34f : 46f;
            var buttonGap = compact ? 40f : 62f;
            var firstButtonY = compact ? 62f : 126f;
            var fontSize = compact ? 15 : 16;

            var title = CreateText("Title", panel.transform, compact ? $"BetTicket {index + 1}" : "BetTicket", compact ? 20 : 26, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
            SetFixed(title.rectTransform, 20f, titleY, innerWidth, compact ? 34f : 48f);

            var type = CreateBoardMiniButton("Type", panel.transform, ticket.GetTypeName(language == UiLanguage.Korean), () => ShowTicketTypeModal(ticket));
            SetFixed(type.GetComponent<RectTransform>(), 22f, firstButtonY, innerWidth, buttonHeight);
            SetButtonFontSize(type, fontSize);

            var first = CreateBoardMiniButton("First", panel.transform, GetShortHorseName(ticket.First), () => ShowTicketHorseModal(ticket, false));
            SetFixed(first.GetComponent<RectTransform>(), 22f, firstButtonY + buttonGap, innerWidth, buttonHeight);
            SetButtonFontSize(first, fontSize);

            if (ticket.NeedsSecondHorse)
            {
                var second = CreateBoardMiniButton("Second", panel.transform, GetShortHorseName(ticket.Second), () => ShowTicketHorseModal(ticket, true));
                SetFixed(second.GetComponent<RectTransform>(), 22f, firstButtonY + buttonGap * 2f, innerWidth, buttonHeight);
                SetButtonFontSize(second, fontSize);
            }

            var payoutText = compact
                ? $"{ticket.Odds:0.0}x / {GetRelicAdjustedPayout(ticket):N0}"
                : $"{ticket.Odds:0.0}x\n{L("payout")} {GetRelicAdjustedPayout(ticket):N0}";
            var payout = CreateText("Payout", panel.transform, payoutText, compact ? 15 : 20, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
            SetFixed(payout.rectTransform, 22f, height - (compact ? 36f : 82f), innerWidth, compact ? 28f : 60f);
        }

        private void BuildShopScreen()
        {
            BuildShopBoard();
        }

        private void BuildShopBoard()
        {
            var background = CreateImage("Shop Background", screenRoot, BoardBlue, true);
            Stretch(background.rectTransform);
            AddBoardBands(screenRoot);

            BuildRaceInfoPanel(screenRoot, true);
            BuildRelicSlots(screenRoot, false);
            BuildBoardHorseSlots(screenRoot);

            var backButton = CreateWhiteButton("Back To Betting", screenRoot, "bating", () =>
            {
                TransitionTo(GamePhase.Betting);
                SetLog("pick_ticket");
            }, 150f, 20);
            SetFixed(backButton.GetComponent<RectTransform>(), 1640f, 14f, 200f, 150f);

            var offerScroll = CreateHorizontalScrollView("Shop Offer Scroll", screenRoot, 600f, 670f, 1010f, 410f, 45f, 0f, 0f);
            for (var i = 0; i < shopOffers.Count; i++)
            {
                var entry = shopOffers[i];
                if (entry.IsRelic)
                {
                    BuildShopOfferCard(offerScroll, entry.Relic, 0f);
                }
                else
                {
                    BuildHorseStatOfferCard(offerScroll, entry.StatOffer);
                }
            }

            FitHorizontalContent(offerScroll);

            var refresh = CreateWhiteButton("Refresh Shop", screenRoot, $"{L("refresh_shop")}\n{relicShopRefreshCost}", () =>
            {
                RefreshRelicShop();
                MarkUiDirty();
            }, 150f, 20);
            var refreshLabel = refresh.GetComponentInChildren<TextMeshProUGUI>();
            if (refreshLabel != null)
            {
                refreshLabel.text = "refrash";
            }
            SetFixed(refresh.GetComponent<RectTransform>(), 1640f, 805f, 200f, 150f);
        }

        private void BuildShopOfferCard(Transform parent, RelicData relic, float x)
        {
            var owned = relicInventory.Contains(relic);
            var card = CreateImage(
                $"Buy Relic {relic.Id}",
                parent,
                owned ? Tint(relic.Color, 0.32f) : CardPaper,
                true);
            if (parent is RectTransform parentRect && parentRect.GetComponent<HorizontalLayoutGroup>() != null)
            {
                AddLayoutElement(card.gameObject, 300f, 400f, 0f);
            }
            else
            {
                SetFixed(card.rectTransform, x, 670f, 300f, 400f);
            }
            DecorateCard(card.gameObject, relic.Color, true);
            AddAccentBar(card.transform, relic.Color, 9f);

            var name = CreateText(
                "Name",
                card.transform,
                owned
                    ? $"[{L("owned")}]\n{relic.GetName(language == UiLanguage.Korean)}"
                    : relic.GetName(language == UiLanguage.Korean),
                owned ? 18 : 22,
                FontStyles.Bold,
                Ink,
                TextAlignmentOptions.Center);
            SetFixed(name.rectTransform, 20f, 72f, 260f, 64f);

            var price = CreateText(
                "Price",
                card.transform,
                owned ? L("owned") : $"{L("buy")} {relic.Price}",
                19,
                FontStyles.Bold,
                owned ? UiGreen : Ink,
                TextAlignmentOptions.Center);
            SetFixed(price.rectTransform, 20f, 154f, 260f, 42f);

            var description = CreateText("Description", card.transform, relic.GetDescription(language == UiLanguage.Korean), 15, FontStyles.Normal, Ink, TextAlignmentOptions.Top);
            description.textWrappingMode = TextWrappingModes.Normal;
            SetFixed(description.rectTransform, 24f, 220f, 252f, 92f);

            var buy = CreateBoardMiniButton("Buy", card.transform, owned ? L("owned") : L("buy"), () =>
            {
                if (relicInventory.Contains(relic))
                {
                    return;
                }

                BuyRelic(relic);
                MarkUiDirty();
            });
            buy.GetComponent<Button>().interactable = !owned;
            SetFixed(buy.GetComponent<RectTransform>(), 44f, 318f, 212f, 48f);
        }

        private void BuildHorseStatOfferCard(Transform parent, HorseStatShopOffer offer)
        {
            var accent = GetShopRarityColor(offer.Rarity);
            var card = CreateImage(
                offer.RequiresHorseSelection ? "Choose Horse Stat Offer" : "Random Horse Stat Offer",
                parent,
                offer.Purchased ? Tint(accent, 0.34f) : CardPaper,
                true);
            AddLayoutElement(card.gameObject, 300f, 400f, 0f);
            DecorateCard(card.gameObject, accent, true);
            AddAccentBar(card.transform, accent, 9f);

            var name = CreateText(
                "Name",
                card.transform,
                GetHorseStatOfferTitle(offer),
                21,
                FontStyles.Bold,
                Ink,
                TextAlignmentOptions.Center);
            name.textWrappingMode = TextWrappingModes.Normal;
            SetFixed(name.rectTransform, 20f, 58f, 260f, 78f);

            var price = CreateText(
                "Price",
                card.transform,
                offer.Purchased ? L("purchase_complete") : $"{L("buy")} {offer.Price:N0}",
                19,
                FontStyles.Bold,
                offer.Purchased ? UiGreen : Ink,
                TextAlignmentOptions.Center);
            SetFixed(price.rectTransform, 20f, 150f, 260f, 42f);

            var description = CreateText(
                "Description",
                card.transform,
                GetHorseStatOfferDescription(offer),
                16,
                FontStyles.Bold,
                Ink,
                TextAlignmentOptions.Center);
            description.textWrappingMode = TextWrappingModes.Normal;
            SetFixed(description.rectTransform, 24f, 208f, 252f, 98f);

            var buy = CreateBoardMiniButton(
                "Buy",
                card.transform,
                offer.Purchased ? L("purchase_complete") : L("buy"),
                () => BeginHorseStatOfferPurchase(offer));
            buy.GetComponent<Button>().interactable = !offer.Purchased;
            SetFixed(buy.GetComponent<RectTransform>(), 44f, 318f, 212f, 48f);
        }

        private string GetHorseStatOfferTitle(HorseStatShopOffer offer)
        {
            if (offer == null)
            {
                return string.Empty;
            }

            var target = offer.RequiresHorseSelection
                ? (language == UiLanguage.Korean ? "선택" : "Select")
                : offer.RandomTarget != null
                    ? GetHorseName(offer.RandomTarget)
                    : L("random_horse_offer");
            return $"[{GetRarityName(offer.Rarity)}] {target} "
                + $"{GetHorseStatName(offer.Stat)} {GetSignedStatAmount(offer.Amount)}";
        }

        private string GetHorseStatOfferDescription(HorseStatShopOffer offer)
        {
            if (offer == null)
            {
                return string.Empty;
            }

            return offer.RequiresHorseSelection
                ? L("select_horse_offer")
                : L("random_horse_offer");
        }

        private static Color GetShopRarityColor(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => new Color(0.55f, 0.6f, 0.62f),
                RelicRarity.Rare => new Color(0.2f, 0.62f, 0.9f),
                RelicRarity.Epic => new Color(0.68f, 0.34f, 0.9f),
                RelicRarity.Legendary => new Color(0.95f, 0.62f, 0.12f),
                _ => UiGreen
            };
        }

        private void BeginHorseStatOfferPurchase(HorseStatShopOffer offer)
        {
            if (offer == null || offer.Purchased)
            {
                return;
            }

            if (gold < offer.Price)
            {
                SetLog("stat_offer_need_gold", offer.Price);
                MarkUiDirty();
                return;
            }

            if (!offer.RequiresHorseSelection)
            {
                BuyHorseStatOffer(offer, offer.RandomTarget);
                MarkUiDirty();
                return;
            }

            ShowHorseStatOfferSelectionModal(offer);
        }

        private void ShowHorseStatOfferSelectionModal(HorseStatShopOffer offer)
        {
            ClearChildren(modalRoot);
            CreateModalBackdrop(() => ClearChildren(modalRoot));
            var modal = CreateModalPanel("Stat Offer Horse Selection", 620f, 720f);
            var layout = modal.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 22, 22);
            layout.spacing = 8f;

            var title = CreateText(
                "Title",
                modal,
                $"{GetHorseStatOfferTitle(offer)}\n{L("select_horse")}",
                23,
                FontStyles.Bold,
                UiText,
                TextAlignmentOptions.Center);
            AddLayoutElement(title.gameObject, -1f, 78f, 0f);

            foreach (var horse in roster)
            {
                var capturedHorse = horse;
                var button = CreateButton(
                    $"Horse {horse.Lane}",
                    modal,
                    GetHorseStatOfferSelectionLabel(horse, offer),
                    () =>
                    {
                        BuyHorseStatOffer(offer, capturedHorse);
                        ClearChildren(modalRoot);
                        MarkUiDirty();
                    },
                    Tint(horse.Color, 0.22f),
                    15);
                AddLayoutElement(button, -1f, 52f, 0f);
            }

            var close = CreateButton(
                "Close",
                modal,
                "OK",
                () => ClearChildren(modalRoot),
                UiSurfaceRaised,
                16);
            AddLayoutElement(close, -1f, 46f, 0f);
        }

        private string GetHorseStatOfferSelectionLabel(Horse horse, HorseStatShopOffer offer)
        {
            var current = GetHorseStatValue(horse, offer.Stat);
            var adjusted = GetAdjustedHorseStatValue(horse, offer);
            return $"{GetHorseName(horse)}    "
                + $"{GetHorseStatName(offer.Stat)} {current} → {adjusted}    "
                + $"({GetSignedStatAmount(offer.Amount)})";
        }

        private void BuildRaceScreen()
        {
            var layout = CreateVerticalLayout("Race", screenRoot, 14f, new RectOffset(0, 0, 0, 0));
            var raceName = currentRace != null
                ? $"{currentRace.GetName(language == UiLanguage.Korean)}   {currentRace.League} / {currentRace.GetSurfaceName(language == UiLanguage.Korean)} / {currentRace.TotalDistanceMeters}m"
                : L("race_data_missing");
            AddSectionHeader(layout, raceName, string.Empty);

            var track = CreateImage("Track", layout, new Color(0.09f, 0.18f, 0.13f, 1f), true);
            AddLayoutElement(track.gameObject, -1f, 650f, 1f);
            var trackLayout = track.gameObject.AddComponent<VerticalLayoutGroup>();
            trackLayout.padding = new RectOffset(26, 26, 22, 22);
            trackLayout.spacing = 6f;
            trackLayout.childControlHeight = true;
            trackLayout.childForceExpandHeight = true;

            for (var i = 0; i < field.Count; i++)
            {
                BuildRaceLane(track.transform, field[i], i);
            }

            var controls = CreateHorizontalLayout("Race Controls", layout, 10f, new RectOffset(0, 0, 0, 0));
            AddLayoutElement(controls.gameObject, -1f, 78f, 0f);
            raceClockText = CreateText("Clock", controls, string.Empty, 23, FontStyles.Bold, UiGold, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(raceClockText.gameObject, 230f, 62f, 0f);
            raceMessageText = CreateText("Message", controls, string.Empty, 15, FontStyles.Normal, UiMuted, TextAlignmentOptions.Center);
            AddLayoutElement(raceMessageText.gameObject, -1f, 62f, 1f);
            foreach (var speed in new[] { 1f, 1.5f, 2f })
            {
                var capturedSpeed = speed;
                var button = CreateButton($"Speed {speed}", controls, $"{speed:0.#}x", () =>
                {
                    racePlaybackSpeed = capturedSpeed;
                    UpdateRaceCanvas();
                }, UiSurfaceRaised, 16);
                AddLayoutElement(button, 86f, 48f, 0f);
            }

            UpdateRaceCanvas();
        }

        private void BuildResultsScreen()
        {
            var layout = CreateVerticalLayout("Results", screenRoot, 14f, new RectOffset(0, 0, 0, 0));
            AddSectionHeader(layout, L("results"), GetLog());

            var standings = CreateVerticalLayout("Standings", layout, 8f, new RectOffset(0, 0, 0, 0));
            AddLayoutElement(standings.gameObject, -1f, 650f, 1f);
            for (var i = 0; i < latestStandings.Count; i++)
            {
                var horse = latestStandings[i];
                var row = CreateImage($"Standing {i + 1}", standings, i == 0 ? new Color(0.24f, 0.2f, 0.08f, 1f) : UiSurface, true);
                AddLayoutElement(row.gameObject, -1f, 72f, 0f);
                var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(18, 18, 8, 8);
                rowLayout.spacing = 16f;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;

                var place = CreateText("Place", row.transform, $"{i + 1}", 26, FontStyles.Bold, i == 0 ? UiGold : UiText, TextAlignmentOptions.Center);
                AddLayoutElement(place.gameObject, 54f, 54f, 0f);
                var name = CreateText("Horse", row.transform, GetHorseName(horse), 20, FontStyles.Bold, UiText, TextAlignmentOptions.MidlineLeft);
                AddLayoutElement(name.gameObject, 330f, 54f, 0f);
                var detail = CreateText("Detail", row.transform, $"{horse.FinishTime:0.00}s    {L("odds")} {horse.WinOdds:0.0}x", 17, FontStyles.Normal, UiMuted, TextAlignmentOptions.MidlineRight);
                AddLayoutElement(detail.gameObject, -1f, 54f, 1f);
            }

            var footer = CreateHorizontalLayout("Actions", layout, 12f, new RectOffset(0, 0, 0, 0));
            AddLayoutElement(footer.gameObject, -1f, 58f, 0f);
            CreateFlexibleSpacer(footer);
            var label = raceNumber % RacesPerRound == 0 ? L("check_round_button") : L("next_race_button");
            var next = CreateButton("Next", footer, label.Replace(" (Space)", string.Empty), () =>
            {
                if (resultDelay > 0f)
                {
                    return;
                }

                PrepareNextRace();
                MarkUiDirty();
            }, UiGreen, 17);
            AddLayoutElement(next, 220f, 50f, 0f);
        }

        private void AddSectionHeader(Transform parent, string title, string subtitle)
        {
            var block = CreateVerticalLayout("Section Header", parent, 2f, new RectOffset(4, 4, 0, 0));
            AddLayoutElement(block.gameObject, -1f, string.IsNullOrEmpty(subtitle) ? 52f : 78f, 0f);
            var titleText = CreateText("Title", block, title, 30, FontStyles.Bold, UiText, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(titleText.gameObject, -1f, 44f, 0f);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleText = CreateText("Subtitle", block, subtitle, 15, FontStyles.Normal, UiMuted, TextAlignmentOptions.MidlineLeft);
                AddLayoutElement(subtitleText.gameObject, -1f, 26f, 0f);
            }
        }

        private void BuildHorseStrip(Transform parent)
        {
            var strip = CreateHorizontalLayout("Horse Strip", parent, 12f, new RectOffset(0, 0, 0, 0));
            AddLayoutElement(strip.gameObject, -1f, 132f, 0f);
            foreach (var horse in field)
            {
                var capturedHorse = horse;
                var card = CreateImage($"Horse {horse.Lane + 1}", strip, Tint(horse.Color, 0.18f), true);
                AddLayoutElement(card.gameObject, 220f, 126f, 1f);
                var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(10, 10, 8, 8);
                layout.spacing = 3f;
                layout.childAlignment = TextAnchor.MiddleCenter;

                var name = CreateButton("Horse Button", card.transform, GetShortHorseName(horse), () => ShowHorseModal(capturedHorse), Tint(horse.Color, 0.32f), 16);
                AddLayoutElement(name, -1f, 42f, 0f);
                var odds = CreateText("Odds", card.transform, $"{L("odds")} {horse.WinOdds:0.0}x", 15, FontStyles.Bold, UiGold, TextAlignmentOptions.Center);
                AddLayoutElement(odds.gameObject, -1f, 24f, 0f);
                var skill = CreateText("Skill", card.transform, GetSkillName(horse.Skill), 13, FontStyles.Normal, UiMuted, TextAlignmentOptions.Center);
                AddLayoutElement(skill.gameObject, -1f, 28f, 0f);
            }
        }

        private void BuildTicketCard(Transform parent, BetTicket ticket, int index)
        {
            var card = CreateImage($"Ticket {index + 1}", parent, UiSurface, true);
            AddLayoutElement(card.gameObject, 420f, 300f, 1f);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 10f;

            var number = CreateText("Number", card.transform, $"TICKET  {index + 1:00}", 13, FontStyles.Bold, UiGold, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(number.gameObject, -1f, 24f, 0f);
            var type = CreateButton("Type", card.transform, ticket.GetTypeName(language == UiLanguage.Korean), () => ShowTicketTypeModal(ticket), UiSurfaceRaised, 17);
            AddLayoutElement(type, -1f, 48f, 0f);
            var first = CreateButton("First Horse", card.transform, GetHorseName(ticket.First), () => ShowTicketHorseModal(ticket, false), Tint(ticket.First.Color, 0.28f), 16);
            AddLayoutElement(first, -1f, 48f, 0f);
            if (ticket.NeedsSecondHorse)
            {
                var secondHorse = ticket.Second;
                var second = CreateButton("Second Horse", card.transform, GetHorseName(secondHorse), () => ShowTicketHorseModal(ticket, true), Tint(secondHorse.Color, 0.28f), 16);
                AddLayoutElement(second, -1f, 48f, 0f);
            }
            else
            {
                CreateFlexibleSpacer(card.transform);
            }

            var payout = CreateText("Payout", card.transform, $"{ticket.Odds:0.0}x    {L("payout")} {GetRelicAdjustedPayout(ticket):N0}", 17, FontStyles.Bold, UiGold, TextAlignmentOptions.Center);
            AddLayoutElement(payout.gameObject, -1f, 36f, 0f);
        }

        private void BuildRelicCard(Transform parent, RelicData relic)
        {
            var owned = relicInventory.Contains(relic);
            var card = CreateImage($"Relic {relic.Id}", parent, Tint(relic.Color, 0.16f), true);
            AddLayoutElement(card.gameObject, 470f, 224f, 1f);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8f;

            var rarity = CreateText("Rarity", card.transform, GetRarityName(relic.Rarity).ToUpperInvariant(), 12, FontStyles.Bold, relic.Color, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(rarity.gameObject, -1f, 20f, 0f);
            var name = CreateText("Name", card.transform, relic.GetName(language == UiLanguage.Korean), 20, FontStyles.Bold, UiText, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(name.gameObject, -1f, 34f, 0f);
            var description = CreateText("Description", card.transform, relic.GetDescription(language == UiLanguage.Korean), 14, FontStyles.Normal, UiMuted, TextAlignmentOptions.TopLeft);
            description.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(description.gameObject, -1f, 82f, 1f);
            var button = CreateButton("Buy", card.transform, owned ? L("owned") : $"{L("buy")}  {relic.Price}", () =>
            {
                BuyRelic(relic);
                MarkUiDirty();
            }, owned ? UiSurfaceRaised : UiGreen, 16);
            button.GetComponent<Button>().interactable = !owned;
            AddLayoutElement(button, -1f, 42f, 0f);
        }

        private void BuildInventoryRow(Transform parent)
        {
            var inventory = CreateHorizontalLayout("Inventory", parent, 10f, new RectOffset(0, 0, 0, 0));
            AddLayoutElement(inventory.gameObject, -1f, 78f, 0f);
            for (var i = 0; i < RelicInventory.MaximumCapacity; i++)
            {
                if (i >= relicInventory.Count)
                {
                    var empty = CreateImage("Empty", inventory, UiSurface, true);
                    var emptyLabel = CreateText("Label", empty.transform, L("empty_relic_slot"), 14, FontStyles.Normal, UiMuted, TextAlignmentOptions.Center);
                    Stretch(emptyLabel.rectTransform);
                    AddLayoutElement(empty.gameObject, 320f, 70f, 1f);
                    continue;
                }

                var relic = relicInventory.Relics[i];
                var button = CreateButton(
                    $"Owned {i}",
                    inventory,
                    relic.GetName(language == UiLanguage.Korean),
                    () => ShowRelicModal(relic),
                    Tint(relic.Color, 0.22f),
                    14);
                AddLayoutElement(button, 320f, 70f, 1f);
            }
        }

        private void BuildRaceLane(Transform parent, Horse horse, int index)
        {
            var lane = CreateImage($"Lane {index + 1}", parent, index % 2 == 0 ? new Color(0.12f, 0.23f, 0.16f, 1f) : new Color(0.1f, 0.2f, 0.14f, 1f), true);
            var laneRect = lane.rectTransform;
            var label = CreateText("Label", lane.transform, $"{index + 1}  {GetShortHorseName(horse)}", 14, FontStyles.Bold, UiText, TextAlignmentOptions.MidlineLeft);
            SetAnchors(label.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(12f, 0f), new Vector2(180f, 0f));

            var line = CreateImage("Line", lane.transform, new Color(0.82f, 0.85f, 0.78f, 0.18f), true).rectTransform;
            SetAnchors(line, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(190f, 0f), new Vector2(-34f, 2f));

            var icon = CreateImage("Horse", lane.transform, horse.Color, true);
            icon.preserveAspect = true;
            if (horse.RunSheet != null)
            {
                var sprite = Sprite.Create(horse.RunSheet, new Rect(0f, 0f, horse.RunSheet.width * 0.5f, horse.RunSheet.height), new Vector2(0.5f, 0.5f));
                icon.sprite = sprite;
                icon.color = Color.white;
            }

            var iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(72f, 72f);
            iconRect.anchoredPosition = new Vector2(230f, 0f);
            raceHorseIcons[horse] = iconRect;

            var finish = CreateText("Finish", lane.transform, "FINISH", 11, FontStyles.Bold, UiGold, TextAlignmentOptions.Center);
            SetAnchors(finish.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-60f, 0f), new Vector2(70f, 0f));
        }

        private void UpdateRaceCanvas()
        {
            if (raceInfoText != null)
            {
                raceInfoText.color = Color.black;
                var title = currentRace != null ? currentRace.GetName(language == UiLanguage.Korean) : "Race Name";
                var subtitle = currentRace != null
                    ? $"{currentRace.League} {currentRace.GetSurfaceName(language == UiLanguage.Korean)}: {currentRace.TotalDistanceMeters}m"
                    : "G1 dirt: 2000m";
                var ticketInfo = BuildActiveRaceTicketInfo();
                raceInfoText.text = $"{title}\n\n{subtitle}\n\n\n{L("round")} {roundNumber} ({GetRaceInRound()}/{RacesPerRound})\n{L("round_goal")}\n{roundEarnedGold:N0}/{roundTargetGold:N0}\n\n{L("gold")}: {gold:N0}\n{L("race_clock")}: {raceClock:0.0}s\n\n{ticketInfo}";
            }

            if (raceClockText != null)
            {
                raceClockText.text = $"{L("race_clock")}  {raceClock:0.0}s";
            }

            if (raceMessageText != null)
            {
                var activeSkills = field
                    .Where(horse => !string.IsNullOrEmpty(horse.SkillMessage))
                    .Select(horse => $"{GetShortHorseName(horse)}: {GetSkillName(horse.Skill)}");
                var message = string.Join("    ", activeSkills);
                raceMessageText.text = IsFinalDuel()
                    ? GetRaceMomentText()
                    : string.IsNullOrEmpty(message) ? GetRaceMomentText() : message;
            }

            if (raceStandingText != null)
            {
                var standings = field
                    .OrderByDescending(horse => horse.Distance)
                    .ThenByDescending(horse => horse.CurrentSpeed)
                    .Take(6)
                    .Select((horse, index) => $"{index + 1}. {GetShortHorseName(horse)}  {Mathf.Max(0f, GetTrackLength() - horse.Distance):0}m");
                raceStandingText.text = string.Join("\n", standings);
            }

            var trackLength = Mathf.Max(1f, GetTrackLength());
            var ordered = field
                .OrderByDescending(horse => horse.Distance)
                .ThenByDescending(horse => horse.CurrentSpeed)
                .ToList();
            var leaderDistance = ordered.Count > 0 ? ordered[0].Distance : 0f;
            var cameraTarget = GetCameraTarget();
            var focusDistance = cameraTarget != null ? cameraTarget.Distance : leaderDistance;
            if (raceTargetText != null)
            {
                raceTargetText.text = cameraTargetLane < 0
                    ? $"Q / E   {L("following")}: {L("leader")}"
                    : $"Q / E   {L("following")}: {GetShortHorseName(cameraTarget)}";
            }

            UpdateRaceRouteMarker(focusDistance, trackLength);
            UpdateRaceProgressDisplay(ordered, trackLength, cameraTarget);
        }

        private string BuildActiveRaceTicketInfo()
        {
            var korean = language == UiLanguage.Korean;
            var header = korean ? "적용 중인 마권 / 현재 예상 적중 확률" : "Active tickets / Current hit chance";
            if (offeredTickets.Count == 0)
            {
                return $"{header}\n{(korean ? "적용 중인 마권 없음" : "No active tickets")}";
            }

            RefreshRaceTicketHitProbabilities();
            var lines = new List<string> { header };
            for (var i = 0; i < offeredTickets.Count; i++)
            {
                var ticket = offeredTickets[i];
                raceTicketHitProbabilities.TryGetValue(ticket, out var probability);
                lines.Add($"{i + 1}. {GetRaceTicketSummary(ticket, korean)}  {probability:P0}");
            }

            return string.Join("\n", lines);
        }

        private string GetRaceTicketSummary(BetTicket ticket, bool korean)
        {
            var typeName = ticket.Type switch
            {
                BetType.Win => korean ? "단승" : "Win",
                BetType.Place => korean ? "연승" : "Place",
                BetType.Quinella => korean ? "복승" : "Quinella",
                BetType.Exacta => korean ? "쌍승" : "Exacta",
                _ => korean ? "마권" : "Ticket"
            };
            var firstName = ticket.First != null ? GetShortHorseName(ticket.First) : "-";
            if (!ticket.NeedsSecondHorse)
            {
                return $"{typeName} {firstName}";
            }

            var secondName = ticket.Second != null ? GetShortHorseName(ticket.Second) : "-";
            return ticket.Type == BetType.Exacta
                ? $"{typeName} {firstName} > {secondName}"
                : $"{typeName} {firstName} + {secondName}";
        }

        private void RefreshRaceTicketHitProbabilities()
        {
            if (phase == GamePhase.Results && latestStandings.Count > 0)
            {
                foreach (var ticket in offeredTickets)
                {
                    raceTicketHitProbabilities[ticket] = ticket.Evaluate(latestStandings) ? 1f : 0f;
                }
                return;
            }

            if (Time.unscaledTime < nextRaceTicketProbabilityRefresh
                && offeredTickets.All(ticket => raceTicketHitProbabilities.ContainsKey(ticket)))
            {
                return;
            }

            nextRaceTicketProbabilityRefresh = Time.unscaledTime + 0.25f;
            raceTicketHitProbabilities.Clear();
            if (field.Count == 0)
            {
                return;
            }

            const int sampleCount = 96;
            var hitCounts = new int[offeredTickets.Count];
            var trackLength = Mathf.Max(1f, GetTrackLength());
            var leaderProgress = field.Max(horse => Mathf.Clamp01(horse.Distance / trackLength));
            var uncertainty = Mathf.Lerp(3.2f, 0.08f, leaderProgress);

            for (var sample = 0; sample < sampleCount; sample++)
            {
                var projectedStandings = field
                    .OrderBy(horse => GetProjectedFinishTime(horse, trackLength)
                        + (horse.Finished ? 0f : GetTicketPredictionNoise(horse, sample) * uncertainty))
                    .ThenBy(horse => horse.Lane)
                    .ToList();

                for (var ticketIndex = 0; ticketIndex < offeredTickets.Count; ticketIndex++)
                {
                    if (offeredTickets[ticketIndex].Evaluate(projectedStandings))
                    {
                        hitCounts[ticketIndex]++;
                    }
                }
            }

            for (var i = 0; i < offeredTickets.Count; i++)
            {
                raceTicketHitProbabilities[offeredTickets[i]] = hitCounts[i] / (float)sampleCount;
            }
        }

        private float GetProjectedFinishTime(Horse horse, float trackLength)
        {
            if (horse.Finished)
            {
                return horse.FinishTime;
            }

            var aptitude = GetSurfaceAptitudeMultiplier(horse);
            var baseSpeed = horse.Speed * aptitude
                + horse.RelicSpeedBonus
                + horse.TemporarySpeed
                + horse.TimedSpeedBonus;
            var liveSpeed = Mathf.Max(1.5f, horse.CurrentSpeed);
            var estimatedSpeed = Mathf.Max(1.5f, Mathf.Lerp(baseSpeed, liveSpeed, 0.68f));
            var remainingDistance = Mathf.Max(0f, trackLength - horse.Distance);
            var statusDelay = Mathf.Max(horse.StunTimer, horse.TimeStopTimer);
            return raceClock + statusDelay + remainingDistance / estimatedSpeed;
        }

        private static float GetTicketPredictionNoise(Horse horse, int sample)
        {
            var seed = (sample + 1) * 12.9898f + (horse.Lane + 1) * 78.233f;
            return Mathf.Repeat(Mathf.Sin(seed) * 43758.5453f, 1f) * 2f - 1f;
        }

        private void BuildRaceProgressMarkers()
        {
            raceProgressHorseMarkers.Clear();
            if (raceProgressMarkers == null)
            {
                return;
            }

            ClearChildren(raceProgressMarkers);
            for (var i = 0; i < field.Count; i++)
            {
                var horse = field[i];
                var marker = CreateImage($"ProgressHorse_{i + 1:00}", raceProgressMarkers, Color.white, false);
                marker.sprite = GetHorsePortraitSprite(horse);
                marker.preserveAspect = true;
                marker.raycastTarget = false;
                var rect = marker.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(42f, 42f);
                raceProgressHorseMarkers[horse] = rect;
            }
        }

        private void UpdateRaceProgressDisplay(
            IReadOnlyList<Horse> ordered,
            float trackLength,
            Horse tracked)
        {
            if (ordered == null || ordered.Count == 0)
            {
                return;
            }

            var leader = ordered[0];
            var leaderProgress = Mathf.Clamp01(leader.Distance / trackLength);
            if (raceProgressFill != null)
            {
                raceProgressFill.anchorMin = Vector2.zero;
                raceProgressFill.anchorMax = new Vector2(leaderProgress, 1f);
                raceProgressFill.offsetMin = Vector2.zero;
                raceProgressFill.offsetMax = Vector2.zero;
            }

            if (raceProgressText != null)
            {
                raceProgressText.text = $"{GetShortHorseName(leader)}  {leaderProgress:P0}";
            }

            var positionOrder = field
                .OrderBy(horse => horse.Distance)
                .ThenBy(horse => horse.Lane)
                .ToList();
            for (var i = 0; i < positionOrder.Count; i++)
            {
                var horse = positionOrder[i];
                if (!raceProgressHorseMarkers.TryGetValue(horse, out var marker))
                {
                    continue;
                }

                var progress = Mathf.Clamp01(horse.Distance / trackLength);
                marker.anchorMin = new Vector2(progress, 0.5f);
                marker.anchorMax = marker.anchorMin;
                marker.anchoredPosition = new Vector2(0f, i % 2 == 0 ? -9f : 9f);
                var scale = horse == tracked ? 1.22f : horse == leader ? 1.1f : 0.92f;
                marker.localScale = Vector3.one * scale;

                var image = marker.GetComponent<Image>();
                if (image != null)
                {
                    image.color = horse == tracked
                        ? new Color(1f, 0.82f, 0.25f, 1f)
                        : Color.white;
                }
                marker.SetSiblingIndex(i);
            }
        }

        private void UpdateHorseRaceIcon(Horse horse, RectTransform icon, int rank, float trackLength, float leaderDistance, float bend)
        {
            var progress = Mathf.Clamp01(horse.Distance / trackLength);
            var currentOffset = Mathf.Lerp(100f, -100f, horse.LaneOffset);

            if (raceCourseRect != null)
            {
                var courseSize = raceCourseRect.rect.size;
                var cameraCenter = 0.5f;
                var normalizedX = cameraCenter + (horse.Distance - leaderDistance) / raceCameraViewDistance;
                normalizedX = Mathf.Clamp(normalizedX, -0.08f, 1.08f);
                var x = (normalizedX - 0.5f) * courseSize.x;
                var curve = (1f - Mathf.Pow(normalizedX * 2f - 1f, 2f)) * bend * courseSize.y * 0.24f;
                var position = new Vector2(x, curve + currentOffset);
                icon.SetParent(raceCourseRect, false);
                icon.anchorMin = new Vector2(0.5f, 0.5f);
                icon.anchorMax = new Vector2(0.5f, 0.5f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                icon.anchoredPosition = position;
            }
            else
            {
                icon.anchorMin = new Vector2(Mathf.Lerp(0.13f, 0.91f, progress), 0.5f);
                icon.anchorMax = icon.anchorMin;
                icon.anchoredPosition = Vector2.zero;
            }

            var cornerScale = 1f - Mathf.Abs(bend) * 0.08f;
            var perspectiveScale = Mathf.Lerp(0.86f, 1.08f, rank / Mathf.Max(1f, field.Count - 1f));
            var scale = cornerScale * perspectiveScale;
            icon.localScale = new Vector3(scale, scale, 1f);
            var slope = -4f * (progress - 0.5f) * bend;
            icon.localEulerAngles = new Vector3(0f, 0f, slope * 9f);
            icon.SetSiblingIndex(field.Count - rank);

            var rankText = FindDeep(icon, "RankText")?.GetComponent<TextMeshProUGUI>();
            if (rankText != null)
            {
                rankText.text = $"{rank + 1}";
            }
        }

        private void UpdateRaceFinishMarker(float trackLength, float leaderDistance, float bend)
        {
            if (raceFinishMarker == null || raceCourseRect == null)
            {
                return;
            }

            const float cameraCenter = 0.5f;
            var normalizedX = cameraCenter + (trackLength - leaderDistance) / raceCameraViewDistance;
            var visible = normalizedX >= -0.04f && normalizedX <= 1.04f;
            raceFinishMarker.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var courseSize = raceCourseRect.rect.size;
            var x = (normalizedX - 0.5f) * courseSize.x;
            var curve = (1f - Mathf.Pow(normalizedX * 2f - 1f, 2f)) * bend * courseSize.y * 0.24f;
            raceFinishMarker.anchorMin = new Vector2(0.5f, 0.5f);
            raceFinishMarker.anchorMax = new Vector2(0.5f, 0.5f);
            raceFinishMarker.pivot = new Vector2(0.5f, 0.5f);
            raceFinishMarker.anchoredPosition = new Vector2(x, curve);
            raceFinishMarker.localEulerAngles = new Vector3(0f, 0f, -4f * (normalizedX - 0.5f) * bend * 9f);
        }

        private void BuildRaceRouteDisplay()
        {
            if (raceRouteBar == null)
            {
                return;
            }

            ClearChildren(raceRouteBar);
            var baseLine = CreateImage("Straight", raceRouteBar, new Color(0.28f, 0.34f, 0.3f, 1f), false).rectTransform;
            Stretch(baseLine);

            if (currentRace?.Corners == null)
            {
                return;
            }

            var totalDistance = Mathf.Max(1f, currentRace.TotalDistanceMeters);
            for (var i = 0; i < currentRace.Corners.Count; i++)
            {
                var corner = currentRace.Corners[i];
                if (corner == null)
                {
                    continue;
                }

                var start = Mathf.Clamp01(corner.StartDistanceMeters / totalDistance);
                var end = Mathf.Clamp01(corner.EndDistanceMeters / totalDistance);
                var color = corner.Direction == RaceTurnDirection.Left
                    ? new Color(0.88f, 0.55f, 0.18f, 1f)
                    : new Color(0.3f, 0.58f, 0.84f, 1f);
                var segment = CreateImage($"Corner {i + 1}", raceRouteBar, color, false).rectTransform;
                segment.anchorMin = new Vector2(start, 0f);
                segment.anchorMax = new Vector2(end, 1f);
                segment.offsetMin = Vector2.zero;
                segment.offsetMax = Vector2.zero;
            }
        }

        private void UpdateRaceRouteMarker(float focusDistance, float trackLength)
        {
            if (raceRouteMarker == null)
            {
                return;
            }

            var progress = Mathf.Clamp01(focusDistance / trackLength);
            raceRouteMarker.anchorMin = new Vector2(progress, 0.5f);
            raceRouteMarker.anchorMax = raceRouteMarker.anchorMin;
            raceRouteMarker.anchoredPosition = Vector2.zero;
        }

        private float GetCurrentCourseBend()
        {
            if (currentRace == null || field.Count == 0)
            {
                return 0f;
            }

            var leaderDistanceMeters = field.Max(horse => horse.Distance) * 8f;
            var corner = currentRace.GetCornerAt(leaderDistanceMeters);
            if (corner == null)
            {
                return 0f;
            }

            var phase = corner.GetPhase(leaderDistanceMeters);
            var ease = Mathf.Sin(phase * Mathf.PI);
            return ease * corner.VisualStrength * (int)corner.Direction;
        }

        private string GetRaceMomentText()
        {
            if (field.Count == 0)
            {
                return string.Empty;
            }

            if (IsFinalDuel())
            {
                var duelists = field
                    .Where(horse => !horse.Finished)
                    .OrderByDescending(horse => horse.Distance)
                    .Take(2)
                    .ToList();
                if (duelists.Count == 2)
                {
                    return language == UiLanguage.Korean
                        ? $"{GetShortHorseName(duelists[0])}  VS  {GetShortHorseName(duelists[1])}    대접전!!"
                        : $"{GetShortHorseName(duelists[0])}  VS  {GetShortHorseName(duelists[1])}    PHOTO FINISH!";
                }
            }

            var leader = field.OrderByDescending(horse => horse.Distance).First();
            var leaderDistanceMeters = leader.Distance * 8f;
            var corner = currentRace != null ? currentRace.GetCornerAt(leaderDistanceMeters) : null;
            if (corner != null)
            {
                var cornerIndex = currentRace.Corners.IndexOf(corner) + 1;
                return $"corner {cornerIndex}";
            }

            var chaser = field
                .Where(horse => horse != leader)
                .OrderBy(horse => leader.Distance - horse.Distance)
                .FirstOrDefault();
            if (chaser != null && leader.Distance - chaser.Distance < 45f)
            {
                return $"{GetShortHorseName(chaser)} chasing {GetShortHorseName(leader)}";
            }

            var remaining = currentRace != null ? currentRace.TotalDistanceMeters - leaderDistanceMeters : 0f;
            return remaining <= 400f ? "last straight" : "straight";
        }

        private void ShowHorseModal(Horse horse)
        {
            ClearChildren(modalRoot);
            CreateModalBackdrop(() => ClearChildren(modalRoot));
            var modal = CreateModalPanel("Horse Detail", 620f, 690f);
            var layout = modal.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 22, 22);
            layout.spacing = 12f;

            var portrait = CreateImage("Horse Image", modal, new Color(0f, 0f, 0f, 0f), false);
            portrait.sprite = GetHorsePortraitSprite(horse);
            portrait.preserveAspect = true;
            AddLayoutElement(portrait.gameObject, -1f, 150f, 0f);
            var title = CreateText("Name", modal, GetHorseName(horse), 28, FontStyles.Bold, UiText, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(title.gameObject, -1f, 48f, 0f);
            var odds = CreateText("Odds", modal, $"{L("odds")}  {horse.WinOdds:0.0}x", 20, FontStyles.Bold, UiGold, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(odds.gameObject, -1f, 34f, 0f);
            var stats = CreateText("Stats", modal, StatLine(horse), 16, FontStyles.Normal, UiText, TextAlignmentOptions.TopLeft);
            stats.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(stats.gameObject, -1f, 84f, 0f);
            var skillName = CreateText("Skill Name", modal, GetSkillName(horse.Skill), 20, FontStyles.Bold, horse.Skill != null ? horse.Skill.EffectColor : UiText, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(skillName.gameObject, -1f, 36f, 0f);
            var description = horse.Skill != null ? horse.Skill.GetDescription(language == UiLanguage.Korean) : string.Empty;
            var skillDescription = CreateText("Skill Description", modal, description, 16, FontStyles.Normal, UiMuted, TextAlignmentOptions.TopLeft);
            skillDescription.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(skillDescription.gameObject, -1f, 130f, 1f);
            var close = CreateButton("Close", modal, "OK", () => ClearChildren(modalRoot), UiGreen, 16);
            AddLayoutElement(close, -1f, 46f, 0f);
        }

        private void ShowRelicModal(RelicData relic)
        {
            if (relic == null || !relicInventory.Contains(relic))
            {
                return;
            }

            ClearChildren(modalRoot);
            CreateModalBackdrop(() => ClearChildren(modalRoot));
            var modal = CreateModalPanel("Relic Detail", 620f, 620f);
            var layout = modal.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 26, 26);
            layout.spacing = 12f;

            var icon = CreateImage("Relic Icon", modal, Tint(relic.Color, 0.22f), false);
            icon.sprite = relic.Icon;
            icon.preserveAspect = true;
            AddLayoutElement(icon.gameObject, -1f, 150f, 0f);

            var rarity = CreateText(
                "Rarity",
                modal,
                GetRarityName(relic.Rarity).ToUpperInvariant(),
                16,
                FontStyles.Bold,
                relic.Color,
                TextAlignmentOptions.Center);
            AddLayoutElement(rarity.gameObject, -1f, 28f, 0f);

            var title = CreateText(
                "Name",
                modal,
                relic.GetName(language == UiLanguage.Korean),
                28,
                FontStyles.Bold,
                UiText,
                TextAlignmentOptions.Center);
            AddLayoutElement(title.gameObject, -1f, 48f, 0f);

            var description = CreateText(
                "Description",
                modal,
                relic.GetDescription(language == UiLanguage.Korean),
                17,
                FontStyles.Normal,
                UiMuted,
                TextAlignmentOptions.TopLeft);
            description.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(description.gameObject, -1f, 120f, 1f);

            var prices = CreateText(
                "Prices",
                modal,
                $"{L("buy")} {relic.Price:N0}    {L("sell")} +{relic.SellPrice:N0}",
                18,
                FontStyles.Bold,
                UiGold,
                TextAlignmentOptions.Center);
            AddLayoutElement(prices.gameObject, -1f, 34f, 0f);

            var actions = CreateHorizontalLayout("Actions", modal, 12f, new RectOffset(0, 0, 0, 0));
            AddLayoutElement(actions.gameObject, -1f, 52f, 0f);
            var close = CreateButton(
                "Close",
                actions,
                language == UiLanguage.Korean ? "닫기" : "Close",
                () => ClearChildren(modalRoot),
                UiSurfaceRaised,
                17);
            AddLayoutElement(close, -1f, 50f, 1f);

            var sell = CreateButton(
                "Sell",
                actions,
                $"{L("sell")} +{relic.SellPrice:N0}",
                () =>
                {
                    if (!relicInventory.Contains(relic))
                    {
                        ClearChildren(modalRoot);
                        return;
                    }

                    SellRelic(relic);
                    ClearChildren(modalRoot);
                    MarkUiDirty();
                },
                UiRed,
                17);
            AddLayoutElement(sell, -1f, 50f, 1f);
        }

        private void ShowTicketTypeModal(BetTicket ticket)
        {
            ClearChildren(modalRoot);
            CreateModalBackdrop(() => ClearChildren(modalRoot));
            var modal = CreateModalPanel("Ticket Type", 520f, 390f);
            var layout = modal.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 22, 22);
            layout.spacing = 10f;
            var title = CreateText("Title", modal, L("select_ticket_type"), 25, FontStyles.Bold, UiText, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(title.gameObject, -1f, 48f, 0f);

            foreach (var type in (BetType[])Enum.GetValues(typeof(BetType)))
            {
                var capturedType = type;
                var button = CreateButton(type.ToString(), modal, GetBetTypeName(type), () =>
                {
                    ticket.SetType(capturedType);
                    if (ticket.NeedsSecondHorse && ticket.Second == null)
                    {
                        ticket.SetSecond(field.FirstOrDefault(horse => horse != ticket.First));
                    }
                    SetLog("customize_all");
                    ClearChildren(modalRoot);
                    MarkUiDirty();
                }, ticket.Type == type ? UiGreen : UiSurfaceRaised, 17);
                AddLayoutElement(button, -1f, 54f, 0f);
            }
        }

        private void ShowTicketHorseModal(BetTicket ticket, bool secondTarget)
        {
            ClearChildren(modalRoot);
            CreateModalBackdrop(() => ClearChildren(modalRoot));
            var modal = CreateModalPanel("Horse Selection", 560f, 560f);
            var layout = modal.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 22, 22);
            layout.spacing = 9f;
            var title = CreateText("Title", modal, L("select_horse"), 25, FontStyles.Bold, UiText, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(title.gameObject, -1f, 48f, 0f);

            foreach (var horse in field)
            {
                var capturedHorse = horse;
                var disabled = secondTarget ? horse == ticket.First : ticket.NeedsSecondHorse && horse == ticket.Second;
                var selected = secondTarget ? horse == ticket.Second : horse == ticket.First;
                var button = CreateButton($"Horse {horse.Lane}", modal, $"{GetHorseName(horse)}    {horse.WinOdds:0.0}x", () =>
                {
                    if (secondTarget)
                    {
                        ticket.SetSecond(capturedHorse);
                    }
                    else
                    {
                        ticket.SetFirst(capturedHorse);
                    }
                    SetLog("customize_all");
                    ClearChildren(modalRoot);
                    MarkUiDirty();
                }, selected ? UiGreen : Tint(horse.Color, 0.22f), 16);
                button.GetComponent<Button>().interactable = !disabled;
                AddLayoutElement(button, -1f, 54f, 0f);
            }
        }

        private void CreateModalBackdrop(Action closeAction)
        {
            var backdrop = CreateImage("Backdrop", modalRoot, new Color(0f, 0f, 0f, 0.72f), true);
            Stretch(backdrop.rectTransform);
            var button = backdrop.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => closeAction());
        }

        private RectTransform CreateModalPanel(string name, float width, float height)
        {
            var modal = CreateImage(name, modalRoot, UiSurface, true).rectTransform;
            modal.anchorMin = new Vector2(0.5f, 0.5f);
            modal.anchorMax = new Vector2(0.5f, 0.5f);
            modal.pivot = new Vector2(0.5f, 0.5f);
            modal.sizeDelta = new Vector2(width, height);
            modal.anchoredPosition = Vector2.zero;
            modal.SetAsLastSibling();
            return modal;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            ApplyUiFont(label);
            label.text = text;
            label.fontSize = size;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(10f, size * 0.65f);
            label.enableAutoSizing = true;
            label.fontStyle = FontStyles.Normal;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Truncate;
            return label;
        }

        private GameObject CreateButton(string name, Transform parent, string label, Action action, Color color, int fontSize)
        {
            var image = CreateImage(name, parent, color, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.7f);
            button.colors = colors;
            button.onClick.AddListener(() => action());

            var text = CreateText("Label", image.transform, label, fontSize, FontStyles.Bold, UiText, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.margin = new Vector4(8f, 4f, 8f, 4f);
            return image.gameObject;
        }

        private GameObject CreateWhiteButton(string name, Transform parent, string label, Action action, float labelHeight, int fontSize)
        {
            var buttonObject = CreateButton(name, parent, label, action, CardPaper, fontSize);
            var labelText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.color = Ink;
                labelText.fontStyle = FontStyles.Normal;
                labelText.textWrappingMode = TextWrappingModes.Normal;
                labelText.overflowMode = TextOverflowModes.Ellipsis;
                labelText.margin = new Vector4(10f, 6f, 10f, 6f);
            }

            DecorateCard(buttonObject, CardPaperMuted, true);

            return buttonObject;
        }

        private GameObject CreateBoardMiniButton(string name, Transform parent, string label, Action action)
        {
            var buttonObject = CreateButton(name, parent, label, action, new Color(0.9f, 0.88f, 0.8f, 1f), 16);
            var labelText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.color = Ink;
                labelText.fontStyle = FontStyles.Normal;
            }

            DecorateCard(buttonObject, UiGold, false);

            return buttonObject;
        }

        private static void AddBoardBands(Transform parent)
        {
            var leftBand = CreateImage("Left Shade", parent, BoardBlueDark, false).rectTransform;
            SetFixed(leftBand, 0f, 0f, 64f, 1080f);

            var bottomBand = CreateImage("Bottom Shade", parent, new Color(0.09f, 0.18f, 0.3f, 1f), false).rectTransform;
            SetFixed(bottomBand, 0f, 1038f, 1920f, 42f);

            var topLine = CreateImage("Top Line", parent, new Color(0.98f, 0.78f, 0.34f, 0.38f), false).rectTransform;
            SetFixed(topLine, 0f, 0f, 1920f, 3f);
        }

        private static void DecorateCard(GameObject target, Color accent, bool shadow)
        {
            if (shadow)
            {
                var cardShadow = target.GetComponent<Shadow>() ?? target.AddComponent<Shadow>();
                cardShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
                cardShadow.effectDistance = new Vector2(7f, -7f);
                cardShadow.useGraphicAlpha = true;
            }

            var outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = new Color(
                Mathf.Lerp(0.13f, accent.r, 0.35f),
                Mathf.Lerp(0.12f, accent.g, 0.35f),
                Mathf.Lerp(0.1f, accent.b, 0.35f),
                0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        private static void AddAccentBar(Transform parent, Color color, float height)
        {
            var bar = CreateImage("Accent", parent, color, false).rectTransform;
            SetFixed(bar, 0f, 0f, 10000f, height);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.offsetMin = new Vector2(0f, -height);
            bar.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateHorizontalScrollView(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            float spacing,
            float paddingLeft,
            float paddingRight)
        {
            var root = CreateRect(name, parent);
            SetFixed(root, x, y, width, height);

            var rootImage = root.gameObject.AddComponent<Image>();
            rootImage.color = new Color(0.04f, 0.08f, 0.14f, 0.16f);
            rootImage.raycastTarget = true;
            var rootOutline = root.gameObject.AddComponent<Outline>();
            rootOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
            rootOutline.effectDistance = new Vector2(1f, -1f);

            var scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 42f;

            var viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(3600f, 0f);

            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(Mathf.RoundToInt(paddingLeft), Mathf.RoundToInt(paddingRight), 0, 0);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        private static RectTransform CreateVerticalLayout(string name, Transform parent, float spacing, RectOffset padding)
        {
            var rect = CreateRect(name, parent);
            Stretch(rect);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        private static RectTransform CreateHorizontalLayout(string name, Transform parent, float spacing, RectOffset padding)
        {
            var rect = CreateRect(name, parent);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return rect;
        }

        private static void AddLayoutElement(GameObject gameObject, float preferredWidth, float preferredHeight, float flexibleWidth)
        {
            var element = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f)
            {
                element.preferredWidth = preferredWidth;
            }
            if (preferredHeight >= 0f)
            {
                element.preferredHeight = preferredHeight;
            }
            element.flexibleWidth = flexibleWidth;
        }

        private static void CreateFlexibleSpacer(Transform parent)
        {
            var spacer = CreateRect("Spacer", parent);
            var element = spacer.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void SetFixed(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Vector2 GetGridPosition(int index, float startX, float startY, int columns, float itemWidth, float itemHeight, float gapX, float gapY)
        {
            columns = Mathf.Max(1, columns);
            var column = index % columns;
            var row = index / columns;
            return new Vector2(
                startX + column * (itemWidth + gapX),
                startY + row * (itemHeight + gapY));
        }

        private static void SetButtonFontSize(GameObject buttonObject, int fontSize)
        {
            var label = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = fontSize;
            }
        }

        private static Transform FindDirect(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == name)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindDeep(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        private void SetEditableText(Transform root, string objectName, string value)
        {
            var target = FindDeep(root, objectName);
            if (target == null)
            {
                return;
            }

            var label = target.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = target.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (label == null)
            {
                return;
            }

            ApplyUiFont(label);
            label.fontStyle = FontStyles.Normal;
            label.text = value;
        }

        private void ApplyUiFontToHierarchy(Transform root)
        {
            if (root == null || uiFontAsset == null)
            {
                return;
            }

            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                ApplyUiFont(label);
            }
        }

        private void ApplyUiFont(TextMeshProUGUI label)
        {
            if (label == null || uiFontAsset == null)
            {
                return;
            }

            label.font = uiFontAsset;
            if (uiFontAsset.material != null)
            {
                label.fontSharedMaterial = uiFontAsset.material;
            }

            label.fontStyle = FontStyles.Normal;
            label.enabled = true;
            label.SetAllDirty();
        }

        private void BindEditableButton(Transform root, string buttonName, string label, UnityAction action)
        {
            var target = string.IsNullOrEmpty(buttonName) ? root : FindDeep(root, buttonName);
            if (target == null)
            {
                return;
            }

            var button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            var image = target.GetComponent<Image>();
            if (image != null)
            {
                button.targetGraphic = image;
            }

            if (label == null)
            {
                return;
            }

            var labelText = target.GetComponentInChildren<TextMeshProUGUI>(true);
            if (labelText != null)
            {
                if (uiFontAsset != null)
                {
                    ApplyUiFont(labelText);
                }

                labelText.fontStyle = FontStyles.Normal;
                labelText.text = label;
            }
        }

        private void SetEditableShopOfferOwnedState(Transform slot, bool owned, Color relicColor)
        {
            if (slot == null)
            {
                return;
            }

            var cardImage = slot.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = owned ? Tint(relicColor, 0.32f) : CardPaper;
            }

            var buyTransform = FindDeep(slot, "BuyButton");
            if (buyTransform == null)
            {
                return;
            }

            var buyButton = buyTransform.GetComponent<Button>();
            if (buyButton != null)
            {
                buyButton.interactable = !owned;
            }

            var buyImage = buyTransform.GetComponent<Image>();
            if (buyImage != null)
            {
                buyImage.color = owned ? UiSurfaceRaised : UiGreen;
            }

            var title = FindDeep(slot, "TitleText")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.color = owned ? UiMuted : Ink;
            }
        }

        private void SetHorseImage(Transform parent, Horse horse, string objectName, float x, float y, float width, float height)
        {
            var imageTransform = FindDirect(parent, objectName) as RectTransform;
            Image image;
            if (imageTransform == null)
            {
                image = CreateImage(objectName, parent, new Color(0f, 0f, 0f, 0f), false);
                imageTransform = image.rectTransform;
                SetFixed(imageTransform, x, y, width, height);
            }
            else
            {
                image = imageTransform.GetComponent<Image>() ?? imageTransform.gameObject.AddComponent<Image>();
            }

            image.sprite = GetHorsePortraitSprite(horse);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static Sprite GetHorsePortraitSprite(Horse horse)
        {
            if (horse?.RunFrames != null && horse.RunFrames.Length > 0)
            {
                return horse.RunFrames[0];
            }

            var texture = horse?.Data != null ? horse.Data.RunSheet : horse?.RunSheet;
            if (texture == null)
            {
                return null;
            }

            return CreateHorseFrameSprite(
                texture,
                false,
                Mathf.Max(texture.width, texture.height));
        }

        private static void FitEditableScrollContent(Transform root, string scrollName)
        {
            var content = GetEditableScrollContent(root, scrollName);
            FitHorizontalContent(content);
        }

        private static RectTransform GetEditableScrollContent(Transform root, string scrollName)
        {
            var scrollRoot = FindDeep(root, scrollName);
            var viewport = FindDirect(scrollRoot, "Viewport");
            return viewport != null
                ? FindDirect(viewport, "Content") as RectTransform
                : null;
        }

        private static Transform InstantiateEditablePrefab(
            GameObject prefab,
            RectTransform content,
            string instanceName)
        {
            if (prefab == null)
            {
                Debug.LogError($"UI prefab for {instanceName} was not found in Resources/UI.");
                return null;
            }

            var instance = Instantiate(prefab, content, false);
            instance.name = instanceName;
            instance.SetActive(true);
            return instance.transform;
        }

        private static void ClearSpawnedUi(RectTransform content)
        {
            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private static void FitHorizontalContent(RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            var layout = content.GetComponent<HorizontalLayoutGroup>();
            var spacing = layout != null ? layout.spacing : 0f;
            var paddingLeft = layout != null ? layout.padding.left : 0;
            var paddingRight = layout != null ? layout.padding.right : 0;
            var activeCount = 0;
            var totalWidth = (float)(paddingLeft + paddingRight);

            for (var i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                var layoutElement = child.GetComponent<LayoutElement>();
                var childWidth = layoutElement != null && layoutElement.preferredWidth > 0f
                    ? layoutElement.preferredWidth
                    : child.sizeDelta.x;
                totalWidth += Mathf.Max(0f, childWidth);
                activeCount++;
            }

            if (activeCount > 1)
            {
                totalWidth += spacing * (activeCount - 1);
            }

            var viewport = content.parent as RectTransform;
            var viewportWidth = viewport != null ? viewport.rect.width : 0f;
            content.sizeDelta = new Vector2(Mathf.Max(viewportWidth, totalWidth), content.sizeDelta.y);

            var scroll = viewport != null ? viewport.GetComponentInParent<ScrollRect>() : null;
            if (scroll != null)
            {
                scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition);
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        private static Color Tint(Color color, float strength)
        {
            return new Color(
                Mathf.Lerp(UiSurface.r, color.r, strength),
                Mathf.Lerp(UiSurface.g, color.g, strength),
                Mathf.Lerp(UiSurface.b, color.b, strength),
                1f);
        }
    }
}

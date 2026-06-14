using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Malatro
{
    public sealed partial class MalatroPrototype
    {
        private static readonly Color UiBackground = new(0.035f, 0.045f, 0.05f, 1f);
        private static readonly Color UiSurface = new(0.075f, 0.09f, 0.095f, 1f);
        private static readonly Color UiSurfaceRaised = new(0.11f, 0.125f, 0.13f, 1f);
        private static readonly Color UiBorder = new(0.24f, 0.28f, 0.27f, 1f);
        private static readonly Color UiText = new(0.93f, 0.94f, 0.9f, 1f);
        private static readonly Color UiMuted = new(0.64f, 0.68f, 0.65f, 1f);
        private static readonly Color UiGold = new(0.96f, 0.73f, 0.24f, 1f);
        private static readonly Color UiGreen = new(0.22f, 0.62f, 0.42f, 1f);
        private static readonly Color UiRed = new(0.72f, 0.28f, 0.25f, 1f);
        private static readonly Color BoardBlue = new(0.13f, 0.25f, 0.41f, 1f);
        private static readonly Color BoardBlueDark = new(0.08f, 0.16f, 0.27f, 1f);
        private static readonly Color CardPaper = new(0.96f, 0.95f, 0.9f, 1f);
        private static readonly Color CardPaperMuted = new(0.88f, 0.87f, 0.81f, 1f);
        private static readonly Color Ink = new(0.09f, 0.085f, 0.075f, 1f);

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
        private GameObject horseInfoPopupPrefab;
        private GameObject relicInfoPopupPrefab;
        private readonly Dictionary<Horse, RectTransform> raceHorseIcons = new();
        private readonly Dictionary<Horse, RectTransform> raceProgressHorseMarkers = new();
        private readonly Dictionary<BetTicket, float> raceTicketHitProbabilities = new();
        private float nextRaceTicketProbabilityRefresh;
        private GamePhase renderedPhase = (GamePhase)(-1);
        private bool uiDirty;
        private bool usingEditableSceneUi;
        private readonly MalatroUiFactory uiFactory = new();
        private static MalatroUiFactory activeUiFactory;

        private PredictionScreenController predictionScreenController;
        private ShopScreenController shopScreenController;
        private RaceScreenController raceScreenController;
        private ResultsScreenController resultsScreenController;
    }
}

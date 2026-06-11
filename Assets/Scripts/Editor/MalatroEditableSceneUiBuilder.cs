using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Malatro.EditorTools
{
    [InitializeOnLoad]
    public static class MalatroEditableSceneUiBuilder
    {
        private const string BuildQueuePath = "Temp/MalatroEditableSceneUiBuilder.queue";
        private const string HorseImageQueuePath = "Temp/MalatroEditableHorseImages.queue";
        private const string RaceScreenQueuePath = "Temp/MalatroEditableRaceScreen.queue";
        private const string KoreanFontQueuePath = "Temp/MalatroApplyKoreanFont.queue";
        private const string HorseRaceIconPrefabPath = "Assets/Resources/UI/HorseRaceIcon.prefab";
        private const string HorseInfoCardPrefabPath = "Assets/Resources/UI/HorseInfoCard.prefab";
        private const string BetTicketCardPrefabPath = "Assets/Resources/UI/BetTicketCard.prefab";
        private const string RelicSlotPrefabPath = "Assets/Resources/UI/RelicSlot.prefab";
        private const string RelicShopOfferPrefabPath = "Assets/Resources/UI/RelicShopOfferCard.prefab";
        private const string KoreanFontPath = "Assets/Resources/Fonts/NanumGothic-Bold.ttf";
        private const string KoreanFontAssetPath = "Assets/Resources/Fonts/NanumGothic Bold SDF.asset";
        private static TMP_FontAsset koreanFontAsset;

        private static readonly Color BoardBlue = new(0.13f, 0.25f, 0.41f, 1f);
        private static readonly Color BoardBlueDark = new(0.08f, 0.16f, 0.27f, 1f);
        private static readonly Color Paper = new(0.96f, 0.95f, 0.9f, 1f);
        private static readonly Color PaperMuted = new(0.88f, 0.87f, 0.81f, 1f);
        private static readonly Color Ink = new(0.09f, 0.085f, 0.075f, 1f);
        private static readonly Color Gold = new(0.96f, 0.73f, 0.24f, 1f);
        private static readonly Color Green = new(0.22f, 0.62f, 0.42f, 1f);

        static MalatroEditableSceneUiBuilder()
        {
            EditorApplication.delayCall += RebuildQueuedEditableSceneUi;
            EditorApplication.delayCall += AddQueuedHorseImages;
            EditorApplication.delayCall += RebuildQueuedRaceScreen;
            EditorApplication.delayCall += ApplyQueuedKoreanFont;
        }

        private static void RebuildQueuedEditableSceneUi()
        {
            if (!File.Exists(BuildQueuePath))
            {
                return;
            }

            File.Delete(BuildQueuePath);
            RebuildEditableSceneUi();
            Debug.Log("Malatro editable scene UI was rebuilt from the queued request.");
        }

        private static void AddQueuedHorseImages()
        {
            if (!File.Exists(HorseImageQueuePath))
            {
                return;
            }

            File.Delete(HorseImageQueuePath);
            AddHorseImagesToExistingUi();
            Debug.Log("Horse images were added to the existing Malatro editable UI.");
        }

        private static void RebuildQueuedRaceScreen()
        {
            if (!File.Exists(RaceScreenQueuePath))
            {
                return;
            }

            File.Delete(RaceScreenQueuePath);
            RebuildEditableRaceScreen();
            Debug.Log("The editable race screen was rebuilt as an oval race course.");
        }

        private static void ApplyQueuedKoreanFont()
        {
            if (!File.Exists(KoreanFontQueuePath))
            {
                return;
            }

            File.Delete(KoreanFontQueuePath);
            ApplyKoreanFontToExistingUi();
            Debug.Log("Nanum Gothic Bold was applied to the existing editable UI.");
        }

        [MenuItem("Malatro/UI/Apply Korean Font To Existing UI")]
        public static void ApplyKoreanFontToExistingUi()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            ApplyKoreanFontToOpenEditableUi(true);
        }

        private static void ApplyKoreanFontToOpenEditableUi(bool logMissing = false)
        {
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity")
            {
                return;
            }

            EnsureKoreanFontAsset();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var editableRoot = canvas != null ? canvas.transform.Find("Malatro Editable UI") : null;
            if (editableRoot == null || koreanFontAsset == null)
            {
                if (logMissing)
                {
                    Debug.LogError("Malatro Editable UI or Nanum Gothic Bold font asset was not found.");
                }
                return;
            }

            foreach (var label in editableRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                label.font = koreanFontAsset;
                if (koreanFontAsset.material != null)
                {
                    label.fontSharedMaterial = koreanFontAsset.material;
                }
                label.fontStyle = FontStyles.Normal;
                if (label.name == "RaceInfoText")
                {
                    label.color = Color.black;
                }

                EditorUtility.SetDirty(label);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Malatro/UI/Rebuild Editable Scene UI")]
        public static void RebuildEditableSceneUi()
        {
            EnsureKoreanFontAsset();
            EnsureHorseRaceIconPrefab();
            EnsureReusableUiPrefabs();
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            var oldRoot = canvas.transform.Find("Malatro Editable UI");
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot.gameObject);
            }

            var root = Rect("Malatro Editable UI", canvas.transform);
            Stretch(root);

            BuildBettingScreen(root);
            BuildShopScreen(root);
            BuildDynamicRaceScreen(root);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Malatro/UI/Add Horse Images To Existing UI")]
        public static void AddHorseImagesToExistingUi()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var editableRoot = canvas != null ? canvas.transform.Find("Malatro Editable UI") : null;
            var bettingScreen = editableRoot != null ? editableRoot.Find("BettingScreen") : null;
            if (bettingScreen == null)
            {
                Debug.LogError("Malatro Editable UI/BettingScreen was not found.");
                return;
            }

            var spritePaths = new[]
            {
                "Assets/Resources/Horses/leaf-run.png",
                "Assets/Resources/Horses/purple-run.png",
                "Assets/Resources/Horses/ram-run.png",
                "Assets/Resources/Horses/witch-run.png",
                "Assets/Resources/Horses/blue-cat-run.png",
                "Assets/Resources/Horses/blond-cat-run.png"
            };

            for (var i = 0; i < spritePaths.Length; i++)
            {
                var slot = FindDeep(bettingScreen, $"HorseSlot_{i + 1:00}");
                if (slot == null)
                {
                    continue;
                }

                var imageTransform = slot.Find("HorseImage") as RectTransform;
                Image image;
                if (imageTransform == null)
                {
                    image = Image("HorseImage", slot, Color.white, false, 35f, 52f, 130f, 84f);
                    imageTransform = image.rectTransform;
                }
                else
                {
                    image = imageTransform.GetComponent<Image>() ?? imageTransform.gameObject.AddComponent<Image>();
                }

                image.sprite = AssetDatabase.LoadAllAssetsAtPath(spritePaths[i]).OfType<Sprite>().FirstOrDefault();
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;

                var title = slot.Find("TitleText") as RectTransform;
                if (title != null)
                {
                    Fixed(title, 16f, 18f, 168f, 34f);
                }

                var odds = slot.Find("OddsText") as RectTransform;
                if (odds != null)
                {
                    Fixed(odds, 16f, 150f, 168f, 32f);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Malatro/UI/Rebuild Editable Race Screen")]
        public static void RebuildEditableRaceScreen()
        {
            EnsureKoreanFontAsset();
            EnsureHorseRaceIconPrefab();
            EnsureReusableUiPrefabs();
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var editableRoot = canvas != null ? canvas.transform.Find("Malatro Editable UI") : null;
            if (editableRoot == null)
            {
                Debug.LogError("Malatro Editable UI was not found.");
                return;
            }

            var oldRaceScreen = editableRoot.Find("RaceScreen");
            if (oldRaceScreen != null)
            {
                Object.DestroyImmediate(oldRaceScreen.gameObject);
            }

            BuildDynamicRaceScreen(editableRoot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Malatro/UI/Remove Prediction Shop Button")]
        public static void RemovePredictionShopButton()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var editableRoot = canvas != null ? canvas.transform.Find("Malatro Editable UI") : null;
            var bettingScreen = editableRoot != null ? editableRoot.Find("BettingScreen") : null;
            var shopButton = bettingScreen != null ? FindDeep(bettingScreen, "ShopButton") : null;
            if (shopButton != null)
            {
                Object.DestroyImmediate(shopButton.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Malatro/UI/Add Prediction Shop Button")]
        public static void AddPredictionShopButton()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var editableRoot = canvas != null ? canvas.transform.Find("Malatro Editable UI") : null;
            var bettingScreen = editableRoot != null ? editableRoot.Find("BettingScreen") : null;
            if (bettingScreen == null)
            {
                Debug.LogError("Malatro Editable UI/BettingScreen was not found.");
                return;
            }

            var oldButton = FindDeep(bettingScreen, "ShopButton");
            if (oldButton != null)
            {
                Object.DestroyImmediate(oldButton.gameObject);
            }

            Button("ShopButton", bettingScreen, "상점", 1662f, 16f, 200f, 150f, Gold);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static void BuildBettingScreen(Transform root)
        {
            var screen = Rect("BettingScreen", root);
            Stretch(screen);
            Image("Background", screen, BoardBlue, false, stretch: true);
            BoardBands(screen);
            RaceInfo(screen, false);

            var relics = Scroll("OwnedRelicsScroll", screen, 686f, 26f, 820f, 132f, 22f);
            FitContent(relics);

            var horses = Scroll("HorseScroll", screen, 620f, 300f, 910f, 220f, 50f);
            FitContent(horses);

            var tickets = Scroll("BetTicketScroll", screen, 590f, 660f, 1040f, 420f, 50f);
            FitContent(tickets);

            Button("ShopButton", screen, "상점", 1662f, 16f, 200f, 150f, Gold);
            Button("RaceStartButton", screen, "경주 시작", 1662f, 815f, 200f, 150f, Green);
        }

        private static void BuildShopScreen(Transform root)
        {
            var screen = Rect("ShopScreen", root);
            Stretch(screen);
            screen.gameObject.SetActive(false);
            Image("Background", screen, BoardBlue, false, stretch: true);
            BoardBands(screen);
            RaceInfo(screen, true);

            var relics = Scroll("OwnedRelicsScroll", screen, 686f, 26f, 820f, 132f, 22f);
            FitContent(relics);

            var horses = Scroll("HorseScroll", screen, 620f, 300f, 910f, 220f, 50f);
            FitContent(horses);

            var offers = Scroll("ShopOfferScroll", screen, 600f, 670f, 1010f, 410f, 45f);
            FitContent(offers);

            Button("BackToBettingButton", screen, "마권", 1642f, 12f, 200f, 150f, Gold);
            Button("RefreshShopButton", screen, "새로고침", 1642f, 804f, 200f, 150f, Gold);
        }

        private static void BuildRaceScreen(Transform root)
        {
            var screen = Rect("RaceScreen", root);
            Stretch(screen);
            screen.gameObject.SetActive(false);
            Image("Background", screen, new Color(0.07f, 0.11f, 0.09f, 1f), false, stretch: true);

            var track = Image("TrackPanel", screen, new Color(0.1f, 0.2f, 0.14f, 1f), false, 80f, 140f, 1420f, 780f);
            Outline(track.gameObject, Gold);
            for (var i = 0; i < 6; i++)
            {
                var lane = Image($"Lane_{i + 1:00}", track.transform, i % 2 == 0 ? new Color(0.13f, 0.25f, 0.16f, 1f) : new Color(0.09f, 0.2f, 0.14f, 1f), false, 24f, 28f + i * 122f, 1372f, 96f);
                Label("HorseName", lane.transform, $"말 {i + 1}", 22, FontStyles.Bold, Paper, 28f, 18f, 120f, 36f);
                Image("HorseIcon", lane.transform, HorseColor(i), false, 170f, 20f, 64f, 64f);
                Image("FinishLine", lane.transform, Paper, false, 1280f, 0f, 8f, 96f);
            }

            var side = Image("RaceSidePanel", screen, Paper, false, 1540f, 140f, 280f, 780f);
            Outline(side.gameObject, Gold);
            Label("RaceClock", side.transform, "00:00", 34, FontStyles.Bold, Ink, 24f, 32f, 232f, 56f);
            Label("RaceMessage", side.transform, "경주 진행", 22, FontStyles.Normal, Ink, 24f, 106f, 232f, 160f);
            Button("ResultButton", side.transform, "결과", 40f, 660f, 200f, 64f, Green);
        }

        private static void BuildOvalRaceScreen(Transform root)
        {
            var screen = Rect("RaceScreen", root);
            Stretch(screen);
            screen.gameObject.SetActive(false);
            Image("Background", screen, new Color(0.035f, 0.11f, 0.075f, 1f), false, stretch: true);
            Image("GrandstandShade", screen, new Color(0.03f, 0.055f, 0.07f, 0.94f), false, 0f, 0f, 1920f, 150f);
            Label("RaceTitle", screen, "G1 RACE", 34, FontStyles.Bold, Paper, 72f, 38f, 720f, 54f, TextAlignmentOptions.MidlineLeft);
            Label("RaceSubtitle", screen, "2000m  FINAL", 18, FontStyles.Normal, Gold, 74f, 94f, 520f, 32f, TextAlignmentOptions.MidlineLeft);

            var course = Rect("RaceCourse", screen);
            Fixed(course, 56f, 174f, 1460f, 790f);
            var track = course.gameObject.AddComponent<OvalRaceTrackGraphic>();
            track.raycastTarget = false;
            Outline(course.gameObject, new Color(0.9f, 0.78f, 0.38f, 0.65f));

            for (var i = 0; i < 6; i++)
            {
                var icon = Image($"HorseIcon_{i + 1:00}", course, Color.white, false, 650f + i * 12f, 570f + i * 9f, 112f, 82f);
                icon.preserveAspect = true;
                var rank = Image("RankBadge", icon.transform, HorseColor(i), false, -8f, -8f, 34f, 34f);
                Label("RankText", rank.transform, $"{i + 1}", 18, FontStyles.Bold, Color.white, 0f, 0f, 34f, 34f);
                var namePlate = Image("NamePlate", icon.transform, new Color(0.02f, 0.03f, 0.025f, 0.78f), false, 4f, 64f, 104f, 24f);
                Label("HorseName", namePlate.transform, $"Horse {i + 1}", 13, FontStyles.Bold, Paper, 2f, 0f, 100f, 24f);
            }

            var side = Image("RaceSidePanel", screen, Paper, false, 1540f, 174f, 320f, 790f);
            Outline(side.gameObject, Gold);
            Accent(side.transform, Gold, 8f);
            Label("RaceClock", side.transform, "00:00", 36, FontStyles.Bold, Ink, 24f, 34f, 272f, 58f);
            Label("RaceMessage", side.transform, "first corner", 19, FontStyles.Bold, new Color(0.55f, 0.2f, 0.12f, 1f), 24f, 104f, 272f, 62f);
            Label("StandingTitle", side.transform, "LIVE ORDER", 16, FontStyles.Bold, Ink, 24f, 198f, 272f, 30f, TextAlignmentOptions.MidlineLeft);
            Label("RaceStandings", side.transform, "1. Horse 1\n2. Horse 2\n3. Horse 3\n4. Horse 4\n5. Horse 5\n6. Horse 6", 19, FontStyles.Normal, Ink, 24f, 240f, 272f, 310f, TextAlignmentOptions.TopLeft);
            Label("CornerGuide", side.transform, "1 CORNER\nBACK STRETCH\nFINAL CORNER\nLAST STRAIGHT", 14, FontStyles.Normal, new Color(0.35f, 0.38f, 0.34f, 1f), 24f, 560f, 272f, 100f, TextAlignmentOptions.TopLeft);
            Button("ResultButton", side.transform, "RESULT", 50f, 690f, 220f, 60f, Green);
        }

        private static void BuildDynamicRaceScreen(Transform root)
        {
            var screen = Rect("RaceScreen", root);
            Stretch(screen);
            screen.gameObject.SetActive(false);
            Image("Background", screen, Color.clear, false, stretch: true);
            Image("GrandstandShade", screen, new Color(0.03f, 0.052f, 0.065f, 0.96f), false, 0f, 0f, 1920f, 132f);
            Label("RaceTitle", screen, "G1 RACE", 34, FontStyles.Bold, Paper, 72f, 34f, 720f, 52f, TextAlignmentOptions.MidlineLeft);
            Label("RaceSubtitle", screen, "Straight course / DB corner points", 18, FontStyles.Normal, Gold, 74f, 88f, 660f, 32f, TextAlignmentOptions.MidlineLeft);

            var progressPanel = Image("RaceProgressPanel", screen, new Color(0.02f, 0.035f, 0.03f, 0.9f), false, 760f, 28f, 700f, 88f);
            Outline(progressPanel.gameObject, new Color(0.7f, 0.62f, 0.35f, 0.65f));
            var progressBar = Image("RaceProgressBar", progressPanel.transform, new Color(0.15f, 0.17f, 0.16f, 1f), false, 42f, 42f, 616f, 10f);
            var progressFill = Image("RaceProgressFill", progressBar.transform, Gold, false, stretch: true).rectTransform;
            progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
            var markers = Rect("RaceProgressMarkers", progressPanel.transform);
            Fixed(markers, 42f, 16f, 616f, 58f);
            Label("ProgressStart", progressPanel.transform, "0%", 13, FontStyles.Bold, PaperMuted, 4f, 32f, 36f, 28f);
            Label("ProgressFinish", progressPanel.transform, "100%", 13, FontStyles.Bold, Gold, 660f, 32f, 38f, 28f);
            Label("RaceProgressText", progressPanel.transform, "Leader 0%", 13, FontStyles.Bold, Paper, 250f, 62f, 200f, 22f);

            var course = Rect("RaceCourse", screen);
            Fixed(course, 56f, 172f, 1460f, 790f);

            RaceInfo(screen, true);
            var records = Image("PodiumRecordsPanel", screen, Paper, false, 1550f, 205f, 340f, 580f);
            Outline(records.gameObject, Gold);
            Accent(records.transform, Gold, 8f);
            Label("PodiumRecordsTitle", records.transform, "RACE RECORDS", 24, FontStyles.Bold, Ink, 28f, 28f, 284f, 42f);
            Label("PodiumRecordsText", records.transform, "1. Horse  00.00s", 18, FontStyles.Normal, Ink, 28f, 84f, 284f, 460f, TextAlignmentOptions.TopLeft);
            Button("ResultButton", screen, "RESULT", 1662f, 815f, 200f, 150f, Green);
        }

        [MenuItem("Malatro/UI/Create Horse Race Icon Prefab")]
        public static void EnsureHorseRaceIconPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HorseRaceIconPrefabPath) != null)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                AssetDatabase.CreateFolder("Assets/Resources", "UI");
            }

            var root = Rect("HorseRaceIcon", null);
            Fixed(root, 0f, 0f, 112f, 82f);
            var portrait = root.gameObject.AddComponent<Image>();
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            var rank = Image("RankBadge", root, HorseColor(0), false, -8f, -8f, 34f, 34f);
            Label("RankText", rank.transform, "1", 18, FontStyles.Bold, Color.white, 0f, 0f, 34f, 34f);
            var namePlate = Image("NamePlate", root, new Color(0.02f, 0.03f, 0.025f, 0.78f), false, 4f, 64f, 104f, 24f);
            Label("HorseName", namePlate.transform, "Horse", 13, FontStyles.Bold, Paper, 2f, 0f, 100f, 24f);

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, HorseRaceIconPrefabPath);
            Object.DestroyImmediate(root.gameObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created editable horse race icon prefab at {HorseRaceIconPrefabPath}.");
        }

        [MenuItem("Malatro/UI/Create Reusable UI Prefabs")]
        public static void EnsureReusableUiPrefabs()
        {
            EnsureUiFolder();
            CreatePrefabIfMissing(HorseInfoCardPrefabPath, () =>
            {
                var card = Card(null, "HorseInfoCard", "Horse", 200f, 200f, Gold);
                var title = card.Find("TitleText") as RectTransform;
                if (title != null)
                {
                    Fixed(title, 16f, 18f, 168f, 34f);
                }

                var horseImage = Image("HorseImage", card, Color.white, false, 35f, 52f, 130f, 84f);
                horseImage.preserveAspect = true;
                Label("OddsText", card, "2.5x", 18, FontStyles.Bold, Ink, 16f, 150f, 168f, 32f);
                return card.gameObject;
            });

            CreatePrefabIfMissing(BetTicketCardPrefabPath, () =>
            {
                var card = Card(null, "BetTicketCard", "BetTicket", 300f, 400f, Gold);
                Button("TicketTypeButton", card, "Ticket Type", 22f, 126f, 256f, 46f, PaperMuted);
                Button("FirstHorseButton", card, "First Horse", 22f, 188f, 256f, 46f, PaperMuted);
                Button("SecondHorseButton", card, "Second Horse", 22f, 250f, 256f, 46f, PaperMuted);
                Button("BuyTicketButton", card, "Payout", 54f, 326f, 192f, 46f, Green);
                return card.gameObject;
            });

            CreatePrefabIfMissing(RelicSlotPrefabPath, () =>
            {
                var card = Card(null, "RelicSlot", "Relic", 150f, 112f, Gold);
                return card.gameObject;
            });

            CreatePrefabIfMissing(RelicShopOfferPrefabPath, () =>
            {
                var card = Card(null, "RelicShopOfferCard", "Relic", 300f, 390f, Gold);
                Label("PriceText", card, "Price 30", 20, FontStyles.Bold, Ink, 34f, 282f, 232f, 36f);
                Button("BuyButton", card, "Buy", 54f, 326f, 192f, 42f, Green);
                return card.gameObject;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureUiFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "UI");
            }
        }

        private static void CreatePrefabIfMissing(string path, System.Func<GameObject> factory)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return;
            }

            var root = factory();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"Created editable UI prefab at {path}.");
        }

        private static void RaceInfo(Transform parent, bool showMoney)
        {
            var panel = Image("RaceInfoPanel", parent, Paper, false, 100f, 50f, 400f, 980f);
            Outline(panel.gameObject, Gold);
            Accent(panel.transform, Gold, 8f);
            var text = "Race Name\n\nG1 dirt: 2000m\n\n\nround 1 (1/3)\nround need\nmoney\n(20/100)";
            if (showMoney)
            {
                text += "\n\nmoney: 99";
            }

            Label("RaceInfoText", panel.transform, text, 26, FontStyles.Normal, Color.black, 48f, 56f, 304f, 850f, TextAlignmentOptions.Top);
        }

        private static void EnsureKoreanFontAsset()
        {
            koreanFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            if (koreanFontAsset != null && koreanFontAsset.material != null)
            {
                koreanFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                return;
            }

            if (koreanFontAsset != null)
            {
                AssetDatabase.DeleteAsset(KoreanFontAssetPath);
                koreanFontAsset = null;
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"Korean UI font was not found at {KoreanFontPath}.");
                return;
            }

            koreanFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            koreanFontAsset.name = "NanumGothic Bold SDF";
            koreanFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            koreanFontAsset.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(koreanFontAsset, KoreanFontAssetPath);
            if (koreanFontAsset.material != null && !AssetDatabase.Contains(koreanFontAsset.material))
            {
                koreanFontAsset.material.name = "NanumGothic Bold Material";
                AssetDatabase.AddObjectToAsset(koreanFontAsset.material, koreanFontAsset);
            }

            foreach (var atlasTexture in koreanFontAsset.atlasTextures)
            {
                if (atlasTexture == null || AssetDatabase.Contains(atlasTexture))
                {
                    continue;
                }

                atlasTexture.name = "NanumGothic Bold Atlas";
                AssetDatabase.AddObjectToAsset(atlasTexture, koreanFontAsset);
            }

            EditorUtility.SetDirty(koreanFontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(KoreanFontAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void TicketCard(Transform parent, int index)
        {
            var card = Card(parent, $"BetTicket_{index:00}", $"마권 {index}", 300f, 400f, Gold);
            Button("TicketTypeButton", card, "마권 종류", 22f, 126f, 256f, 46f, PaperMuted);
            Button("FirstHorseButton", card, "첫 번째 말", 22f, 188f, 256f, 46f, PaperMuted);
            Button("SecondHorseButton", card, "두 번째 말", 22f, 250f, 256f, 46f, PaperMuted);
            Button("BuyTicketButton", card, "구매", 54f, 326f, 192f, 46f, Green);
        }

        private static RectTransform Card(Transform parent, string name, string title, float width, float height, Color accent)
        {
            var card = Image(name, parent, Paper, true).rectTransform;
            card.gameObject.AddComponent<Button>();
            Layout(card.gameObject, width, height);
            Outline(card.gameObject, accent);
            Accent(card.transform, accent, 8f);
            Label("TitleText", card.transform, title, 24, FontStyles.Bold, Ink, 18f, 44f, width - 36f, 56f);
            return card;
        }

        private static GameObject Button(string name, Transform parent, string text, float x, float y, float width, float height, Color accent)
        {
            var button = Image(name, parent, Paper, true, x, y, width, height).gameObject;
            button.AddComponent<Button>();
            Outline(button, accent);
            Accent(button.transform, accent, 6f);
            Label("Label", button.transform, text, 22, FontStyles.Bold, Ink, 12f, 10f, width - 24f, height - 20f);
            return button;
        }

        private static RectTransform Scroll(string name, Transform parent, float x, float y, float width, float height, float spacing)
        {
            var root = Image(name, parent, new Color(0.04f, 0.08f, 0.14f, 0.16f), true, x, y, width, height).rectTransform;
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = Rect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(3600f, 0f);

            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        private static void FitContent(RectTransform content)
        {
            var layout = content.GetComponent<HorizontalLayoutGroup>();
            var spacing = layout != null ? layout.spacing : 0f;
            var activeCount = 0;
            var width = 0f;

            for (var i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                var layoutElement = child.GetComponent<LayoutElement>();
                width += layoutElement != null && layoutElement.preferredWidth > 0f
                    ? layoutElement.preferredWidth
                    : child.sizeDelta.x;
                activeCount++;
            }

            if (activeCount > 1)
            {
                width += spacing * (activeCount - 1);
            }

            var viewport = content.parent as RectTransform;
            var viewportWidth = viewport != null ? viewport.rect.width : 0f;
            content.sizeDelta = new Vector2(Mathf.Max(viewportWidth, width), content.sizeDelta.y);
        }

        private static Image Image(string name, Transform parent, Color color, bool raycast, float x = 0f, float y = 0f, float width = 100f, float height = 100f, bool stretch = false)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            if (stretch)
            {
                Stretch(rect);
            }
            else
            {
                Fixed(rect, x, y, width, height);
            }

            return image;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string text, int size, FontStyles style, Color color, float x, float y, float width, float height, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            koreanFontAsset ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            if (koreanFontAsset != null)
            {
                label.font = koreanFontAsset;
                if (koreanFontAsset.material != null)
                {
                    label.fontSharedMaterial = koreanFontAsset.material;
                }
            }

            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Normal;
            label.color = color;
            label.alignment = align;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            Fixed(label.rectTransform, x, y, width, height);
            return label;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
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

        private static void Fixed(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Layout(GameObject obj, float width, float height)
        {
            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.minWidth = width;
            layout.minHeight = height;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private static void Outline(GameObject obj, Color accent)
        {
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(6f, -6f);
        }

        private static void Accent(Transform parent, Color color, float height)
        {
            var bar = Image("Accent", parent, color, false, stretch: true).rectTransform;
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.offsetMin = new Vector2(0f, -height);
            bar.offsetMax = Vector2.zero;
        }

        private static void BoardBands(Transform parent)
        {
            Image("LeftShade", parent, BoardBlueDark, false, 0f, 0f, 560f, 1080f);
            Image("BottomShade", parent, new Color(0.09f, 0.18f, 0.3f, 1f), false, 0f, 720f, 1920f, 360f);
            Image("TopLine", parent, new Color(0.98f, 0.78f, 0.34f, 0.38f), false, 0f, 0f, 1920f, 4f);
        }

        private static Color HorseColor(int index)
        {
            var colors = new[]
            {
                new Color(0.82f, 0.25f, 0.22f, 1f),
                new Color(0.22f, 0.48f, 0.82f, 1f),
                new Color(0.35f, 0.68f, 0.38f, 1f),
                new Color(0.88f, 0.66f, 0.18f, 1f),
                new Color(0.57f, 0.38f, 0.82f, 1f),
                new Color(0.85f, 0.42f, 0.16f, 1f)
            };
            return colors[index % colors.Length];
        }
    }
}

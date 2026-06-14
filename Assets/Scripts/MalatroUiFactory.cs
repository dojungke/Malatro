using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Malatro
{
    public sealed class MalatroUiFactory
    {
        private TMP_FontAsset font;
        private Color defaultTextColor;

        public void Configure(TMP_FontAsset fontAsset, Color textColor)
        {
            font = fontAsset;
            defaultTextColor = textColor;
        }

        public RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            int size,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
            }
            label.text = text;
            label.fontSize = size;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(10f, size * 0.65f);
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Truncate;
            return label;
        }

        public GameObject CreateButton(
            string name,
            Transform parent,
            string label,
            Action action,
            Color color,
            int fontSize)
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
            button.onClick.AddListener(() => action?.Invoke());

            var text = CreateText(
                "Label",
                image.transform,
                label,
                fontSize,
                FontStyles.Bold,
                defaultTextColor,
                TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.margin = new Vector4(8f, 4f, 8f, 4f);
            return image.gameObject;
        }

        public RectTransform CreateHorizontalScrollView(
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

        public RectTransform CreateVerticalLayout(string name, Transform parent, float spacing, RectOffset padding)
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

        public RectTransform CreateHorizontalLayout(string name, Transform parent, float spacing, RectOffset padding)
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

        public static void AddLayoutElement(GameObject target, float width, float height, float flexibleWidth)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            if (width >= 0f) element.preferredWidth = width;
            if (height >= 0f) element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
        }

        public RectTransform CreateFlexibleSpacer(Transform parent)
        {
            var spacer = CreateRect("Spacer", parent);
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return spacer;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void SetFixed(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}

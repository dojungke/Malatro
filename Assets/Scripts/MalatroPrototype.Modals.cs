using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Malatro
{
    public sealed partial class MalatroPrototype
    {
        private void ShowHorseModal(Horse horse)
        {
            BuildHorseModal(horse);
        }

        private void ShowRelicModal(RelicData relic)
        {
            BuildRelicModal(relic);
        }

        private void ShowTicketTypeModal(BetTicket ticket)
        {
            BuildTicketTypeModal(ticket);
        }

        private void ShowTicketHorseModal(BetTicket ticket, bool secondTarget)
        {
            BuildTicketHorseModal(ticket, secondTarget);
        }

        private void ShowHorseStatOfferSelectionModal(HorseStatShopOffer offer)
        {
            BuildHorseStatOfferSelectionModal(offer);
        }

        private void BuildHorseModal(Horse horse)
        {
            if (horse == null)
            {
                return;
            }

            ClearChildren(modalRoot);
            CreateModalBackdrop(() => ClearChildren(modalRoot));
            horseInfoPopupPrefab ??= Resources.Load<GameObject>("UI/HorseInfoPopup");
            var modal = InstantiateModalPrefab(horseInfoPopupPrefab, "Horse Detail");
            if (modal == null)
            {
                return;
            }

            var portrait = FindDeep(modal, "HorseImage")?.GetComponent<Image>();
            if (portrait != null)
            {
                portrait.sprite = GetHorsePortraitSprite(horse);
                portrait.color = Color.white;
                portrait.preserveAspect = true;
            }

            SetModalText(modal, "NameText", GetHorseName(horse), UiText);
            SetModalText(modal, "OddsText", $"{L("odds")}  {horse.WinOdds:0.0}x", UiGold);
            SetModalText(
                modal,
                "DescriptionText",
                horse.Data != null ? horse.Data.GetDescription(language == UiLanguage.Korean) : string.Empty,
                UiMuted);
            SetModalText(modal, "StatsText", StatLine(horse), UiText);
            SetModalText(modal, "TagsText", GetHorseTagsText(horse), UiGold);
            SetModalText(
                modal,
                "SkillNameText",
                GetSkillName(horse.Skill),
                horse.Skill != null ? horse.Skill.EffectColor : UiText);
            SetModalText(
                modal,
                "SkillDescriptionText",
                horse.Skill != null ? horse.Skill.GetDescription(language == UiLanguage.Korean) : string.Empty,
                UiMuted);
            BindModalButton(modal, "CloseButton", "OK", () => ClearChildren(modalRoot));
        }

        private string GetHorseTagsText(Horse horse)
        {
            var tags = horse?.Data?.Tags;
            var prefix = language == UiLanguage.Korean ? "태그" : "Tags";
            if (tags == null || tags.Count == 0)
            {
                return language == UiLanguage.Korean ? "태그 없음" : "No tags";
            }

            return $"{prefix}: {string.Join(" · ", tags)}";
        }

        private void BuildRelicModal(RelicData relic)
        {
            if (relic == null)
            {
                return;
            }

            var owned = relicInventory.Contains(relic);
            ClearChildren(modalRoot);
            CreateModalBackdrop(() => ClearChildren(modalRoot));
            relicInfoPopupPrefab ??= Resources.Load<GameObject>("UI/RelicInfoPopup");
            var modal = InstantiateModalPrefab(relicInfoPopupPrefab, "Relic Detail");
            if (modal == null)
            {
                return;
            }

            var icon = FindDeep(modal, "RelicIcon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = relic.Icon;
                icon.color = relic.Icon != null ? Color.white : Tint(relic.Color, 0.22f);
                icon.preserveAspect = true;
            }

            SetModalText(modal, "RarityText", GetRarityName(relic.Rarity).ToUpperInvariant(), relic.Color);
            SetModalText(modal, "NameText", relic.GetName(language == UiLanguage.Korean), UiText);
            SetModalText(modal, "DescriptionText", relic.GetDescription(language == UiLanguage.Korean), UiMuted);
            SetModalText(
                modal,
                "PriceText",
                owned ? $"{L("sell")} +{relic.SellPrice:N0}" : $"{L("buy")} {relic.Price:N0}",
                UiGold);
            BindModalButton(
                modal,
                "CloseButton",
                language == UiLanguage.Korean ? "닫기" : "Close",
                () => ClearChildren(modalRoot));

            if (owned)
            {
                BindModalButton(
                    modal,
                    "ActionButton",
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
                    UiRed);
            }
            else
            {
                BindModalButton(
                    modal,
                    "ActionButton",
                    $"{L("buy")} {relic.Price:N0}",
                    () =>
                    {
                        BuyRelic(relic);
                        if (!relicInventory.Contains(relic))
                        {
                            return;
                        }

                        ClearChildren(modalRoot);
                        MarkUiDirty();
                    },
                    UiGreen);
            }
        }

        private RectTransform InstantiateModalPrefab(GameObject prefab, string instanceName)
        {
            if (prefab == null)
            {
                Debug.LogError($"Modal prefab was not found for {instanceName}.");
                return null;
            }

            var instance = Instantiate(prefab, modalRoot, false);
            instance.name = instanceName;
            instance.SetActive(true);
            var rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.SetAsLastSibling();
            ApplyUiFontToHierarchy(instance.transform);
            return rect;
        }

        private static void SetModalText(Transform root, string objectName, string value, Color color)
        {
            var label = FindDeep(root, objectName)?.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                return;
            }

            label.text = value;
            label.color = color;
        }

        private static void BindModalButton(
            Transform root,
            string objectName,
            string label,
            UnityAction action,
            Color? color = null)
        {
            var buttonTransform = FindDeep(root, objectName);
            var button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            var buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (buttonLabel != null)
            {
                buttonLabel.text = label;
            }

            var image = button.GetComponent<Image>();
            if (image != null && color.HasValue)
            {
                image.color = color.Value;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Malatro
{
    public sealed partial class MalatroPrototype
    {
        private void ShowEntrantSwapModal()
        {
            if (phase != GamePhase.Betting && phase != GamePhase.Shop)
            {
                return;
            }

            if (entrantSwapUsesRemaining <= 0 || roster.Count <= field.Count)
            {
                SetLog("swap_unavailable");
                MarkUiDirty();
                return;
            }

            BuildEntrantSwapModal(new List<Horse>());
        }

        private void BuildEntrantSwapModal(List<Horse> outgoing)
        {
            var maximum = GetEntrantSwapLimitPerUse();

            ClearChildren(modalRoot);
            CreateModalBackdrop(() => ClearChildren(modalRoot));
            var modal = CreateModalPanel("Entrant Swap", 1180f, 820f);
            var rootLayout = modal.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(30, 30, 26, 26);
            rootLayout.spacing = 14f;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = false;

            var title = CreateText(
                "Title",
                modal,
                $"{L("swap_entrants")}  ·  {string.Format(L("swap_uses_left"), entrantSwapUsesRemaining)}",
                30,
                FontStyles.Bold,
                UiText,
                TextAlignmentOptions.Center);
            AddLayoutElement(title.gameObject, -1f, 48f, 0f);

            var instruction = CreateText(
                "Instruction",
                modal,
                string.Format(L("swap_instruction"), maximum),
                17,
                FontStyles.Normal,
                UiMuted,
                TextAlignmentOptions.Center);
            AddLayoutElement(instruction.gameObject, -1f, 36f, 0f);

            var selection = CreateHorizontalLayout("Selection", modal, 24f, new RectOffset(270, 270, 0, 0));
            AddLayoutElement(selection.gameObject, -1f, 570f, 1f);
            BuildEntrantSwapColumn(selection, L("swap_current"), field, outgoing, maximum, () =>
                BuildEntrantSwapModal(outgoing));

            var actions = CreateHorizontalLayout("Actions", modal, 16f, new RectOffset(0, 0, 0, 0));
            AddLayoutElement(actions.gameObject, -1f, 58f, 0f);
            var cancel = CreateButton("Cancel", actions, L("swap_cancel"), () => ClearChildren(modalRoot), UiSurfaceRaised, 17);
            AddLayoutElement(cancel, 220f, 52f, 0f);
            CreateFlexibleSpacer(actions);
            var confirm = CreateButton(
                "Confirm",
                actions,
                $"{L("swap_confirm")} ({outgoing.Count}/{maximum})",
                () =>
                {
                    if (!ApplyEntrantSwaps(outgoing, maximum))
                    {
                        BuildEntrantSwapModal(outgoing);
                    }
                },
                UiGreen,
                17);
            confirm.GetComponent<Button>().interactable = outgoing.Count > 0;
            AddLayoutElement(confirm, 280f, 52f, 0f);
        }

        private void BuildEntrantSwapColumn(
            Transform parent,
            string titleText,
            IReadOnlyList<Horse> horses,
            List<Horse> selected,
            int maximum,
            System.Action refresh)
        {
            var panel = CreateImage(titleText, parent, UiSurface, true);
            AddLayoutElement(panel.gameObject, 540f, 570f, 1f);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var heading = CreateText(
                "Heading",
                panel.transform,
                $"{titleText}  {selected.Count}/{maximum}",
                21,
                FontStyles.Bold,
                UiGold,
                TextAlignmentOptions.Center);
            AddLayoutElement(heading.gameObject, -1f, 40f, 0f);

            foreach (var horse in horses)
            {
                var capturedHorse = horse;
                var isSelected = selected.Contains(horse);
                var button = CreateButton(
                    GetShortHorseName(horse),
                    panel.transform,
                    $"{(isSelected ? (language == UiLanguage.Korean ? "[선택] " : "[SELECTED] ") : string.Empty)}"
                    + $"{GetHorseName(horse)}    {StatLine(horse)}",
                    () =>
                    {
                        if (!selected.Remove(capturedHorse) && selected.Count < maximum)
                        {
                            selected.Add(capturedHorse);
                        }

                        refresh();
                    },
                    isSelected ? UiGreen : UiSurfaceRaised,
                    15);
                AddLayoutElement(button, -1f, 52f, 0f);
            }
        }

        private bool ApplyEntrantSwaps(List<Horse> outgoing, int maximum)
        {
            var incoming = roster
                .Where(horse => !field.Contains(horse))
                .OrderBy(_ => rng.Next())
                .Take(outgoing.Count)
                .ToList();

            if ((phase != GamePhase.Betting && phase != GamePhase.Shop)
                || entrantSwapUsesRemaining <= 0
                || outgoing.Count < 1
                || outgoing.Count > maximum
                || outgoing.Distinct().Count() != outgoing.Count
                || outgoing.Any(horse => !field.Contains(horse))
                || incoming.Count != outgoing.Count)
            {
                SetLog("swap_invalid");
                return false;
            }

            for (var i = 0; i < outgoing.Count; i++)
            {
                var oldHorse = outgoing[i];
                var newHorse = incoming[i];
                var fieldIndex = field.IndexOf(oldHorse);
                newHorse.Lane = oldHorse.Lane;
                field[fieldIndex] = newHorse;

                if (oldHorse.Renderer != null)
                {
                    oldHorse.Renderer.enabled = false;
                }

                if (newHorse.Renderer != null)
                {
                    newHorse.Renderer.enabled = newHorse.RunSheet == null;
                }
            }

            entrantSwapUsesRemaining--;
            offeredTickets.Clear();
            EnsureTicketCount();
            SetLog("swap_complete", outgoing.Count, entrantSwapUsesRemaining);
            ClearChildren(modalRoot);
            MarkUiDirty();
            return true;
        }

        private int GetEntrantSwapLimitPerUse()
        {
            if (multiplayerRulesEnabled)
            {
                return 1;
            }

            return Mathf.Clamp(
                currentRace != null ? currentRace.MaxEntrantSwapsPerUse : 3,
                1,
                3);
        }
    }
}

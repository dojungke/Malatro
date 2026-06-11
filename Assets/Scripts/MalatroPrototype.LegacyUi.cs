using System;
using System.Linq;
using UnityEngine;

namespace Malatro
{
    public sealed partial class MalatroPrototype
    {
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

            var laneSpacing = Mathf.Min(46f, (trackRect.height - 30f) / Mathf.Max(1, field.Count));
            for (var i = 0; i < field.Count; i++)
            {
                var horse = field[i];
                var y = trackRect.y + 18f + i * laneSpacing;
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
            var pixelsPerDistance = viewport.width / raceCameraViewDistance;
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
                var laneSpacing = Mathf.Min(46f, (viewport.height - 20f) / Mathf.Max(1, field.Count));
                var y = -5f + horse.LaneOffset * laneSpacing * Mathf.Max(1, field.Count - 1);
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

        private void DrawTopPanel()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, 132f), new Color(0.07f, 0.08f, 0.085f, 0.99f));
            GUI.Label(new Rect(22f, 10f, 470f, 34f), L("game_title"), titleStyle);
            GUI.Label(new Rect(22f, 48f, 230f, 28f), $"{L("gold")}  {gold:N0}", navStyle);
            var showRaceContext = phase == GamePhase.Racing || phase == GamePhase.Results;
            if (showRaceContext)
            {
                GUI.Label(
                    new Rect(264f, 48f, 520f, 26f),
                    $"{L("round")} {roundNumber}  |  {L("race")} {GetRaceInRound()}/{RacesPerRound}  |  {L("round_goal")} {roundEarnedGold:N0}/{roundTargetGold:N0}",
                    bodyStyle);
            }
            else
            {
                GUI.Label(
                    new Rect(264f, 48f, 520f, 26f),
                    $"{L("relic_inventory")} {relicInventory.Count}/{RelicInventory.MaximumCapacity}",
                    bodyStyle);
            }
            if (showRaceContext && currentRace != null)
            {
                GUI.Label(
                    new Rect(800f, 46f, Screen.width - 990f, 42f),
                    $"{currentRace.GetName(language == UiLanguage.Korean)}  |  {currentRace.League}\n{currentRace.GetSurfaceName(language == UiLanguage.Korean)}  |  {currentRace.TotalDistanceMeters}m  |  {L("entrants")} {field.Count}",
                    bodyStyle);
            }

            if (GUI.Button(new Rect(Screen.width - 170f, 12f, 142f, 32f), language == UiLanguage.Korean ? "??癰궽살쐿??| EN" : "English | KR", buttonStyle))
            {
                ToggleLanguage();
            }

            if (!showRaceContext)
            {
                return;
            }

            GUI.Label(new Rect(22f, 90f, 124f, 24f), L("camera"), smallStyle);
            var autoSelected = cameraTargetLane == -1;
            if (GUI.Button(new Rect(116f, 84f, 92f, 32f), autoSelected ? $"[*] {L("leader")}" : L("leader"), buttonStyle))
            {
                cameraTargetLane = -1;
            }

            var availableWidth = Screen.width - 236f;
            var buttonWidth = Mathf.Clamp(availableWidth / Mathf.Max(1, field.Count) - 8f, 92f, 170f);
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

            GUI.Label(new Rect(panel.x + 24f, panel.y + 14f, 500f, 28f), runComplete ? L("run_failed_title") : L("prediction_screen"), titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 48f, panel.width - 48f, 26f), GetLog(), bodyStyle);

            DrawHorseRoster(panel, panel.y + 86f);

            if (!runComplete)
            {
                DrawTickets(panel);
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

        private void DrawShopPanel()
        {
            var panel = GetLowerPanel();
            DrawRect(panel, new Color(0.09f, 0.105f, 0.11f, 0.98f));

            GUI.Label(new Rect(panel.x + 24f, panel.y + 14f, 500f, 28f), L("shop_screen"), titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 48f, panel.width - 48f, 26f), GetLog(), bodyStyle);

            DrawHorseRoster(panel, panel.y + 86f);
            DrawRelicShop(panel, panel.y + 178f);

            GUI.enabled = !runComplete;
            if (GUI.Button(new Rect(panel.x + panel.width - 220f, panel.y + 30f, 180f, 42f), L("start_race"), buttonStyle))
            {
                StartRace();
            }

            GUI.enabled = true;
            if (GUI.Button(new Rect(panel.x + panel.width - 220f, panel.y + 84f, 180f, 34f), L("back_prediction"), buttonStyle))
            {
                TransitionTo(GamePhase.Betting);
                SetLog("pick_ticket");
            }
        }

        private void DrawHorseRoster(Rect panel, float y)
        {
            GUI.Label(new Rect(panel.x + 24f, y - 24f, panel.width - 48f, 22f), L("horse_info_hint"), smallStyle);

            var gap = 10f;
            var columns = Mathf.Max(1, field.Count);
            var buttonWidth = Mathf.Min(178f, (panel.width - 48f - gap * (columns - 1)) / columns);
            for (var i = 0; i < field.Count; i++)
            {
                var horse = field[i];
                var rect = new Rect(panel.x + 24f + i * (buttonWidth + gap), y, buttonWidth, 58f);
                DrawRect(rect, new Color(horse.Color.r * 0.2f, horse.Color.g * 0.2f, horse.Color.b * 0.2f, 1f));
                GUI.Box(rect, GUIContent.none, cardStyle);
                if (GUI.Button(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 28f), GetShortHorseName(horse), buttonStyle))
                {
                    selectedHorseInfo = horse;
                }

                GUI.Label(new Rect(rect.x + 8f, rect.y + 36f, rect.width - 16f, 18f), $"{L("odds")} {horse.WinOdds:0.0}x", centeredStyle);
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
                    OpenTicketSelection(ticket, TicketSelectionMode.Type);
                }

                var targetWidth = ticket.NeedsSecondHorse ? (rect.width - 32f) * 0.5f : rect.width - 24f;
                if (GUI.Button(new Rect(rect.x + 12f, rect.y + 46f, targetWidth, 30f), GetShortHorseName(ticket.First), buttonStyle))
                {
                    OpenTicketSelection(ticket, TicketSelectionMode.FirstHorse);
                }

                if (ticket.NeedsSecondHorse && GUI.Button(new Rect(rect.x + 20f + targetWidth, rect.y + 46f, targetWidth, 30f), GetShortHorseName(ticket.Second), buttonStyle))
                {
                    OpenTicketSelection(ticket, TicketSelectionMode.SecondHorse);
                }

                GUI.Label(new Rect(rect.x + 12f, rect.y + 80f, rect.width - 24f, 22f), $"{ticket.Odds:0.0}x  |  {L("payout")} {GetRelicAdjustedPayout(ticket)}", centeredStyle);
            }

            GUI.Label(
                new Rect(panel.x + panel.width - 220f, panel.y + 132f, 180f, 44f),
                $"{L("all_tickets")}  {offeredTickets.Count}",
                centeredStyle);
        }

        private void DrawRelicShop(Rect panel, float shopY)
        {
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
            var totalOfferCount = Mathf.Max(1, shopOffers.Count);
            var cardWidth = (panel.width - 48f - gap * (totalOfferCount - 1)) / totalOfferCount;
            var cardY = shopY + 28f;
            for (var i = 0; i < shopOffers.Count; i++)
            {
                var entry = shopOffers[i];
                var rect = new Rect(
                    panel.x + 24f + i * (cardWidth + gap),
                    cardY,
                    cardWidth,
                    86f);
                if (entry.IsRelic)
                {
                    var relic = entry.Relic;
                    var owned = relicInventory.Contains(relic);
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
                    continue;
                }

                var offer = entry.StatOffer;
                DrawRect(rect, offer.RequiresHorseSelection
                    ? new Color(0.05f, 0.28f, 0.34f, 1f)
                    : new Color(0.34f, 0.25f, 0.06f, 1f));
                GUI.Box(rect, GUIContent.none, cardStyle);
                GUI.Label(
                    new Rect(rect.x + 6f, rect.y + 5f, rect.width - 12f, 34f),
                    GetHorseStatOfferTitle(offer),
                    centeredStyle);
                GUI.Label(
                    new Rect(rect.x + 6f, rect.y + 38f, rect.width - 12f, 20f),
                    GetHorseStatOfferDescription(offer).Replace("\n", " / "),
                    smallStyle);

                GUI.enabled = !offer.Purchased;
                var label = offer.Purchased ? L("purchase_complete") : $"{L("buy")} {offer.Price}";
                if (GUI.Button(new Rect(rect.x + 6f, rect.y + 59f, rect.width - 12f, 22f), label, buttonStyle))
                {
                    if (gold < offer.Price)
                    {
                        SetLog("stat_offer_need_gold", offer.Price);
                    }
                    else if (offer.RequiresHorseSelection)
                    {
                        pendingHorseStatOffer = offer;
                    }
                    else
                    {
                        BuyHorseStatOffer(offer, offer.RandomTarget);
                    }
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
                    selectedRelicInfo = relic;
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

        private void OpenTicketSelection(BetTicket ticket, TicketSelectionMode mode)
        {
            editingTicket = ticket;
            ticketSelectionMode = mode;
            selectedHorseInfo = null;
        }

        private void CloseTicketSelection()
        {
            editingTicket = null;
            ticketSelectionMode = TicketSelectionMode.None;
        }

        private void DrawRacePanel()
        {
            var panel = new Rect(32f, 464f, Screen.width - 64f, 132f);
            DrawRect(panel, new Color(0.09f, 0.105f, 0.11f, 0.98f));
            var target = GetCameraTarget();
            GUI.Label(new Rect(panel.x + 24f, panel.y + 16f, 420f, 28f), $"{L("race_clock")}: {raceClock:0.0}s", titleStyle);
            GUI.Label(new Rect(panel.x + 360f, panel.y + 18f, panel.width - 650f, 26f), $"{L("following")}: {(cameraTargetLane == -1 ? L("leader") : GetHorseName(target))}  (Q / E)", bodyStyle);
            DrawRaceSpeedControls(panel);
            var ticketSummary = string.Join("   |   ", offeredTickets.Select((ticket, index) =>
                $"{index + 1}. {ticket.GetLabel(language == UiLanguage.Korean, GetHorseName)}"));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 50f, panel.width - 48f, 42f), ticketSummary, smallStyle);

            var messages = field
                .Where(horse => !string.IsNullOrEmpty(horse.SkillMessage))
                .Select(horse => $"{GetHorseName(horse)}: {GetSkillName(horse.Skill)}");
            GUI.Label(new Rect(panel.x + 24f, panel.y + 82f, panel.width - 48f, 24f), string.Join("   ", messages), smallStyle);
        }

        private void DrawRaceSpeedControls(Rect panel)
        {
            var startX = panel.x + panel.width - 272f;
            GUI.Label(new Rect(startX - 58f, panel.y + 18f, 54f, 24f), L("speed_control"), smallStyle);

            var speeds = new[] { 1f, 1.5f, 2f };
            for (var i = 0; i < speeds.Length; i++)
            {
                var speed = speeds[i];
                var selected = Mathf.Approximately(racePlaybackSpeed, speed);
                var label = selected ? $"[*] {speed:0.#}x" : $"{speed:0.#}x";
                if (GUI.Button(new Rect(startX + i * 82f, panel.y + 12f, 76f, 32f), label, buttonStyle))
                {
                    racePlaybackSpeed = speed;
                }
            }
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

        private void DrawHorseInfoPopup()
        {
            if (selectedHorseInfo == null || !field.Contains(selectedHorseInfo))
            {
                selectedHorseInfo = null;
                return;
            }

            var width = Mathf.Min(460f, Screen.width - 64f);
            var height = 292f;
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.42f));
            GUI.Window(7219, rect, DrawHorseInfoWindow, GUIContent.none, cardStyle);
        }

        private void DrawRelicInfoPopup()
        {
            if (selectedRelicInfo == null || !relicInventory.Contains(selectedRelicInfo))
            {
                selectedRelicInfo = null;
                return;
            }

            var width = Mathf.Min(520f, Screen.width - 40f);
            var height = 330f;
            var rect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            GUI.ModalWindow(7103, rect, DrawRelicInfoWindow, string.Empty);
        }

        private void DrawRelicInfoWindow(int windowId)
        {
            var relic = selectedRelicInfo;
            if (relic == null)
            {
                return;
            }

            GUI.Label(new Rect(24f, 18f, 360f, 34f), relic.GetName(language == UiLanguage.Korean), titleStyle);
            GUI.Label(new Rect(24f, 56f, 360f, 26f), GetRarityName(relic.Rarity), navStyle);
            GUI.Label(
                new Rect(24f, 96f, 472f, 128f),
                relic.GetDescription(language == UiLanguage.Korean),
                bodyStyle);
            GUI.Label(
                new Rect(24f, 228f, 280f, 28f),
                $"{L("buy")} {relic.Price:N0}    {L("sell")} +{relic.SellPrice:N0}",
                bodyStyle);

            if (GUI.Button(new Rect(24f, 270f, 210f, 42f), language == UiLanguage.Korean ? "닫기" : "Close", buttonStyle))
            {
                selectedRelicInfo = null;
            }

            if (GUI.Button(new Rect(286f, 270f, 210f, 42f), $"{L("sell")} +{relic.SellPrice:N0}", buttonStyle))
            {
                SellRelic(relic);
                selectedRelicInfo = null;
            }
        }

        private void DrawHorseInfoWindow(int windowId)
        {
            var horse = selectedHorseInfo;
            if (horse == null)
            {
                return;
            }

            DrawRect(new Rect(0f, 0f, 460f, 292f), new Color(0.1f, 0.115f, 0.12f, 0.98f));
            GUI.Label(new Rect(20f, 18f, 320f, 30f), GetHorseName(horse), titleStyle);
            if (GUI.Button(new Rect(380f, 18f, 52f, 28f), "X", buttonStyle))
            {
                selectedHorseInfo = null;
                return;
            }

            var texture = horse.RunSheet;
            if (texture != null)
            {
                var iconRect = new Rect(24f, 62f, 92f, 92f);
                DrawRect(iconRect, new Color(horse.Color.r * 0.25f, horse.Color.g * 0.25f, horse.Color.b * 0.25f, 1f));
                GUI.DrawTextureWithTexCoords(iconRect, texture, new Rect(0f, 0f, 0.5f, 1f), true);
            }

            GUI.Label(new Rect(136f, 62f, 292f, 24f), $"{L("odds")} {horse.WinOdds:0.0}x", bodyStyle);
            GUI.Label(new Rect(136f, 90f, 292f, 42f), StatLine(horse), smallStyle);
            GUI.Label(new Rect(136f, 138f, 292f, 24f), $"{L("skill")}: {GetSkillName(horse.Skill)}", bodyStyle);

            var description = horse.Skill != null
                ? horse.Skill.GetDescription(language == UiLanguage.Korean)
                : string.Empty;
            GUI.Label(new Rect(24f, 172f, 408f, 58f), description, smallStyle);

            var tags = horse.Data != null && horse.Data.Tags != null
                ? string.Join("  ", horse.Data.Tags)
                : string.Empty;
            GUI.Label(new Rect(24f, 236f, 408f, 24f), tags, smallStyle);
        }

        private void DrawTicketSelectionPopup()
        {
            if (editingTicket == null || ticketSelectionMode == TicketSelectionMode.None)
            {
                return;
            }

            var optionCount = ticketSelectionMode == TicketSelectionMode.Type
                ? Enum.GetValues(typeof(BetType)).Length
                : field.Count;
            var width = Mathf.Min(420f, Screen.width - 64f);
            var height = 92f + optionCount * 42f;
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.48f));
            GUI.Window(7220, rect, DrawTicketSelectionWindow, GUIContent.none, cardStyle);
        }

        private void DrawHorseStatOfferSelectionPopup()
        {
            if (pendingHorseStatOffer == null || pendingHorseStatOffer.Purchased)
            {
                pendingHorseStatOffer = null;
                return;
            }

            var width = Mathf.Min(720f, Screen.width - 80f);
            var height = Mathf.Min(520f, Screen.height - 80f);
            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.7f));
            GUI.Box(panel, GUIContent.none, cardStyle);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 54f),
                $"{GetHorseStatOfferTitle(pendingHorseStatOffer)}\n{L("select_horse")}",
                centeredStyle);

            var y = panel.y + 82f;
            foreach (var horse in roster)
            {
                if (GUI.Button(
                    new Rect(panel.x + 28f, y, panel.width - 56f, 36f),
                    GetHorseStatOfferSelectionLabel(horse, pendingHorseStatOffer),
                    buttonStyle))
                {
                    BuyHorseStatOffer(pendingHorseStatOffer, horse);
                    pendingHorseStatOffer = null;
                    return;
                }

                y += 40f;
            }

            if (GUI.Button(
                new Rect(panel.x + panel.width - 128f, panel.y + panel.height - 48f, 100f, 30f),
                "OK",
                buttonStyle))
            {
                pendingHorseStatOffer = null;
            }
        }

        private void DrawTicketSelectionWindow(int windowId)
        {
            var title = ticketSelectionMode == TicketSelectionMode.Type
                ? L("select_ticket_type")
                : L("select_horse");
            GUI.Label(new Rect(20f, 16f, 300f, 30f), title, titleStyle);
            if (GUI.Button(new Rect(348f, 16f, 44f, 28f), "X", buttonStyle))
            {
                CloseTicketSelection();
                return;
            }

            if (ticketSelectionMode == TicketSelectionMode.Type)
            {
                var values = (BetType[])Enum.GetValues(typeof(BetType));
                for (var i = 0; i < values.Length; i++)
                {
                    var type = values[i];
                    var selected = editingTicket.Type == type;
                    var label = selected ? $"[*] {GetBetTypeName(type)}" : GetBetTypeName(type);
                    if (GUI.Button(new Rect(24f, 58f + i * 42f, 368f, 34f), label, buttonStyle))
                    {
                        editingTicket.SetType(type);
                        if (editingTicket.NeedsSecondHorse && editingTicket.Second == null)
                        {
                            editingTicket.SetSecond(field.FirstOrDefault(horse => horse != editingTicket.First));
                        }

                        SetLog("customize_all");
                        CloseTicketSelection();
                        return;
                    }
                }

                return;
            }

            for (var i = 0; i < field.Count; i++)
            {
                var horse = field[i];
                var disabled = ticketSelectionMode == TicketSelectionMode.FirstHorse
                    ? editingTicket.NeedsSecondHorse && horse == editingTicket.Second
                    : horse == editingTicket.First;
                var selected = ticketSelectionMode == TicketSelectionMode.FirstHorse
                    ? horse == editingTicket.First
                    : horse == editingTicket.Second;

                GUI.enabled = !disabled;
                var label = $"{(selected ? "[*] " : string.Empty)}{GetHorseName(horse)}  |  {horse.WinOdds:0.0}x";
                if (GUI.Button(new Rect(24f, 58f + i * 42f, 368f, 34f), label, buttonStyle))
                {
                    if (ticketSelectionMode == TicketSelectionMode.FirstHorse)
                    {
                        editingTicket.SetFirst(horse);
                    }
                    else
                    {
                        editingTicket.SetSecond(horse);
                    }

                    SetLog("customize_all");
                    CloseTicketSelection();
                    GUI.enabled = true;
                    return;
                }

                GUI.enabled = true;
            }
        }

    }
}

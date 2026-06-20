using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Malatro
{
    public enum BetType
    {
        Win,
        Place,
        Quinella,
        Exacta
    }

    [Serializable]
    // 踰좏똿 醫낅쪟? ???留먯쓣 蹂닿??섍퀬 諛곕떦 諛??곸쨷 ?щ?瑜?怨꾩궛?쒕떎.
    public sealed class BetTicket
    {
        public const int BasePayout = 10;

        private BetType type;
        private Horse first;
        private Horse second;
        private float odds;

        public BetType Type => type;
        public Horse First => first;
        public Horse Second => second;
        public float Odds => odds;
        public bool NeedsSecondHorse => type == BetType.Quinella || type == BetType.Exacta;
        public int ExpectedPayout => Mathf.RoundToInt(BasePayout * odds);
        public string Signature => $"{type}:{first?.Lane ?? -1}:{second?.Lane ?? -1}";

        public BetTicket(BetType type, Horse first, Horse second)
        {
            this.type = type;
            this.first = first;
            this.second = second;
            NormalizeTargets();
            RefreshOdds();
        }

        public void SetType(BetType value)
        {
            type = value;
            NormalizeTargets();
            RefreshOdds();
        }

        public void SetFirst(Horse horse)
        {
            first = horse;
            NormalizeTargets();
            RefreshOdds();
        }

        public void SetSecond(Horse horse)
        {
            second = horse;
            NormalizeTargets();
            RefreshOdds();
        }

        public void RefreshOdds()
        {
            if (first == null)
            {
                odds = 1f;
                return;
            }

            switch (type)
            {
                // 蹂듯빀 踰좏똿?쇱닔濡?留욏엳湲??대젮??留뚰겮 ??留먯쓽 諛곕떦???④퍡 諛섏쁺?쒕떎.
                case BetType.Win:
                    odds = first.WinOdds;
                    break;
                case BetType.Place:
                    odds = Mathf.Max(1.1f, first.WinOdds * 0.42f);
                    break;
                case BetType.Quinella:
                    odds = second == null ? 1f : Mathf.Max(1.3f, (first.WinOdds + second.WinOdds) * 0.72f);
                    break;
                case BetType.Exacta:
                    odds = second == null ? 1f : Mathf.Max(1.6f, first.WinOdds * second.WinOdds * 0.38f);
                    break;
            }
        }

        public int GetExpectedPayout(int baseGoldBonus)
        {
            return Mathf.RoundToInt(BasePayout * odds) + Mathf.Max(0, baseGoldBonus);
        }

        public string GetTypeName(bool korean)
        {
            return MalatroLocalization.GetBetTypeName(type, korean);
        }

        public string GetTargetText(bool korean, Func<Horse, string> horseName)
        {
            var firstName = horseName(first);
            var secondName = second != null ? horseName(second) : string.Empty;
            return MalatroLocalization.GetBetTarget(type, firstName, secondName, korean);
        }

        public string GetLabel(bool korean, Func<Horse, string> horseName)
        {
            return $"{GetTypeName(korean)} {GetTargetText(korean, horseName)} ({odds:0.0}x)";
        }

        public bool Evaluate(IReadOnlyList<Horse> standings)
        {
            if (standings == null || standings.Count == 0 || first == null)
            {
                return false;
            }

            switch (type)
            {
                case BetType.Win:
                    return standings[0] == first;
                case BetType.Place:
                    return standings.Take(3).Contains(first);
                case BetType.Quinella:
                    return second != null && standings.Take(2).Contains(first) && standings.Take(2).Contains(second);
                case BetType.Exacta:
                    return second != null && standings.Count > 1 && standings[0] == first && standings[1] == second;
                default:
                    return false;
            }
        }

        private void NormalizeTargets()
        {
            // ?⑥씪 ???踰좏똿? ??踰덉㎏ 留먯쓣 踰꾨━怨? 蹂듯빀 踰좏똿? 媛숈? 留먯쓣 以묐났 ?좏깮?섏? 紐삵븯寃??쒕떎.
            if (!NeedsSecondHorse)
            {
                second = null;
                return;
            }

            if (second == first)
            {
                second = null;
            }
        }
    }
}

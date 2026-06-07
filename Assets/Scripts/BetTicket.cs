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
    // 베팅 종류와 대상 말을 보관하고 배당 및 적중 여부를 계산한다.
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
                // 복합 베팅일수록 맞히기 어려운 만큼 두 말의 배당을 함께 반영한다.
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
            return Mathf.RoundToInt((BasePayout + Mathf.Max(0, baseGoldBonus)) * odds);
        }

        public string GetTypeName(bool korean)
        {
            switch (type)
            {
                case BetType.Win: return korean ? "단승식" : "Win";
                case BetType.Place: return korean ? "연승식" : "Place";
                case BetType.Quinella: return korean ? "복승식" : "Quinella";
                case BetType.Exacta: return korean ? "쌍승식" : "Exacta";
                default: return korean ? "마권" : "Ticket";
            }
        }

        public string GetTargetText(bool korean, Func<Horse, string> horseName)
        {
            var firstName = horseName(first);
            var secondName = second != null ? horseName(second) : string.Empty;
            switch (type)
            {
                case BetType.Win:
                    return korean ? $"{firstName} 1위" : $"{firstName} must finish 1st.";
                case BetType.Place:
                    return korean ? $"{firstName} 3위 이내" : $"{firstName} must finish top 3.";
                case BetType.Quinella:
                    return korean ? $"{firstName} + {secondName} 순서 무관 2위 이내" : $"{firstName} + {secondName} top 2, any order.";
                case BetType.Exacta:
                    return korean ? $"{firstName} 1위, {secondName} 2위" : $"{firstName} 1st, {secondName} 2nd.";
                default:
                    return string.Empty;
            }
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
            // 단일 대상 베팅은 두 번째 말을 버리고, 복합 베팅은 같은 말을 중복 선택하지 못하게 한다.
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

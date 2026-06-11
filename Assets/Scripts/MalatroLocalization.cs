using System.Collections.Generic;

namespace Malatro
{
    public static class MalatroLocalization
    {
        private readonly struct Entry
        {
            public readonly string Korean;
            public readonly string English;

            public Entry(string korean, string english)
            {
                Korean = korean;
                English = english;
            }
        }

        private static readonly Dictionary<string, Entry> Entries = new()
        {
            ["prediction_screen"] = new("승부 예측", "Prediction"),
            ["shop_screen"] = new("상점", "Shop"),
            ["open_shop"] = new("상점으로 (Space)", "Go to Shop (Space)"),
            ["back_prediction"] = new("예측 수정", "Edit Picks"),
            ["horse_info_hint"] = new("말을 누르면 상세 정보를 볼 수 있습니다.", "Click a horse to inspect details."),
            ["select_ticket_type"] = new("마권 종류 선택", "Select Ticket Type"),
            ["select_horse"] = new("말 선택", "Select Horse"),
            ["shop_before_race"] = new("유물을 정비한 뒤 레이스를 시작하세요.", "Tune your relics, then start the race."),
            ["visit_shop_before_race"] = new("레이스 전에는 상점을 거쳐야 합니다.", "Visit the shop before starting the race."),
            ["game_title"] = new("말라트로: 승부 예측 경마", "Malatro: Prediction Horse Racing"),
            ["gold"] = new("골드", "Gold"),
            ["race"] = new("레이스", "Race"),
            ["round"] = new("라운드", "Round"),
            ["entrants"] = new("출전", "Entrants"),
            ["round_goal"] = new("목표", "Goal"),
            ["camera"] = new("카메라", "Camera"),
            ["leader"] = new("선두 자동", "Auto Leader"),
            ["finish"] = new("결승", "FINISH"),
            ["ticket_board"] = new("마권 선택", "Ticket Board"),
            ["meet_complete_title"] = new("시즌 종료", "Meet Complete"),
            ["start_race"] = new("레이스 시작 (Space)", "Start Race (Space)"),
            ["new_run"] = new("새 게임", "New Run"),
            ["stake"] = new("베팅", "Stake"),
            ["payout"] = new("예상 지급", "Payout"),
            ["all_tickets"] = new("전체 마권 적용", "All Tickets Active"),
            ["total_cost"] = new("총 비용", "Total Cost"),
            ["selected"] = new("선택됨", "Selected"),
            ["select"] = new("선택", "Select"),
            ["race_clock"] = new("경기 시간", "Race clock"),
            ["speed_control"] = new("배속", "Speed"),
            ["following"] = new("추격 중", "Following"),
            ["results"] = new("경기 결과", "Results"),
            ["odds"] = new("배당", "odds"),
            ["next_race_button"] = new("다음 레이스 (Space)", "Next Race (Space)"),
            ["check_round_button"] = new("라운드 결과 확인 (Space)", "Check Round (Space)"),
            ["speed_short"] = new("속도", "SPD"),
            ["accel_short"] = new("가속", "ACC"),
            ["stamina_short"] = new("지구력", "STA"),
            ["magic_short"] = new("마력", "MAG"),
            ["aptitude_short"] = new("적성", "APT"),
            ["skill"] = new("스킬", "Skill"),
            ["stunned"] = new("기절", "STUN"),
            ["time_stopped"] = new("시간 정지", "TIME STOP"),
            ["choose_ticket"] = new("마권을 선택하고 경기를 예측하세요.", "Choose one of three tickets, then read the race."),
            ["pick_ticket"] = new("마권 종류와 말을 설정하세요. 모든 마권이 레이스에 적용됩니다.", "Customize all three tickets. Every ticket is active when the race starts."),
            ["customize_all"] = new("마권 종류와 대상 말을 조정하세요. 모든 마권이 자동 적용됩니다.", "Customize the type and horses. All three tickets are automatically active."),
            ["ticket_selected"] = new("{0} 선택. Space로 레이스를 시작하세요.", "Selected {0}. Press Space to race."),
            ["season_complete"] = new("시즌이 끝났습니다. 새 게임을 시작하세요.", "Season complete. Start a new run."),
            ["pick_first"] = new("먼저 마권을 선택하세요.", "Pick a ticket first: 1, 2, or 3."),
            ["need_gold_all"] = new("마권을 적용하려면 골드 {0}이 필요합니다.", "You need {0} gold to activate all three tickets."),
            ["race_started"] = new("{0}라운드 {1}경기 시작. 마권 {2}장이 모두 적용됩니다.", "Round {0}, race {1}: all {2} tickets are active."),
            ["all_ticket_result"] = new("마권 {0}/{1}장 적중, 총 골드 {2} 획득.", "{0}/{1} tickets hit. Total payout: {2} gold."),
            ["ticket_hit"] = new("적중! {0} / 골드 {1} 획득.", "Hit! {0} paid {1} gold."),
            ["ticket_miss"] = new("미적중: {0}", "Missed {0}. The track keeps the stake."),
            ["meet_complete"] = new("시즌 종료. 마권 적중 {0}/{1}, 최종 골드 {2}.", "Meet complete. Ticket hits {0}/{1}, final gold {2}."),
            ["next_race"] = new("{0}경주: 이전 결과에 따라 배당이 변동했습니다.", "Race {0}: odds moved after the last result."),
            ["race_entry_free"] = new("레이스 참가비 무료", "Free race entry"),
            ["race_data_missing"] = new("{0} 리그 경기 데이터가 DB에 없습니다.", "No {0} race is registered in the database."),
            ["round_cleared"] = new("{0}라운드 통과! {1}골드를 획득했습니다. 다음 목표는 {2}골드입니다.", "Round {0} cleared with {1} gold. The next goal is {2} gold."),
            ["round_failed"] = new("{0}라운드 실패. 획득 골드 {1}/{2}. 새 게임을 시작하세요.", "Round {0} failed. Earned {1}/{2} gold. Start a new run."),
            ["run_failed_title"] = new("라운드 실패", "Round Failed"),
            ["relic_shop"] = new("유물 상점", "Relic Shop"),
            ["relic_inventory"] = new("보유 유물", "Owned"),
            ["refresh_shop"] = new("새로고침", "Refresh"),
            ["owned"] = new("보유 중", "Owned"),
            ["empty_relic_slot"] = new("빈 유물 슬롯", "Empty relic slot"),
            ["buy"] = new("구매", "Buy"),
            ["sell"] = new("판매", "Sell"),
            ["rarity_common"] = new("일반", "Common"),
            ["rarity_rare"] = new("희귀", "Rare"),
            ["rarity_epic"] = new("영웅", "Epic"),
            ["rarity_legendary"] = new("전설", "Legendary"),
            ["relic_bought"] = new("{0} 구매 완료.", "Bought {0}."),
            ["relic_sold"] = new("{0} 판매. 골드 {1} 획득.", "Sold {0} for {1} gold."),
            ["relic_full"] = new("유물은 최대 4개까지 보유할 수 있습니다.", "You can hold up to four relics."),
            ["relic_need_gold"] = new("이 유물을 구매하려면 골드 {0}이 필요합니다.", "You need {0} gold to buy this relic."),
            ["relic_refresh_need_gold"] = new("상점을 새로고침하려면 골드 {0}이 필요합니다.", "You need {0} gold to refresh the shop."),
            ["relic_refreshed"] = new("골드 {0}을 사용해 상점을 갱신했습니다. 다음 비용은 {1}골드입니다.", "Spent {0} gold to refresh the shop. The next refresh costs {1}."),
            ["random_horse_offer"] = new("랜덤 말 조정", "Random Horse Tuning"),
            ["select_horse_offer"] = new("말 선택 조정", "Choose Horse Tuning"),
            ["purchase_complete"] = new("구매 완료", "Purchased"),
            ["stat_offer_need_gold"] = new("능력치 상품을 구매하려면 골드 {0}이 필요합니다.", "You need {0} gold to buy this stat offer."),
            ["stat_offer_bought"] = new("{0}: {1} {2} 적용.", "{0}: {1} {2} applied.")
        };

        public static string Get(string key, bool korean)
        {
            return Entries.TryGetValue(key, out var entry)
                ? korean ? entry.Korean : entry.English
                : key;
        }

        public static string GetBetTypeName(BetType type, bool korean)
        {
            return type switch
            {
                BetType.Win => korean ? "단승식" : "Win",
                BetType.Place => korean ? "연승식" : "Place",
                BetType.Quinella => korean ? "복승식" : "Quinella",
                BetType.Exacta => korean ? "쌍승식" : "Exacta",
                _ => korean ? "마권" : "Ticket"
            };
        }

        public static string GetBetTarget(BetType type, string firstName, string secondName, bool korean)
        {
            return type switch
            {
                BetType.Win => korean ? $"{firstName} 1위" : $"{firstName} must finish 1st.",
                BetType.Place => korean ? $"{firstName} 3위 이내" : $"{firstName} must finish top 3.",
                BetType.Quinella => korean
                    ? $"{firstName} + {secondName} 순서 무관 2위 이내"
                    : $"{firstName} + {secondName} top 2, any order.",
                BetType.Exacta => korean
                    ? $"{firstName} 1위, {secondName} 2위"
                    : $"{firstName} 1st, {secondName} 2nd.",
                _ => string.Empty
            };
        }
    }
}

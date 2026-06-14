using System;

namespace Malatro
{
    public sealed partial class MalatroPrototype
    {
        private void TransitionTo(GamePhase nextPhase, bool refreshUi = true)
        {
            if (phase == nextPhase)
            {
                return;
            }

            phase = nextPhase;
            if (refreshUi)
            {
                MarkUiDirty();
            }
        }

        private void SetLog(string key, params object[] args)
        {
            logKey = key;
            logArgs = args ?? Array.Empty<object>();
        }

        private string GetLog()
        {
            return string.Format(L(logKey), logArgs);
        }

        private string L(string key)
        {
            return MalatroLocalization.Get(key, language == UiLanguage.Korean);
        }

        private enum GamePhase
        {
            Betting,
            Shop,
            Racing,
            Results
        }


        private enum UiLanguage
        {
            Korean,
            English
        }
    }
}

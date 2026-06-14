using UnityEngine;

namespace Malatro
{
    public sealed partial class MalatroPrototype
    {
        private abstract class ScreenController
        {
            protected readonly MalatroPrototype View;

            protected ScreenController(MalatroPrototype view)
            {
                View = view;
            }

            public abstract void BuildRuntime();
            public abstract void BindEditable(Transform screen);
        }

        private sealed class PredictionScreenController : ScreenController
        {
            public PredictionScreenController(MalatroPrototype view) : base(view) { }
            public override void BuildRuntime() => View.BuildPredictionScreen();
            public override void BindEditable(Transform screen) => View.BindEditableBoardScreen(screen, false);
        }

        private sealed class GameSetupScreenController : ScreenController
        {
            public GameSetupScreenController(MalatroPrototype view) : base(view) { }
            public override void BuildRuntime() => View.BuildGameSetupScreen();
            public override void BindEditable(Transform screen) => View.BindEditableGameSetupScreen(screen);
        }

        private sealed class ShopScreenController : ScreenController
        {
            public ShopScreenController(MalatroPrototype view) : base(view) { }
            public override void BuildRuntime() => View.BuildShopScreen();
            public override void BindEditable(Transform screen) => View.BindEditableShopScreen(screen);
        }

        private sealed class RaceScreenController : ScreenController
        {
            public RaceScreenController(MalatroPrototype view) : base(view) { }
            public override void BuildRuntime() => View.BuildRaceScreen();
            public override void BindEditable(Transform screen) => View.BindEditableRaceScreen(screen);
        }

        private sealed class ResultsScreenController : ScreenController
        {
            public ResultsScreenController(MalatroPrototype view) : base(view) { }
            public override void BuildRuntime() => View.BuildResultsScreen();
            public override void BindEditable(Transform screen) => View.BindEditableRaceScreen(screen);
        }

        private sealed class GameOverScreenController : ScreenController
        {
            public GameOverScreenController(MalatroPrototype view) : base(view) { }
            public override void BuildRuntime() => View.BuildGameOverScreen();
            public override void BindEditable(Transform screen) => View.BindEditableGameOverScreen(screen);
        }

        private void InitializeScreenControllers()
        {
            predictionScreenController ??= new PredictionScreenController(this);
            gameSetupScreenController ??= new GameSetupScreenController(this);
            shopScreenController ??= new ShopScreenController(this);
            raceScreenController ??= new RaceScreenController(this);
            resultsScreenController ??= new ResultsScreenController(this);
            gameOverScreenController ??= new GameOverScreenController(this);
        }

        private ScreenController GetActiveScreenController()
        {
            InitializeScreenControllers();
            return phase switch
            {
                GamePhase.GameSetup => gameSetupScreenController,
                GamePhase.Betting => predictionScreenController,
                GamePhase.Shop => shopScreenController,
                GamePhase.Racing => raceScreenController,
                GamePhase.Results => resultsScreenController,
                GamePhase.GameOver => gameOverScreenController,
                _ => predictionScreenController
            };
        }
    }
}

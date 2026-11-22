using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Calavera
{
    public class GameController : MonoBehaviour
    {
        public CardView cardPrefab;
        public RectTransform board;
        public GridLayoutGroup grid;
        public Sprite[] symbols;
        public Text movesText, timeText, pairsText;
        public Button restartButton, menuButton;
        public Mascot mascot;

        MemoryGame game;
        CardView[] cards;
        bool busy, started, finished;
        float elapsed;

        void Start()
        {
            restartButton.onClick.AddListener(() => SceneFader.Go(SceneNames.Game));
            menuButton.onClick.AddListener(() => SceneFader.Go(SceneNames.Menu));
            BuildBoard();
        }

        void BuildBoard()
        {
            int pairs = GameSession.Pairs;
            game = new MemoryGame(pairs, new System.Random());

            int n = game.Deck.Length;
            int cols = pairs >= 10 ? 5 : 4;
            int rows = Mathf.CeilToInt(n / (float)cols);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            board.sizeDelta = new Vector2(
                cols * grid.cellSize.x + (cols - 1) * grid.spacing.x + grid.padding.left + grid.padding.right,
                rows * grid.cellSize.y + (rows - 1) * grid.spacing.y + grid.padding.top + grid.padding.bottom);

            cards = new CardView[n];
            for (int i = 0; i < n; i++)
            {
                int id = game.Deck[i];
                CardView c = Instantiate(cardPrefab, board);
                c.name = "Carta_" + i;
                c.Setup(i, id, symbols[id % symbols.Length], Palette.Simbolos[id % Palette.Simbolos.Length], OnCardClicked);
                cards[i] = c;
            }
            UpdateHud();
        }

        void OnCardClicked(CardView card)
        {
            if (busy || finished) return;

            int other;
            FlipResult result = game.Flip(card.Index, out other);
            switch (result)
            {
                case FlipResult.Ignored:
                    return;
                case FlipResult.FirstCard:
                    started = true;
                    card.Reveal();
                    break;
                case FlipResult.Match:
                    busy = true;
                    card.Reveal();
                    StartCoroutine(ResolveMatch(cards[other], card));
                    break;
                case FlipResult.Mismatch:
                    busy = true;
                    card.Reveal();
                    StartCoroutine(ResolveMismatch(cards[other], card));
                    break;
            }
            UpdateHud();
        }

        IEnumerator ResolveMatch(CardView a, CardView b)
        {
            yield return new WaitForSeconds(0.45f);
            a.Match();
            b.Match();
            mascot.Acierto();

            // Suma +10 monedas y las guarda en Firebase Firestore en tiempo real
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.SumarMonedas(10);
            }

            UpdateHud();
            yield return new WaitForSeconds(0.75f);
            busy = false;
            if (game.IsComplete) StartCoroutine(Finish());
        }

        IEnumerator ResolveMismatch(CardView a, CardView b)
        {
            yield return new WaitForSeconds(0.8f);
            a.Mismatch();
            b.Mismatch();
            mascot.Error();
            yield return new WaitForSeconds(0.95f);
            busy = false;
        }

        IEnumerator Finish()
        {
            finished = true;
            GameSession.LastMoves = game.Moves;
            GameSession.LastSeconds = elapsed;
            GameSession.LastScore = MemoryGame.CalculateScore(game.PairCount, game.Moves, elapsed);
            GameSession.LastStars = MemoryGame.CalculateStars(game.PairCount, game.Moves);
            GameSession.LastIsRecord = GameSession.TrySaveRecord(game.PairCount, GameSession.LastScore);

            // Registra el avance del nivel (6 pares = Nivel 1, más de 6 pares = Nivel 2) + 50 monedas de bono
            if (FirebaseManager.Instance != null)
            {
                int nivelCompletado = (game.PairCount <= 6) ? 1 : 2;
                FirebaseManager.Instance.RegistrarNivelCompletado(nivelCompletado, 50);
            }

            yield return new WaitForSeconds(0.6f);
            SceneFader.Go(SceneNames.Results);
        }

        void Update()
        {
            if (started && !finished)
            {
                elapsed += Time.deltaTime;
                timeText.text = FormatTime(elapsed);
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) SceneFader.Go(SceneNames.Menu);
            if (Input.GetKeyDown(KeyCode.R)) SceneFader.Go(SceneNames.Game);
#endif
        }

        void UpdateHud()
        {
            movesText.text = game.Moves.ToString();
            pairsText.text = game.MatchedPairs + "/" + game.PairCount;
            timeText.text = FormatTime(elapsed);
        }

        public static string FormatTime(float seconds)
        {
            int s = (int)seconds;
            return string.Format("{0:00}:{1:00}", s / 60, s % 60);
        }
    }
}
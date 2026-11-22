using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Calavera
{
    public class ResultsController : MonoBehaviour
    {
        public Text messageText, movesText, timeText, scoreText, bestText;
        public Image[] stars;
        public Button playAgainButton, menuButton;
        public Mascot mascot;

        IEnumerator Start()
        {
            playAgainButton.onClick.AddListener(() => SceneFader.Go(SceneNames.Game));
            menuButton.onClick.AddListener(() => SceneFader.Go(SceneNames.Menu));

            movesText.text = GameSession.LastMoves.ToString();
            timeText.text = GameController.FormatTime(GameSession.LastSeconds);
            scoreText.text = GameSession.LastScore.ToString();
            bestText.text = GameSession.GetBest(GameSession.Pairs).ToString();

            if (GameSession.LastIsRecord) messageText.text = "¡NUEVO RÉCORD!";
            else if (GameSession.LastStars >= 3) messageText.text = "¡Memoria perfecta!";
            else if (GameSession.LastStars == 2) messageText.text = "¡Muy bien!";
            else messageText.text = "¡Sigue practicando!";

            foreach (Image s in stars) s.color = Palette.Gris;

            yield return new WaitForSeconds(0.7f);
            mascot.Acierto();
            for (int i = 0; i < stars.Length && i < GameSession.LastStars; i++)
            {
                stars[i].color = Palette.Oro;
                yield return Pop(stars[i].rectTransform);
            }
        }

        IEnumerator Pop(RectTransform rt)
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float k = t / 0.35f;
                float s = k < 0.6f ? Mathf.Lerp(0.4f, 1.3f, k / 0.6f) : Mathf.Lerp(1.3f, 1f, (k - 0.6f) / 0.4f);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}

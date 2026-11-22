using UnityEngine;
using UnityEngine.UI;

namespace Calavera
{
    public class MenuController : MonoBehaviour
    {
        public Button playButton, difficultyButton, instructionsButton, quitButton;
        public Text difficultyLabel;

        void Start()
        {
            playButton.onClick.AddListener(() => SceneFader.Go(SceneNames.Game));
            instructionsButton.onClick.AddListener(() => SceneFader.Go(SceneNames.Instructions));
            quitButton.onClick.AddListener(Quit);
            difficultyButton.onClick.AddListener(() => { GameSession.NextDifficulty(); RefreshLabel(); });
            RefreshLabel();
        }

        void RefreshLabel()
        {
            difficultyLabel.text = "DIFICULTAD: " + GameSession.DifficultyName(GameSession.Pairs) + " (" + GameSession.Pairs + " pares)";
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

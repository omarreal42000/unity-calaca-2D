using UnityEngine;
using UnityEngine.UI;

namespace Calavera
{
    public class InstructionsController : MonoBehaviour
    {
        public Button backButton;

        void Start() { backButton.onClick.AddListener(() => SceneFader.Go(SceneNames.Menu)); }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) SceneFader.Go(SceneNames.Menu);
#endif
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Calavera
{
    /// <summary>Transición de entrada/salida entre escenas.</summary>
    public class SceneFader : MonoBehaviour
    {
        public CanvasGroup overlay;
        public float duration = 0.35f;
        static SceneFader instance;
        bool leaving;

        void Awake()
        {
            instance = this;
            overlay.alpha = 1f;
            overlay.blocksRaycasts = true;
        }

        IEnumerator Start()
        {
            yield return Fade(1f, 0f);
            overlay.blocksRaycasts = false;
        }

        public static void Go(string scene)
        {
            if (instance != null) { if (!instance.leaving) instance.StartCoroutine(instance.Out(scene)); }
            else SceneManager.LoadScene(scene);
        }

        IEnumerator Out(string scene)
        {
            leaving = true;
            overlay.blocksRaycasts = true;
            yield return Fade(overlay.alpha, 1f);
            SceneManager.LoadScene(scene);
        }

        IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                overlay.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            overlay.alpha = to;
        }
    }
}

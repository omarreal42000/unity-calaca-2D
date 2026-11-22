using UnityEngine;
using UnityEngine.EventSystems;

namespace Calavera
{
    /// <summary>Retroalimentación visual de botones: crecen al pasar el cursor y se hunden al presionar.</summary>
    public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        float target = 1f;
        Vector3 baseScale;
        bool hover;

        void Awake() { baseScale = transform.localScale; }

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; target = 1.06f; }
        public void OnPointerExit(PointerEventData e) { hover = false; target = 1f; }
        public void OnPointerDown(PointerEventData e) { target = 0.95f; }
        public void OnPointerUp(PointerEventData e) { target = hover ? 1.06f : 1f; }
    }
}

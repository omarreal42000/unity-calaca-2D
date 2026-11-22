using UnityEngine;
using UnityEngine.EventSystems;

namespace Calavera
{
    /// <summary>
    /// Calavera protagonista. Su Animator permanece en "Idle" (sin animar) hasta que ocurre un evento:
    /// clic (Saludo), pareja correcta (Acierto) o pareja incorrecta (Error).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class Mascot : MonoBehaviour, IPointerClickHandler
    {
        static readonly int SaludoHash = Animator.StringToHash("Saludo");
        static readonly int AciertoHash = Animator.StringToHash("Acierto");
        static readonly int ErrorHash = Animator.StringToHash("Error");
        Animator animator;

        void Awake() { animator = GetComponent<Animator>(); }

        public void Saludo() { animator.SetTrigger(SaludoHash); }
        public void Acierto() { animator.SetTrigger(AciertoHash); }
        public void Error() { animator.SetTrigger(ErrorHash); }

        public void OnPointerClick(PointerEventData eventData) { Saludo(); }
    }
}

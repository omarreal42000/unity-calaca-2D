using System;
using UnityEngine;
using UnityEngine.UI;

namespace Calavera
{
    /// <summary>
    /// Carta del memorama. Estados del Animator: Idle, Flip, Revealed, Match, Matched, Mismatch, UnFlip.
    /// Ninguna animación se reproduce hasta que el jugador selecciona la carta.
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(Button))]
    public class CardView : MonoBehaviour
    {
        static readonly int FlipHash = Animator.StringToHash("Flip");
        static readonly int MatchHash = Animator.StringToHash("Match");
        static readonly int MismatchHash = Animator.StringToHash("Mismatch");

        public Image symbolImage;
        public int Index { get; private set; }
        public int Id { get; private set; }

        Animator animator;
        Button button;
        Action<CardView> onClick;

        void Awake()
        {
            animator = GetComponent<Animator>();
            button = GetComponent<Button>();
            button.onClick.AddListener(() => { if (onClick != null) onClick(this); });
        }

        public void Setup(int index, int id, Sprite sprite, Color color, Action<CardView> callback)
        {
            Index = index;
            Id = id;
            symbolImage.sprite = sprite;
            symbolImage.color = color;
            onClick = callback;
        }

        public void Reveal() { animator.SetTrigger(FlipHash); }
        public void Match() { animator.SetTrigger(MatchHash); }
        public void Mismatch() { animator.SetTrigger(MismatchHash); }
    }
}

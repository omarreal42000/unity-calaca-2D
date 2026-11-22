using System;

namespace Calavera
{
    public enum FlipResult { Ignored, FirstCard, Match, Mismatch }

    /// <summary>Lógica pura del memorama (sin Unity), por eso se puede probar con pruebas automáticas.</summary>
    public class MemoryGame
    {
        readonly int[] deck;
        readonly bool[] matched;
        int first = -1;

        public int PairCount { get; private set; }
        public int Moves { get; private set; }
        public int MatchedPairs { get; private set; }
        public int[] Deck { get { return deck; } }
        public bool IsComplete { get { return MatchedPairs == PairCount; } }

        public MemoryGame(int pairs, Random rng) : this(BuildDeck(pairs, rng)) { }

        public MemoryGame(int[] deck)
        {
            if (deck == null || deck.Length < 2 || deck.Length % 2 != 0)
                throw new ArgumentException("El mazo debe tener un número par de cartas.");
            this.deck = deck;
            matched = new bool[deck.Length];
            PairCount = deck.Length / 2;
        }

        /// <summary>Crea un mazo con cada id repetido dos veces y lo mezcla (Fisher-Yates).</summary>
        public static int[] BuildDeck(int pairs, Random rng)
        {
            if (pairs < 1) throw new ArgumentOutOfRangeException("pairs");
            var d = new int[pairs * 2];
            for (int i = 0; i < pairs; i++) { d[i * 2] = i; d[i * 2 + 1] = i; }
            for (int i = d.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = d[i]; d[i] = d[j]; d[j] = tmp;
            }
            return d;
        }

        public FlipResult Flip(int index, out int otherIndex)
        {
            otherIndex = -1;
            if (index < 0 || index >= deck.Length || matched[index] || index == first)
                return FlipResult.Ignored;

            if (first < 0)
            {
                first = index;
                return FlipResult.FirstCard;
            }

            Moves++;
            otherIndex = first;
            first = -1;

            if (deck[index] == deck[otherIndex])
            {
                matched[index] = true;
                matched[otherIndex] = true;
                MatchedPairs++;
                return FlipResult.Match;
            }
            return FlipResult.Mismatch;
        }

        public static int CalculateScore(int pairs, int moves, float seconds)
        {
            int extraMoves = Math.Max(0, moves - pairs);
            int score = pairs * 150 - extraMoves * 15 - (int)seconds * 2;
            return Math.Max(0, score);
        }

        public static int CalculateStars(int pairs, int moves)
        {
            if (moves * 2 <= pairs * 3) return 3;
            if (moves * 5 <= pairs * 11) return 2;
            return 1;
        }
    }
}

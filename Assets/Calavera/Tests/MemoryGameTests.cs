using System;
using System.Linq;
using NUnit.Framework;

namespace Calavera.Tests
{
    /// <summary>Pruebas de resultados: Window > General > Test Runner > EditMode > Run All.</summary>
    public class MemoryGameTests
    {
        [Test]
        public void Deck_TieneCadaIdDosVeces()
        {
            int[] deck = MemoryGame.BuildDeck(8, new Random(1));
            Assert.AreEqual(16, deck.Length);
            for (int id = 0; id < 8; id++) Assert.AreEqual(2, deck.Count(d => d == id));
        }

        [Test]
        public void Deck_ConLaMismaSemillaEsIgual()
        {
            CollectionAssert.AreEqual(MemoryGame.BuildDeck(10, new Random(7)), MemoryGame.BuildDeck(10, new Random(7)));
        }

        [Test]
        public void PrimeraCarta_NoCuentaMovimiento()
        {
            var g = new MemoryGame(new[] { 0, 1, 0, 1 });
            int other;
            Assert.AreEqual(FlipResult.FirstCard, g.Flip(0, out other));
            Assert.AreEqual(0, g.Moves);
        }

        [Test]
        public void ParejaCorrecta_DaMatchYSumaMovimiento()
        {
            var g = new MemoryGame(new[] { 0, 1, 0, 1 });
            int other;
            g.Flip(0, out other);
            Assert.AreEqual(FlipResult.Match, g.Flip(2, out other));
            Assert.AreEqual(0, other);
            Assert.AreEqual(1, g.Moves);
            Assert.AreEqual(1, g.MatchedPairs);
        }

        [Test]
        public void ParejaIncorrecta_DaMismatch()
        {
            var g = new MemoryGame(new[] { 0, 1, 0, 1 });
            int other;
            g.Flip(0, out other);
            Assert.AreEqual(FlipResult.Mismatch, g.Flip(1, out other));
            Assert.AreEqual(0, g.MatchedPairs);
        }

        [Test]
        public void MismaCartaDosVeces_SeIgnora()
        {
            var g = new MemoryGame(new[] { 0, 1, 0, 1 });
            int other;
            g.Flip(0, out other);
            Assert.AreEqual(FlipResult.Ignored, g.Flip(0, out other));
        }

        [Test]
        public void CartaYaEmparejada_SeIgnora()
        {
            var g = new MemoryGame(new[] { 0, 1, 0, 1 });
            int other;
            g.Flip(0, out other); g.Flip(2, out other);
            Assert.AreEqual(FlipResult.Ignored, g.Flip(0, out other));
        }

        [Test]
        public void AlEncontrarTodasLasParejas_EstaCompleto()
        {
            var g = new MemoryGame(new[] { 0, 1, 0, 1 });
            int other;
            g.Flip(0, out other); g.Flip(2, out other);
            Assert.IsFalse(g.IsComplete);
            g.Flip(1, out other); g.Flip(3, out other);
            Assert.IsTrue(g.IsComplete);
        }

        [Test]
        public void Puntuacion_Perfecta()
        {
            Assert.AreEqual(1160, MemoryGame.CalculateScore(8, 8, 20f));
        }

        [Test]
        public void Puntuacion_PenalizaMovimientosExtra()
        {
            Assert.AreEqual(1020, MemoryGame.CalculateScore(8, 20, 0f));
        }

        [Test]
        public void Puntuacion_NuncaEsNegativa()
        {
            Assert.AreEqual(0, MemoryGame.CalculateScore(8, 100, 1000f));
        }

        [Test]
        public void Estrellas_SegunMovimientos()
        {
            Assert.AreEqual(3, MemoryGame.CalculateStars(8, 8));
            Assert.AreEqual(3, MemoryGame.CalculateStars(8, 12));
            Assert.AreEqual(2, MemoryGame.CalculateStars(8, 17));
            Assert.AreEqual(1, MemoryGame.CalculateStars(8, 18));
        }
    }
}

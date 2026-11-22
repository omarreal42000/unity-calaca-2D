using UnityEngine;

namespace Calavera
{
    public static class SceneNames
    {
        public const string Menu = "MenuPrincipal";
        public const string Instructions = "Instrucciones";
        public const string Game = "Juego";
        public const string Results = "Resultados";
    }

    /// <summary>Datos que viajan entre escenas.</summary>
    public static class GameSession
    {
        public static int Pairs = 8;
        public static int LastMoves, LastScore, LastStars;
        public static float LastSeconds;
        public static bool LastIsRecord;

        public static string DifficultyName(int pairs)
        {
            return pairs <= 6 ? "FÁCIL" : (pairs >= 10 ? "DIFÍCIL" : "NORMAL");
        }

        public static void NextDifficulty()
        {
            Pairs = Pairs <= 6 ? 8 : (Pairs == 8 ? 10 : 6);
        }

        public static int GetBest(int pairs) { return PlayerPrefs.GetInt("best_" + pairs, 0); }

        public static bool TrySaveRecord(int pairs, int score)
        {
            if (score <= GetBest(pairs)) return false;
            PlayerPrefs.SetInt("best_" + pairs, score);
            PlayerPrefs.Save();
            return true;
        }
    }
}

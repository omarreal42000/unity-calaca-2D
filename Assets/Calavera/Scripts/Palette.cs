using UnityEngine;

namespace Calavera
{
    /// <summary>Paleta del juego. La calavera es negra, por eso todo el entorno usa colores cálidos y vivos que contrastan con ella.</summary>
    public static class Palette
    {
        public static readonly Color Fondo   = Hex("FFC93C"); // ámbar
        public static readonly Color Crema   = Hex("FFF4D6");
        public static readonly Color Magenta = Hex("E6007E");
        public static readonly Color Cian    = Hex("00B8D9");
        public static readonly Color Tinta   = Hex("0B0B0F"); // negro de la calavera
        public static readonly Color Menta   = Hex("B6F2CF");
        public static readonly Color Salmon  = Hex("FFB4AE");
        public static readonly Color Gris    = Hex("C9BFA5");
        public static readonly Color Oro     = Hex("FF8A00");

        public static readonly Color[] Simbolos =
        {
            Hex("1E5BFF"), Hex("12B76A"), Hex("7B3FE4"), Hex("E5322D"), Hex("00B8D9"),
            Hex("FF7A00"), Hex("0F8B8D"), Hex("3730A3"), Hex("C026D3"), Hex("65A30D")
        };

        public static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + hex, out c);
            return c;
        }
    }
}

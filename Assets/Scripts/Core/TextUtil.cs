using System.Globalization;
using System.Text;

namespace MuseoAutista
{
    public static class TextUtil
    {
        /// <summary>
        /// Prepara un texto para la fuente de títulos estilo DOOM (AmazDooM), que solo
        /// tiene letras básicas: pasa a mayúsculas y quita tildes, ñ y signos ¡ ¿.
        /// </summary>
        public static string Doom(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var norm = s.Replace("¡", "").Replace("¿", "").Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (char c in norm)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }
    }
}

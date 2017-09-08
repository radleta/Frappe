using System.Globalization;
using System.Text;

namespace Frappe.Sprites {
    /// <summary>
    /// Generates the selector and the classname based on the filename.
    /// We follow the CSS grammar. Spaces are not allowed.
    /// </summary>
    internal static class CssNameHelper {
        /// <summary>
        /// Generates the selector based on the filename.
        /// </summary>
        /// <param name="filename">Filename to transform</param>
        /// <returns>Transformed filename for selector usage</returns>
        internal static string GenerateSelector(string filename) {
            if (filename != null) {
                // Few more space for the capacity for special characters.
                StringBuilder selector = new StringBuilder(filename.Length + 5);

                // We do not accept space at all
                filename = filename.Replace(' ', '-');

                char? nextCharacter = null;
                if (filename.Length > 1) {
                    nextCharacter = filename[1];
                }

                selector.Append(GetStartingCharacter(filename[0], nextCharacter));
                for (var i = 1; i < filename.Length; i++) {
                    nextCharacter = null;
                    if (i + 1 < filename.Length) {
                        nextCharacter = filename[i + 1];
                    }

                    selector.Append(GetCharacter(filename[i], nextCharacter));
                }

                return selector.ToString();
            }

            return null;
        }

        /// <summary>
        /// Generates the class name based on the filename.
        /// </summary>
        /// <param name="filename">Filename to transform</param>
        /// <returns>Transformed filename for class name usage</returns>
        internal static string GenerateClassName(string filename) {
            if (filename != null) {
                return filename.Replace(' ', '-');
            }

            return null;
        }

        private static string GetStartingCharacter(char c, char? nextCharacter = null) {
            if (c == '-' || c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= 128 && c <= 255)) {
                return c.ToString(CultureInfo.InvariantCulture);
            }

            return GetSimpleEscape(c) ?? GetUnicode(c, nextCharacter);
        }

        private static string GetCharacter(char c, char? nextCharacter = null) {
            if (c == '-' || c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c >= 128 && c <= 255)) {
                return c.ToString(CultureInfo.InvariantCulture);
            }

            return GetSimpleEscape(c) ?? GetUnicode(c, nextCharacter);
        }

        private static string GetUnicode(char c, char? nextCharacter = null) {
            // If our next character is a-fA-F0-9, we need to add a space
            string unicode = "\\" + ((int)c).ToString("x");
            if (nextCharacter.HasValue) {
                if ((nextCharacter >= 'a' && nextCharacter <= 'f') || (nextCharacter >= 'A' && nextCharacter <= 'F') || (nextCharacter >= '0' && nextCharacter <= '9')) {
                    unicode += " ";
                }
            }

            return unicode;
        }

        private static string GetSimpleEscape(char c) {
            if (c > 255 || c == '\n' || c == '\r' || c == '\f' || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || (c >= '0' && c <= '9')) {
                return null;
            }

            return "\\" + c;
        }
    }
}
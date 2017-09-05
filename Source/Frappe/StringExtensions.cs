using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frappe
{
    public static class StringExtensions
    {
        
        #region SplitCommandLine

        /// <summary>
        /// Splits a string the way the command line parser does.
        /// </summary>
        /// <param name="commandLine">The string to extend.</param>
        /// <returns>The arguments</returns>
        /// <remarks>
        /// Ported from: http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
        /// </remarks>
        public static IEnumerable<string> SplitCommandLine(this string commandLine)
        {
            bool inQuotes = false;

            return commandLine.Split(c =>
                {
                    if (c == '\"')
                        inQuotes = !inQuotes;

                    return !inQuotes && c == ' ';
                })
                .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                .Where(arg => !string.IsNullOrEmpty(arg));
        }

        /// <summary>
        /// Splits a string based on a matched char.
        /// </summary>
        /// <param name="str">The string to extend.</param>
        /// <param name="controller">Predicate to determine whether to split or not.</param>
        /// <returns>The split string.</returns>
        /// <remarks>
        /// It may yield some empty strings depending on the situation, but maybe 
        /// that information will be useful in other cases, so I don't remove the 
        /// empty entries in this function.
        /// 
        /// Ported from: http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
        /// </remarks>
        public static IEnumerable<string> Split(this string str,
                                            Func<char, bool> controller)
        {
            if (str != null)
            {
                int nextPiece = 0;

                for (int c = 0; c < str.Length; c++)
                {
                    if (controller(str[c]))
                    {
                        yield return str.Substring(nextPiece, c - nextPiece);
                        nextPiece = c + 1;
                    }
                }

                yield return str.Substring(nextPiece);
            }
        }

        /// <summary>
        /// trim a matching pair of quotes from the start and end of a string. 
        /// It's more fussy than the standard Trim method - it will only trim one 
        /// character from each end, and it will not trim from just one end
        /// </summary>
        /// <param name="input">The string to extend.</param>
        /// <param name="quote">The char to quote.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>
        /// Ported from: http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
        /// </remarks>
        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if ((input.Length >= 2) &&
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }

        #endregion

    }
}

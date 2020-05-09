using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ConfigHandler
{
    /// <summary>
    /// Some Helpers
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Return the content of a collection as a string with separator
        /// </summary>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <param name="trailer"></param>
        /// <param name="countTrailer"></param>
        /// <returns></returns>
        public static string GetEnumerableAsString(IEnumerable list, string separator = ",", string trailer = "", int countTrailer = 0)
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (var item in list)
            {
                if (++count > 1)
                    sb.Append(separator);
                sb.Append(item);
            }
            for (int i = 0; i < countTrailer; i++)
                sb.Append(trailer);
            return sb.ToString();
        }
    }
}

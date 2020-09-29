using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLMerger
{
    public static class Helper
    {
        public static string RemoveTags(string text)
        {
            return text.Substring(1, text.Length - 2);
        }

        public static List<string> SplitMultipleKeys(string text)
        {
            var words = text.Split(",");
            return words.Select(word => RemoveTags(word)).ToList();
        }
    }
}

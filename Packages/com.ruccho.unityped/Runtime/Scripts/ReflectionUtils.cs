using System;
using System.Collections.ObjectModel;

namespace UniTyped.Reflection
{
    public static class TagUtility
    {
        private static ReadOnlyCollection<string> tagNames;
        public static ReadOnlyCollection<string> TagNames => tagNames ??= Array.AsReadOnly(TagData.tagNames);

        public static string GetTagName(Tags tag)
        {
            return TagData.tagNames[(int)tag];
        }
        
        public static string ToTagName(this Tags tag)
        {
            return TagData.tagNames[(int)tag];
        }

        public static bool TryGetTagValue(string tagName, out Tags result)
        {
            int index = Array.IndexOf(TagData.tagNames, tagName);
            result = (Tags)index;
            return index >= 0;
        }
    }
}
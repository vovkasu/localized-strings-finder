using System.Collections.Generic;
using UnityEditor.Localization;
using UnityEngine.Localization;

namespace LocalizedStringsFinder.Editor
{
    public static class LocalizationTableUtils
    {
        public static List<LocalizedStringPair> GetAllLocalizedStringsFromTables()
        {
            var result = new List<LocalizedStringPair>();

            var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();

            foreach (var collection in stringTableCollections)
            {
                var tableName = collection.TableCollectionName;

                foreach (var entry in collection.SharedData.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Key))
                        continue;

                    var withKeyId = new LocalizedString
                    {
                        TableReference = tableName,
                        TableEntryReference = entry.Id
                    };
                    var withKeyName = new LocalizedString
                    {
                        TableReference = tableName,
                        TableEntryReference = entry.Key
                    };

                    result.Add(new LocalizedStringPair
                    {
                        WithKeyId = withKeyId,
                        WithKeyName = withKeyName
                    });
                }
            }

            return result;
        }
    }
}
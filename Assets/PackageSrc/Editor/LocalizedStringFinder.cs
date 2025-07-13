using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace LocalizedStringsFinder.Editor
{
    public static class LocalizedStringFinder
    {
        public static Dictionary<LocalizedStringPair, List<LocalizedStringReference>> MatchReferences(
            List<LocalizedStringPair> allLocalizedStrings, List<LocalizedStringReference> references)
        {
            var result = new Dictionary<LocalizedStringPair, List<LocalizedStringReference>>();

            foreach (var localized in allLocalizedStrings)
            {
                var matches = new List<LocalizedStringReference>();

                foreach (var reference in references)
                {
                    if (IsMatch(localized.WithKeyName, reference.LocalizedString) ||
                        IsMatch(localized.WithKeyId, reference.LocalizedString))
                    {
                        matches.Add(reference);
                    }
                }

                result[localized] = matches;
            }

            return result;
        }

        private static bool IsMatch(LocalizedString a, LocalizedString b)
        {
            // return a.TableEntryReference.Equals( b.TableEntryReference);

            if (a.TableEntryReference.ReferenceType != b.TableEntryReference.ReferenceType)
                return false;

            if (a.TableEntryReference.ReferenceType == TableEntryReference.Type.Name)
            {
                return a.TableEntryReference.Key == b.TableEntryReference.Key;
            }
            if (a.TableEntryReference.ReferenceType == TableEntryReference.Type.Id)
            {
                return a.TableEntryReference.KeyId == b.TableEntryReference.KeyId;
            }
            return true;
        }

        public static IEnumerator<float> FindLocalizedStringReferencesInternal(List<LocalizedStringReference> results)
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            var assetGuids = AssetDatabase.FindAssets("t:Object");

            float allCount = sceneGuids.Length + assetGuids.Length;
            // Search all scenes
            for (var i = 0; i < sceneGuids.Length; i++)
            {
                var sceneGuid = sceneGuids[i];
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var rootObject in rootObjects)
                {
                    FindLocalizedStringInGameObject(rootObject, scenePath, AssetObjType.Scene, results);
                    yield return i / allCount;
                }
            }

            // Search other assets

            for (var i = 0; i < assetGuids.Length; i++)
            {
                var assetGuid = assetGuids[i];
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                if (assetPath.EndsWith(".prefab")) // Search prefabs
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab != null)
                    {
                        FindLocalizedStringInGameObject(prefab, assetPath, AssetObjType.Prefab, results);
                        yield return (i + sceneGuids.Length) / allCount;
                    }
                }
                else // Search other scriptable objects
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    if (asset != null && !(asset is MonoScript))
                    {
                        FindLocalizedStringInAsset(asset, assetPath, results);
                        yield return (i + sceneGuids.Length) / allCount;
                    }
                }
            }
        }

        private static void FindLocalizedStringInGameObject(GameObject obj, string contextPath, AssetObjType assetObjType, List<LocalizedStringReference> results)
        {
            var components = obj.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null) continue;

                var so = new SerializedObject(component);
                var references = FindLocalizedStringsInSerializedObject(so);
                if (references.Count == 0)
                    continue;

                foreach (var reference in references)
                {
                    reference.AssetPath = contextPath;
                    reference.AssetType = assetObjType;
                }
                results.AddRange(references);
            }
        }

        private static List<LocalizedStringReference> FindLocalizedStringsInSerializedObject(SerializedObject so)
        {
            List<LocalizedStringReference> results = new();

            using (SerializedProperty prop = so.GetIterator())
            {
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.Generic && prop.type == nameof(LocalizedString))
                    {
                        var localizedString = prop.GetTargetObject() as LocalizedString;
                        results.Add(new LocalizedStringReference
                        {
                            LocalizedString = localizedString
                        });
                    }
                }
            }

            return results;
        }

        private static void FindLocalizedStringInAsset(Object asset, string contextPath, List<LocalizedStringReference> results)
        {
            var so = new SerializedObject(asset);
            var references = FindLocalizedStringsInSerializedObject(so);
            foreach (var reference in references)
            {
                reference.AssetPath = contextPath;
                reference.AssetType = AssetObjType.Other;
            }
            results.AddRange(references);
        }
    }

    public class LocalizedStringPair
    {
        public LocalizedString WithKeyId;
        public LocalizedString WithKeyName;
    }

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
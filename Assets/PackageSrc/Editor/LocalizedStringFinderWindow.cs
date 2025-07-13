using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalizedStringsFinder.Editor
{
    public class LocalizedStringFinderWindow : EditorWindow
    {
        private ProgressBar _progressBar;
        private Button _searchButton;
        private EditorCoroutine _currentCoroutine;
        private MultiColumnListView _resultsListView;
        private List<LocalizedStringMatchRow> _rows = new();

        [MenuItem("Tools/Localized String Finder")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizedStringFinderWindow>();
            window.titleContent = new GUIContent("Localized String Finder");
            window.minSize = new Vector2(400, 100);
        }


        public void CreateGUI()
        {
#if PACKAGE_SAMPLE
            var basePath = "Assets/PackageSrc";
#else
            var basePath = "Packages/com.vovkasu.localized-strings-finder";
#endif
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{basePath}/Editor/LocalizedStringFinderWindow.uxml");

            if (visualTree == null)
            {
                Debug.LogError("UXML not found. Make sure the path is correct.");
                return;
            }

            VisualElement ui = visualTree.CloneTree();
            rootVisualElement.Add(ui);

            _searchButton = rootVisualElement.Q<Button>("search-button");
            _progressBar = rootVisualElement.Q<ProgressBar>("progress-bar");

            _searchButton.clicked += OnSearchClicked;

            _resultsListView = rootVisualElement.Q<MultiColumnListView>("results-listview");

            _resultsListView.columns.Clear();
            _resultsListView.columns.Add(new Column { title = "TableReference", makeCell = () => new Label(), bindCell = (e, i) => ((Label)e).text = _rows[i].TableReference });
            _resultsListView.columns.Add(new Column { title = "KeyId", makeCell = () => new Label(), bindCell = (e, i) => ((Label)e).text = _rows[i].KeyId.ToString() });
            _resultsListView.columns.Add(new Column { title = "Key", makeCell = () => new Label(), bindCell = (e, i) => ((Label)e).text = _rows[i].Key });
            _resultsListView.columns.Add(new Column { title = "Used Count", makeCell = () => new Label(), bindCell = (e, i) => ((Label)e).text = _rows[i].UsedCount.ToString() });
            _resultsListView.columns.Add(new Column
            {
                title = "Actions",
                makeCell = () =>
                {
                    var button = new Button();
                    return button;
                },
                bindCell = (element, index) =>
                {
                    var button = (Button)element;
                    var row = _rows[index];
        
                    if (row.UsedCount < 1)
                    {
                        button.text = "Delete";
                        button.SetEnabled(true);
                        button.visible = true;
                        button.clicked -= DeleteRow;
        
                        void DeleteRow()
                        {
                            DeleteLocalizationEntry(row);
                            _rows.Remove(row);
                            _resultsListView.Rebuild();
                        }
        
                        button.clicked += DeleteRow;
                    }
                    else
                    {
                        button.SetEnabled(false);
                        button.visible = false;
                    }
                }
            });
        
            _resultsListView.columns.Add(new Column { title = "Used At", makeCell = () => new Label(), bindCell = (e, i) => ((Label)e).text = _rows[i].UsedAt });
        
            _resultsListView.itemsSource = _rows;
        }

        private void DeleteLocalizationEntry(LocalizedStringMatchRow row)
        {
            var table = LocalizationEditorSettings.GetStringTableCollection(row.TableReference);
            table.RemoveEntry(row.KeyId);
            foreach (var tableStringTable in table.StringTables)
            {
                EditorUtility.SetDirty(tableStringTable);
            }
            
            Debug.Log($"Del:{row.Key}");
        }


        private void OnSearchClicked()
        {
            if (_currentCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_currentCoroutine);
            }

            _progressBar.value = 0;
            _progressBar.title = "Progress";
            _searchButton.SetEnabled(false);

            _currentCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(SearchCoroutine());
        }

        private IEnumerator SearchCoroutine()
        {
            List<LocalizedStringReference> output = new();
            using var enumerator = LocalizedStringFinder.FindLocalizedStringReferencesInternal(output);
            var updatePeriod = 10;
            var iterations = updatePeriod;
            while (enumerator.MoveNext())
            {
                _progressBar.value = enumerator.Current;
                if (iterations > 0)
                {
                    iterations--;
                }
                else
                {
                    iterations = updatePeriod;
                    yield return null;
                }
            }

            yield return null;

            _progressBar.value = 1;
            _progressBar.title = $"Done. Found {output.Count} uses.";

            _searchButton.SetEnabled(true);
            var allLocalizedStrings = LocalizationTableUtils.GetAllLocalizedStringsFromTables();
            var matchReferences = LocalizedStringFinder.MatchReferences(allLocalizedStrings, output);

            _rows.Clear();
            foreach (var pair in matchReferences)
            {
                var tableReference = pair.Key.WithKeyId.TableReference;
                var keyId = pair.Key.WithKeyId.TableEntryReference.KeyId;
                var keyName = pair.Key.WithKeyName.TableEntryReference.Key;
                var usedCount = pair.Value.Count;
                var usedAt = string.Join(", ", pair.Value.ConvertAll(assetRef => assetRef.AssetPath));

                _rows.Add(new LocalizedStringMatchRow
                {
                    TableReference = tableReference,
                    KeyId = keyId,
                    Key = keyName,
                    UsedCount = usedCount,
                    UsedAt = usedAt,
                    Source = pair.Key
                });
            }

            _resultsListView.Rebuild();
        }
    }
}

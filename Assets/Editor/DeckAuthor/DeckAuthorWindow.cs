using System;
using System.Collections.Generic;
using MemoryFoyer.Infrastructure.ScriptableObjects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MemoryFoyer.Editor.DeckAuthor
{
    public sealed class DeckAuthorWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Memory Foyer/Deck Author";
        private const string UxmlPath = "Assets/Editor/DeckAuthor/DeckAuthorWindow.uxml";
        private const string UssPath = "Assets/Editor/DeckAuthor/DeckAuthorWindow.uss";

        private readonly List<DeckAsset> _decks = new();

        private ListView _deckList = null!;
        private VisualElement _editorRoot = null!;
        private Label _placeholder = null!;
        private Button _saveButton = null!;

        private SerializedObject? _serializedObject;
        private DeckAsset? _selectedDeck;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            DeckAuthorWindow window = GetWindow<DeckAuthorWindow>();
            window.titleContent = new GUIContent("Deck Author");
            window.minSize = new Vector2(560, 360);
        }

        private void CreateGUI()
        {
            VisualTreeAsset? visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            StyleSheet? styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);

            if (visualTree == null || styleSheet == null)
            {
                rootVisualElement.Add(new Label($"[DeckAuthorWindow] Missing asset — check {UxmlPath} and {UssPath}"));
                return;
            }

            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(styleSheet);

            _deckList = rootVisualElement.Q<ListView>("deck-list");
            _editorRoot = rootVisualElement.Q<VisualElement>("editor-root");
            _placeholder = rootVisualElement.Q<Label>("placeholder");
            _saveButton = rootVisualElement.Q<Button>("save-button");

            _saveButton.clicked += OnSaveClicked;

            _deckList.makeItem = () => new Label();
            _deckList.bindItem = (element, i) =>
            {
                string name = string.IsNullOrEmpty(_decks[i].DisplayName)
                    ? _decks[i].name
                    : _decks[i].DisplayName;
                ((Label)element).text = name;
            };
            _deckList.selectionType = SelectionType.Single;
            _deckList.selectionChanged += OnDeckSelectionChanged;

            ReloadDecks();
        }

        private void ReloadDecks()
        {
            _decks.Clear();

            string[] guids = AssetDatabase.FindAssets("t:DeckAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DeckAsset? asset = AssetDatabase.LoadAssetAtPath<DeckAsset>(path);
                if (asset == null)
                {
                    continue;
                }
                _decks.Add(asset);
            }

            _decks.Sort((a, b) => StringComparer.Ordinal.Compare(a.DeckId, b.DeckId));

            _deckList.itemsSource = _decks;
            _deckList.Rebuild();

            if (_decks.Count == 0)
            {
                _placeholder.text = "No DeckAssets found.";
                _placeholder.style.display = DisplayStyle.Flex;
                _editorRoot.style.display = DisplayStyle.None;
                _saveButton.SetEnabled(false);
            }
            else
            {
                _placeholder.style.display = DisplayStyle.None;
                _editorRoot.style.display = DisplayStyle.None;
                _saveButton.SetEnabled(false);
            }
        }

        private void OnDeckSelectionChanged(IEnumerable<object> selection)
        {
            DeckAsset? picked = null;
            foreach (object item in selection)
            {
                picked = item as DeckAsset;
                break;
            }

            if (picked == null)
            {
                return;
            }

            _selectedDeck = picked;
            _serializedObject = new SerializedObject(_selectedDeck);

            _placeholder.style.display = DisplayStyle.None;
            _editorRoot.style.display = DisplayStyle.Flex;
            _editorRoot.Clear();

            PropertyField displayNameField = new PropertyField();
            displayNameField.bindingPath = "_displayName";
            _editorRoot.Add(displayNameField);

            PropertyField descriptionField = new PropertyField();
            descriptionField.bindingPath = "_description";
            _editorRoot.Add(descriptionField);

            PropertyField newCardsPerDayField = new PropertyField();
            newCardsPerDayField.bindingPath = "_newCardsPerDay";
            _editorRoot.Add(newCardsPerDayField);

            PropertyField cardsField = new PropertyField();
            cardsField.bindingPath = "_cards";
            _editorRoot.Add(cardsField);

            _editorRoot.Bind(_serializedObject);
            _saveButton.SetEnabled(true);
        }

        private void OnSaveClicked()
        {
            if (_serializedObject == null || _selectedDeck == null)
            {
                return;
            }

            _serializedObject.ApplyModifiedProperties();

            if (!DeckAssetValidation.TryValidate(_decks, out string error))
            {
                EditorUtility.DisplayDialog("Deck Author — validation failed", error, "OK");
                return;
            }

            EditorUtility.SetDirty(_selectedDeck);
            AssetDatabase.SaveAssets();
            _deckList.RefreshItems();
            ShowNotification(new GUIContent("Saved"));
        }

        private void OnFocus()
        {
            _serializedObject?.Update();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ScenePickerWindow : EditorWindow
{
    // Dossiers à afficher en premier, dans cet ordre précis — le reste suit après, groupé par dossier
    private static readonly string[] PriorityFolders = { "Assets/_Project/Scenes" };

    private List<string> allScenePaths = new List<string>();
    private ListView listView;
    private ToolbarSearchField searchField;

    [MenuItem("Window/Scene Picker %&s")]
    public static void OpenWindow()
    {
        var wnd = GetWindow<ScenePickerWindow>();
        wnd.titleContent = new GUIContent("Scene Picker");
        wnd.minSize = new Vector2(320, 400);
    }

    public void CreateGUI()
    {
        RefreshSceneList();

        searchField = new ToolbarSearchField();
        searchField.RegisterValueChangedCallback(evt => FilterList(evt.newValue));
        rootVisualElement.Add(searchField);

        listView = new ListView(BuildDisplayItems(allScenePaths), 24, MakeItem, BindItem)
        {
            selectionType = SelectionType.Single
        };
        listView.selectionChanged += OnSelectionChanged;
        rootVisualElement.Add(listView);
        listView.style.flexGrow = 1;
    }

    private void RefreshSceneList()
    {
        var guids = AssetDatabase.FindAssets("t:Scene");
        allScenePaths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
    }

    // Regroupe et trie : dossiers prioritaires d'abord (dans l'ordre donné), puis le reste par dossier
    private List<object> BuildDisplayItems(List<string> paths)
    {
        var items = new List<object>();

        foreach (var folder in PriorityFolders)
        {
            var inFolder = paths.Where(p => p.StartsWith(folder)).OrderBy(p => p).ToList();
            if (inFolder.Count == 0) continue;

            items.Add($"— {folder} —");
            items.AddRange(inFolder);
        }

        var remaining = paths
            .Where(p => !PriorityFolders.Any(f => p.StartsWith(f)))
            .GroupBy(p => System.IO.Path.GetDirectoryName(p))
            .OrderBy(g => g.Key);

        foreach (var group in remaining)
        {
            items.Add($"— {group.Key} —");
            items.AddRange(group.OrderBy(p => p));
        }

        return items;
    }

    private void FilterList(string search)
    {
        var filteredPaths = string.IsNullOrEmpty(search)
            ? allScenePaths
            : allScenePaths.Where(p => p.ToLower().Contains(search.ToLower())).ToList();

        listView.itemsSource = BuildDisplayItems(filteredPaths);
        listView.Rebuild();
    }

    private VisualElement MakeItem() => new Label();

    private void BindItem(VisualElement element, int index)
    {
        var label = (Label)element;
        var item = listView.itemsSource[index];

        if (item is string s && s.StartsWith("—"))
        {
            label.text = s;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new Color(0.6f, 0.6f, 0.6f);
        }
        else
        {
            string path = (string)item;
            label.text = "    " + System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }

    private void OnSelectionChanged(IEnumerable<object> selection)
    {
        var item = selection.FirstOrDefault();
        if (item is not string path || path.StartsWith("—")) return; // ignore le clic sur un en-tête de groupe

        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            bool save = EditorUtility.DisplayDialog(
                "Scène modifiée",
                "La scène actuelle a des modifications non sauvegardées. Sauvegarder avant de changer de scène ?",
                "Sauvegarder", "Ignorer");

            if (save) EditorSceneManager.SaveOpenScenes();
        }

        EditorSceneManager.OpenScene(path);
    }
}
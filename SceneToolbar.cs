using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Overlays;
using UnityEditor.UIElements;

[System.Serializable]
public class SceneToolbarData
{
    public List<SceneAsset> scenes = new();
    public int currentIndex = 0;

    public float buttonWidth = 90f;
}

public static class SceneToolbarStorage
{
    private const string PREF_KEY = "SceneToolbarData";

    public static SceneToolbarData Load()
    {
        if (!EditorPrefs.HasKey(PREF_KEY))
            return new SceneToolbarData();

        string json = EditorPrefs.GetString(PREF_KEY);
        return JsonUtility.FromJson<SceneToolbarData>(json) ?? new SceneToolbarData();
    }

    public static void Save(SceneToolbarData data)
    {
        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(PREF_KEY, json);
    }
}

public class SceneToolbarSettingsWindow : EditorWindow
{
    private SceneToolbarData data;
    private Vector2 scroll;

    public static void ShowWindow()
    {
        var window = GetWindow<SceneToolbarSettingsWindow>("Scene Toolbar");
        window.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        data = SceneToolbarStorage.Load();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Scene Toolbar Settings", EditorStyles.boldLabel);

        GUILayout.Space(5);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < data.scenes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            data.scenes[i] = (SceneAsset)EditorGUILayout.ObjectField(
                data.scenes[i],
                typeof(SceneAsset),
                false
            );

            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                data.scenes.RemoveAt(i);
                Save();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        if (GUILayout.Button("Add Scene", GUILayout.Height(30)))
        {
            data.scenes.Add(null);
        }

        GUILayout.Space(10);

        GUILayout.Label("Button Size", EditorStyles.boldLabel);

        float newWidth = EditorGUILayout.Slider(
            "Width",
            data.buttonWidth,
            50f,
            250f
        );

        if (!Mathf.Approximately(newWidth, data.buttonWidth))
        {
            data.buttonWidth = newWidth;
            Save();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Save", GUILayout.Height(35)))
        {
            Save();
        }
    }

    private void Save()
    {
        SceneToolbarStorage.Save(data);
        SceneToolbar.RefreshToolbar();
    }
}

[EditorToolbarElement(ID, typeof(SceneView))]
public class SceneToolbarElement : VisualElement
{
    public const string ID = "Custom/SceneToolbar";

    private static ToolbarButton[] sceneButtons = new ToolbarButton[3];
    private static ToolbarButton leftButton;
    private static ToolbarButton rightButton;
    private static ToolbarButton settingsButton;

    private static SceneToolbarElement instance;

    public SceneToolbarElement()
    {
        instance = this;

        style.flexDirection = FlexDirection.Row;

        settingsButton = new ToolbarButton(() =>
        {
            SceneToolbarSettingsWindow.ShowWindow();
        })
        {
            text = "⚙"
        };

        Add(settingsButton);

        leftButton = new ToolbarButton(() =>
        {
            var data = SceneToolbarStorage.Load();
            data.currentIndex =
                Mathf.Max(0, data.currentIndex - 1);

            SceneToolbarStorage.Save(data);
            RefreshButtons();
        })
        {
            text = "<"
        };

        Add(leftButton);

        for (int i = 0; i < 3; i++)
        {
            int index = i;

            sceneButtons[i] = new ToolbarButton(() =>
            {
                OpenScene(index);
            });

            Add(sceneButtons[i]);
        }

        rightButton = new ToolbarButton(() =>
        {
            var data = SceneToolbarStorage.Load();

            int maxIndex = Mathf.Max(0, data.scenes.Count - 3);

            data.currentIndex =
                Mathf.Min(maxIndex, data.currentIndex + 1);

            SceneToolbarStorage.Save(data);
            RefreshButtons();
        })
        {
            text = ">"
        };

        Add(rightButton);

        RefreshButtons();
    }

    private static void OpenScene(int localIndex)
    {
        var data = SceneToolbarStorage.Load();

        int realIndex = data.currentIndex + localIndex;

        if (realIndex >= data.scenes.Count)
            return;

        SceneAsset sceneAsset = data.scenes[realIndex];

        if (sceneAsset == null)
            return;

        string path = AssetDatabase.GetAssetPath(sceneAsset);

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }

    public static void RefreshButtons()
    {
        if (instance == null)
            return;

        var data = SceneToolbarStorage.Load();

        for (int i = 0; i < 3; i++)
        {
            int sceneIndex = data.currentIndex + i;

            if (sceneIndex < data.scenes.Count &&
                data.scenes[sceneIndex] != null)
            {
                sceneButtons[i].text =
    data.scenes[sceneIndex].name;

                sceneButtons[i].style.width =
                    data.buttonWidth;

                sceneButtons[i].style.minWidth =
                    data.buttonWidth;

                sceneButtons[i].style.maxWidth =
                    data.buttonWidth;

                sceneButtons[i].style.display =
                    DisplayStyle.Flex;
            }
            else
            {
                sceneButtons[i].style.display =
                    DisplayStyle.None;
            }
        }

        leftButton.SetEnabled(data.currentIndex > 0);

        rightButton.SetEnabled(
            data.currentIndex + 3 < data.scenes.Count
        );
    }
}

[Overlay(typeof(SceneView), "Scene Toolbar")]
public class SceneToolbarOverlay : ToolbarOverlay
{
    public SceneToolbarOverlay()
        : base(SceneToolbarElement.ID)
    {
    }
}

public static class SceneToolbar
{
    public static void RefreshToolbar()
    {
        SceneToolbarElement.RefreshButtons();
    }
}
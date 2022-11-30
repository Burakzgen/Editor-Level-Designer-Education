using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LevelDesignerWindow : EditorWindow
{
    static List<List<string>> folderPrefabs = new List<List<string>>(); // Prefab Klasörler
    static string[] subfolders; // Alt Klasorler

    static int selectedFolder = 0; // Seçili Klasör

    static bool openScenePanel = false; // Sahne Panelini Aç
    static Vector2 scenePanelPosition = Vector2.zero; // Sahne Paneli Pozisyonu

    static Vector2 prefabPanelScroll = Vector2.zero;
    static Vector2 folderPanelScroll = Vector2.zero;

    static Camera sceneCamera;

    static bool addSubFolder = false; // Alt Obje Eklenmesi
    static bool selectedAddFolder = false; // Secili Halde Olusturmasý
    static bool openMousePanel = false; // Mouse Paneli Açma

    static string prefabFolderPath = "Assets/Prefabs";

    [MenuItem("EditorEducation/Level Designer")]
    static void ShowWindow()
    {
        var window = GetWindow<LevelDesignerWindow>();
        window.titleContent = new GUIContent("Level Designer");
        window.Show();
    }
    private void OnGUI()
    {
        GUILayout.Label("LEVEL OLUSTURMA ARACI");
        GUILayout.Label("Options");
        addSubFolder = GUILayout.Toggle(addSubFolder, "Alt Obje Olarak Ekle");
        selectedAddFolder = GUILayout.Toggle(selectedAddFolder, "Seçili Halde Oluþtur");
        openMousePanel = GUILayout.Toggle(openMousePanel, "Mouse Konumunda Paneli Aç");

        GUILayout.Label("Selected Folder: " + prefabFolderPath);
        if (GUILayout.Button("Prefab Klasörünü Seç"))
        {
            SelectedPrefabScene();
        }

        if (prefabFolderPath == "")
        {
            EditorGUILayout.HelpBox("Prefab klasörü assets altýnda olmalýdýr", MessageType.Warning);
        }
        GUILayout.Label("Selected SubFolders");
        for (int i = 0; i < subfolders.Length; i++)
        {
            GUILayout.Label(subfolders[i] + " klasörü " + folderPrefabs[i].Count + " adet prefab bulunduruyor.");
        }
    }
    private void OnEnable()
    {
        LoadPanel();
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneView;
    }
    private void OnInspectorUpdate()
    {
        LoadPanel();
    }
    void LoadPanel()
    {
        SceneView.duringSceneGui -= OnSceneView;
        SceneView.duringSceneGui += OnSceneView;
        sceneCamera = SceneView.GetAllSceneCameras()[0];

        if (sceneCamera == null)
        {
            Debug.LogWarning("Empty Camera");
            return;
        }
        LoadPrefabs();
    }
    void OnSceneView(SceneView scene)
    {
        if (sceneCamera == null)
            return;
        Handles.BeginGUI();

        GUI.Label(new Rect(sceneCamera.scaledPixelWidth - 150, sceneCamera.scaledPixelHeight - 20, 150, 20), "Scene Tool", EditorStyles.toolbarButton);
        if (openScenePanel)
        {
            PrefabPanel();
        }
        Handles.EndGUI();

        Event e = Event.current; // Buradan sahnede týklanan key deðerini alýp TAB olup olmadýðýna bakýyoruz.
        switch (e.type)
        {
            case EventType.KeyUp:
                if (e.keyCode == KeyCode.Tab)
                {
                    openScenePanel = !openScenePanel;
                    if (openMousePanel)
                    {
                        Vector2 tempPos = sceneCamera.ScreenToViewportPoint(Event.current.mousePosition); // ScreenToViewportPoint x ve y içerisinde olduðunun kontrolu. Pnaellerde olmadýðýný kontrolu Aksi halde son deðere göre yapacak.
                        if (tempPos.x > 0 && tempPos.x < 1 && tempPos.y > 0 && tempPos.y < 1)
                        {
                            scenePanelPosition = sceneCamera.ViewportToScreenPoint(tempPos);
                        }
                        else
                        {
                            scenePanelPosition = Vector3.zero;
                        }
                    }
                }
                break;
        }
    }
    void PrefabPanel()
    {
        GUIStyle areaStyle = new GUIStyle(GUI.skin.box);
        areaStyle.alignment = TextAnchor.UpperCenter;


        Rect panelRect;
        if (openMousePanel)
        {
            panelRect = new Rect(scenePanelPosition.x, scenePanelPosition.y, 200, 240); // posiyonlar , geniþlikler ve yükseklikler
        }
        else
        {
            panelRect = new Rect(0, 0, 240, SceneView.currentDrawingSceneView.camera.scaledPixelHeight);
        }
        GUILayout.BeginArea(panelRect, areaStyle);
        //         Vector2 olarak 0-1 yön kontrolu,Horizontal mu Vertical mi kontrolu, görünüm atamasý ,Minumum yükseklik
        folderPanelScroll = GUILayout.BeginScrollView(folderPanelScroll, true, false, GUI.skin.horizontalScrollbar, GUIStyle.none, GUILayout.MinHeight(40));
        selectedFolder = GUILayout.Toolbar(selectedFolder, subfolders);
        GUILayout.EndScrollView();

        prefabPanelScroll = GUILayout.BeginScrollView(prefabPanelScroll, false, true, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.MinHeight(panelRect.height - 40));

        for (int i = 0; i < folderPrefabs[selectedFolder].Count; i++)
        {
            int indexer = folderPrefabs[selectedFolder][i].LastIndexOf("/");
            string names = "";
            if (indexer >= 0)
            {
                names = folderPrefabs[selectedFolder][i].Substring(indexer + 1, folderPrefabs[selectedFolder][i].Length - indexer - 8);
            }
            else
            {
                names = folderPrefabs[selectedFolder][i];
            }
            GUIContent contents = new GUIContent();
            contents.text = names;
            contents.image = GetPrefabImage(folderPrefabs[selectedFolder][i]);
            if (GUILayout.Button(contents))
            {
                CreateObject(folderPrefabs[selectedFolder][i]);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    void CreateObject(string prefabPath)
    {
        Object obje = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject newObje = PrefabUtility.InstantiatePrefab(obje as GameObject, EditorSceneManager.GetActiveScene()) as GameObject;
        if (Selection.activeGameObject != null && addSubFolder)
        {
            newObje.transform.parent = Selection.activeGameObject.transform;
        }
        if (selectedAddFolder)
        {
            Selection.activeGameObject = newObje;
        }
        Undo.RegisterCreatedObjectUndo(newObje, "Yeni eklenen objeyi kaldýr!"); // Geçmiþlerin listesini tutan event="Undo"

        openScenePanel = false;
    }
    Texture2D GetPrefabImage(string prefabPath)
    {
        Object obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); // Istenilen objeyi yakalama iþlemi
        return AssetPreview.GetAssetPreview(obj); // Yakalanan objenin resminin çekilmesi. 
    }
    void LoadPrefabs()
    {
        if (prefabFolderPath == null)
            return;

        folderPrefabs.Clear();
        string[] folderPaths = AssetDatabase.GetSubFolders(prefabFolderPath);
        subfolders = new string[folderPaths.Length]; // Klasorun hepsinin ismini verdiði için "/"'dan sonrasýný almak için bu þekilde yapýyoruz
        for (int i = 0; i < subfolders.Length; i++)
        {
            int slashIndex = folderPaths[i].LastIndexOf('/');
            subfolders[i] = folderPaths[i].Substring(slashIndex + 1);
        }
        foreach (string folder in folderPaths)
        {
            List<string> temp = new List<string>();
            string[] subPrefabs = AssetDatabase.FindAssets("t:prefab", new string[] { folder }); // Prefab tipindeki dosyalarý almak için yazýldý
            foreach (string prefabGUID in subPrefabs)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                temp.Add(folderPath);
            }
            folderPrefabs.Add(temp);
        }
    }
    void SelectedPrefabScene()
    {
        string tempPath = EditorUtility.OpenFolderPanel("Prefab Klasörü", "", "folder"); // Panel açtýrma
        int index = tempPath.IndexOf("/Assets/"); // Kontrol amaçlý bu þekilde yazarýz.
        if (index >= 0)
        {
            prefabFolderPath = tempPath.Substring(index + 1);
            LoadPrefabs();
        }
        else
        {
            prefabFolderPath = "";
        }
    }
}

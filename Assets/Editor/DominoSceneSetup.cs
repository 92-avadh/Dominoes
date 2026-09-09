using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Dominoes.Editor
{
    public static class DominoSceneSetup
    {
        private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Dominoes/Setup UI Toolkit Scene")]
        public static void SetupSceneManual()
        {
            SetupScene(true);
        }

        [MenuItem("Dominoes/Open SampleScene & Fix UI")]
        public static void OpenAndSetupScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            SetupScene(true);
        }

        public static void SetupScene(bool logToConsole = true)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded) return;
            
            // 1. Load PanelSettings asset
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings/DominoesPanelSettings.asset");
            if (panelSettings == null)
            {
                panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
            }
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(400, 844);
                panelSettings.match = 0.5f;
                
                string dir = "Assets/UI/PanelSettings";
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                AssetDatabase.CreateAsset(panelSettings, "Assets/UI/PanelSettings/DominoesPanelSettings.asset");
                AssetDatabase.SaveAssets();
            }
            else
            {
                panelSettings.match = 0.5f;
                EditorUtility.SetDirty(panelSettings);
            }
            
            // 2. Setup Main Camera
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var camGO = new GameObject("Main Camera");
                mainCamera = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }
            
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.01f, 0.52f, 0.78f, 1f); // Tropical blue
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5f;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            mainCamera.depth = -1;
            mainCamera.cullingMask = ~0;
            
            // 3. Create EventSystem if missing
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            
            // 4. Find all existing UI Toolkit GameObjects everywhere in the scene (including children of Canvas)
            #if UNITY_2023_1_OR_NEWER
            var allSceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            #else
            var allSceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            #endif

            var uniqueDocs = new Dictionary<string, GameObject>();
            var objectsToDestroy = new List<GameObject>();

            foreach (var go in allSceneObjects)
            {
                if (go.scene != scene) continue;

                // Loading screen is removed so HomeScreen is immediately displayed and interactive
                if (go.name == "UI_LoadingScreen_UIToolkit" || go.name == "LoadingScreen")
                {
                    objectsToDestroy.Add(go);
                    continue;
                }

                if (go.name == "UI_HomeScreen_UIToolkit" ||
                    go.name == "UI_WaitingScreen_UIToolkit" ||
                    go.name == "UI_GameScreen_UIToolkit")
                {
                    if (!uniqueDocs.ContainsKey(go.name))
                    {
                        uniqueDocs[go.name] = go;
                    }
                    else
                    {
                        objectsToDestroy.Add(go);
                    }
                }
            }

            foreach (var duplicate in objectsToDestroy)
            {
                if (duplicate != null)
                {
                    Object.DestroyImmediate(duplicate);
                }
            }
            
            // 5. Ensure the 3 primary UIDocuments exist at ROOT, are ACTIVE, and are configured
            var homeGO = GetOrCreateRootUIDocument(uniqueDocs, "UI_HomeScreen_UIToolkit", "Assets/UI/Documents/HomeScreen.uxml", panelSettings, 10);
            var waitGO = GetOrCreateRootUIDocument(uniqueDocs, "UI_WaitingScreen_UIToolkit", "Assets/UI/Documents/WaitingScreen.uxml", panelSettings, 8);
            var gameGO = GetOrCreateRootUIDocument(uniqueDocs, "UI_GameScreen_UIToolkit", "Assets/UI/Documents/GameScreen.uxml", panelSettings, 6);
            
            // Ensure all 3 GameObjects are unparented from Canvas and active
            homeGO.transform.SetParent(null, false);
            waitGO.transform.SetParent(null, false);
            gameGO.transform.SetParent(null, false);

            homeGO.transform.localPosition = Vector3.zero;
            waitGO.transform.localPosition = Vector3.zero;
            gameGO.transform.localPosition = Vector3.zero;

            homeGO.SetActive(true);
            waitGO.SetActive(true);
            gameGO.SetActive(true);
            
            // 6. Setup Controllers & Wire Cross-References
            var homeController = homeGO.GetComponent<DominoHomeScreenController>() ?? homeGO.AddComponent<DominoHomeScreenController>();
            var waitUIController = waitGO.GetComponent<DominoWaitingScreenUIToolkitController>() ?? waitGO.AddComponent<DominoWaitingScreenUIToolkitController>();
            var gameController = gameGO.GetComponent<DominoGameScreenUIToolkitController>() ?? gameGO.AddComponent<DominoGameScreenUIToolkitController>();
            
            // Ensure dedicated DominoMatchController exists at root and is active
            var waitingLegacyController = Object.FindAnyObjectByType<DominoWaitingScreenController>(FindObjectsInactive.Include);
            if (waitingLegacyController == null)
            {
                var matchGO = new GameObject("DominoMatchController");
                waitingLegacyController = matchGO.AddComponent<DominoWaitingScreenController>();
            }
            else
            {
                waitingLegacyController.gameObject.name = "DominoMatchController";
                waitingLegacyController.gameObject.transform.SetParent(null, false);
                waitingLegacyController.gameObject.SetActive(true);
            }
            
            // Wire Home Controller
            var homeSO = new SerializedObject(homeController);
            SetSerializedRef(homeSO, "uiDocument", homeGO.GetComponent<UIDocument>());
            SetSerializedRef(homeSO, "waitingScreenController", waitingLegacyController);
            SetSerializedRef(homeSO, "waitingScreenUIToolkitController", waitUIController);
            SetSerializedRef(homeSO, "gameScreenUIToolkitController", gameController);
            homeSO.ApplyModifiedProperties();

            // Wire Waiting Controller
            var waitSO = new SerializedObject(waitUIController);
            SetSerializedRef(waitSO, "uiDocument", waitGO.GetComponent<UIDocument>());
            SetSerializedRef(waitSO, "waitingScreenController", waitingLegacyController);
            SetSerializedRef(waitSO, "homeScreenController", homeController);
            SetSerializedRef(waitSO, "gameScreenUIToolkitController", gameController);
            waitSO.ApplyModifiedProperties();

            // Wire Game Controller
            var gameSO = new SerializedObject(gameController);
            SetSerializedRef(gameSO, "uiDocument", gameGO.GetComponent<UIDocument>());
            SetSerializedRef(gameSO, "waitingScreenController", waitingLegacyController);
            SetSerializedRef(gameSO, "homeScreenController", homeController);
            SetSerializedRef(gameSO, "waitingScreenUIToolkitController", waitUIController);
            gameSO.ApplyModifiedProperties();
            
            // 7. AudioManager
            var audioMgr = Object.FindAnyObjectByType<DominoAudioManager>(FindObjectsInactive.Include);
            if (audioMgr == null)
            {
                var go = new GameObject("DominoAudioManager");
                go.AddComponent<DominoAudioManager>();
            }
            
            // 8. ResponsiveUIManager
            var respMgr = Object.FindAnyObjectByType<ResponsiveUIManager>(FindObjectsInactive.Include);
            if (respMgr == null)
            {
                var go = new GameObject("ResponsiveUIManager");
                respMgr = go.AddComponent<ResponsiveUIManager>();
            }
            
            // 9. KenneyDominoTiles
            var kenneyTiles = Object.FindAnyObjectByType<KenneyDominoTiles>(FindObjectsInactive.Include);
            if (kenneyTiles == null)
            {
                var go = new GameObject("KenneyDominoTiles");
                go.AddComponent<KenneyDominoTiles>();
            }
            
            // 10. Disable old Canvas and legacy uGUI objects so they don't intercept or conflict
            DisableLegacyUGUI();
            
            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.SaveScene(scene);
            }
            
            if (logToConsole)
            {
                Debug.Log("<color=green>[DominoSceneSetup] Scene setup complete! UI Toolkit HomeScreen is now 100% active, visible, and ready to play.</color>");
            }
        }
        
        private static void SetSerializedRef(SerializedObject so, string propertyName, Object targetRef)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = targetRef;
            }
        }

        private static GameObject GetOrCreateRootUIDocument(Dictionary<string, GameObject> existingMap, string name, string uxmlPath, PanelSettings panelSettings, int sortingOrder)
        {
            GameObject go;
            if (!existingMap.TryGetValue(name, out go) || go == null)
            {
                go = new GameObject(name);
                existingMap[name] = go;
            }
            
            go.transform.SetParent(null, false);
            var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            doc.sortingOrder = sortingOrder;
            
            var sourceAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (sourceAsset != null)
            {
                doc.visualTreeAsset = sourceAsset;
            }
            else
            {
                Debug.LogError($"[DominoSceneSetup] Could not load UXML: {uxmlPath}");
            }
            
            return go;
        }
        
        private static void DisableLegacyUGUI()
        {
            string[] legacyNames = { "Canvas", "HomeBackground", "PlayDominoesButton", 
                                     "CountdownText", "Leave", "BoardArea", "PlayerBottomCard",
                                     "WaitingTitle", "LeaveWaitingButton", "PlayerCountText", "LoadingScreen" };
            
            // Note: EntryScreen is NOT disabled because DominoWaitingScreenController
            // and DominoMatchManager live there and are needed for game logic at runtime.
            // The Canvas parent being disabled prevents any legacy UI from rendering.
            
            foreach (var name in legacyNames)
            {
                #if UNITY_2023_1_OR_NEWER
                var objs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                #else
                var objs = Resources.FindObjectsOfTypeAll<GameObject>();
                #endif
                foreach (var obj in objs)
                {
                    if (obj != null && obj.name == name)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }
    }
}
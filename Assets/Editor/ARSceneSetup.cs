using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using YouseiAR.AR;
using YouseiAR.Missions;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem.XR;
using YouseiAR.Fairy;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using System.Linq;

namespace YouseiAR.Editor
{
    [InitializeOnLoad]
    public class ARSceneSetup
    {
        static ARSceneSetup()
        {
            EditorApplication.delayCall += CheckAndFixScene;
        }

        private static void CheckAndFixScene()
        {
            // Only run if we are in the correct scene to avoid messing up other scenes
            // For now, checks if the scene name is "yousei" or if it is an empty untitled scene
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "yousei") return;

            if (Object.FindObjectOfType<ARSession>() == null || Object.FindObjectOfType<MissionManager>() == null)
            {
                Debug.Log("Detected missing AR/Game components. Auto-fixing scene...");
                SetupFullScene();
            }
        }

        [MenuItem("Tools/YouSei/Setup Full Game Scene")]
        public static void SetupFullScene()
        {
            SetupARCore();
            SetupGameSystem();
            SetupARCore();
            SetupGameSystem();
            SetupGameSystem();
            SetupUI();
            SetupProjectSettings();
            SetupXRLoaders();
            
            Debug.Log("Scene Setup Complete! 'AR Session', 'XR Origin', 'UI', and 'Fairy Spawner' have been created.");
        }

        private static void SetupProjectSettings()
        {
            // 1. Force OpenGLES3 (ARCore often has issues with Vulkan on Unity)
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 
            });

            // 2. Set Min SDK Version to 24 (Android 7.0) for ARCore
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            }
            
            // 3. Disable Multithreaded Rendering for safety (can cause black screen on some devices)
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, false);

            Debug.Log("Project Settings Updated: Forced OpenGLES3, MinSDK 24, MT Rendering Off");
        }

        private static void SetupXRLoaders()
        {
            // Get Settings for Android
            var buildTargetGroup = BuildTargetGroup.Android;
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            
            if (settings == null)
            {
                // Try to get asset
                var settingsAsset = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                if (settingsAsset != null)
                {
                    settings = settingsAsset.SettingsForBuildTarget(buildTargetGroup);
                }
            }

            if (settings == null)
            {
                Debug.LogError("XR General Settings not found. Please verify XR Plug-in Management is installed/initialized.");
                return;
            }

            var manager = settings.Manager;
            if (manager == null)
            {
                // This shouldn't happen if initialized correctly, but just in case
                Debug.LogError("XR Manager Settings is null for Android.");
                return;
            }

            // Load ARCoreLoader Asset
            var arCoreLoader = AssetDatabase.LoadAssetAtPath<XRLoader>("Assets/XR/Loaders/ARCoreLoader.asset");
            if (arCoreLoader == null)
            {
                Debug.LogError("ARCore Loader Asset not found at Assets/XR/Loaders/ARCoreLoader.asset");
                return;
            }

            // Check if already assigned
            if (!manager.activeLoaders.Contains(arCoreLoader))
            {
#if UNITY_2020_2_OR_NEWER
                manager.TryAddLoader(arCoreLoader);
#else
                // Older API might differ, but assuming 2021+ based on packages
                if (manager.TryAddLoader(arCoreLoader)) {
                    Debug.Log("Enabled ARCoreLoader for Android.");
                } else {
                    Debug.LogWarning("Failed to add ARCoreLoader.");
                }
#endif
                EditorUtility.SetDirty(manager);
                AssetDatabase.SaveAssets();
                Debug.Log("ARCore Loader enabled in XR Plug-in Management.");
            }
            else
            {
                Debug.Log("ARCore Loader is already enabled.");
            }
        }

        private static void SetupARCore()
        {
            // 0. Ensure Resources folder for generated assets
            if (!System.IO.Directory.Exists(Application.dataPath + "/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // 1. Check/Create AR Session
            var arSession = Object.FindObjectOfType<ARSession>();
            if (arSession == null)
            {
                GameObject sessionGO = new GameObject("AR Session");
                arSession = sessionGO.AddComponent<ARSession>();
                sessionGO.AddComponent<ARInputManager>();
                Undo.RegisterCreatedObjectUndo(sessionGO, "Create AR Session");
            }

            // 2. Check/Create XR Origin
            var xrOrigin = Object.FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                GameObject originGO = new GameObject("XR Origin");
                xrOrigin = originGO.AddComponent<XROrigin>();
                Undo.RegisterCreatedObjectUndo(originGO, "Create XR Origin");

                GameObject cameraOffset = new GameObject("Camera Offset");
                cameraOffset.transform.SetParent(originGO.transform, false);
                xrOrigin.CameraFloorOffsetObject = cameraOffset;

                Camera mainCam = Camera.main;
                if (mainCam == null)
                {
                    GameObject camGO = new GameObject("Main Camera");
                    mainCam = camGO.AddComponent<Camera>();
                    camGO.tag = "MainCamera";
                }
                
                // Ensure camera is child of offset
                if (mainCam.transform.parent != cameraOffset.transform)
                {
                    mainCam.transform.SetParent(cameraOffset.transform, false);
                }
                
                mainCam.transform.localPosition = Vector3.zero;
                mainCam.transform.localRotation = Quaternion.identity;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = Color.black;

                // Essential AR Comps on Camera
                if (!mainCam.GetComponent<ARCameraManager>()) mainCam.gameObject.AddComponent<ARCameraManager>();
                if (!mainCam.GetComponent<ARCameraBackground>()) mainCam.gameObject.AddComponent<ARCameraBackground>();
                if (!mainCam.GetComponent<TrackedPoseDriver>()) mainCam.gameObject.AddComponent<TrackedPoseDriver>();
                
                // Add ColorMissionCondition to Camera (requires ARCameraManager)
                if (!mainCam.GetComponent<ColorMissionCondition>())
                {
                    mainCam.gameObject.AddComponent<ColorMissionCondition>();
                }

                xrOrigin.Camera = mainCam;
                
                // Managers on Origin
                if (!originGO.GetComponent<ARPlaneManager>()) originGO.AddComponent<ARPlaneManager>();
                if (!originGO.GetComponent<ARRaycastManager>()) originGO.AddComponent<ARRaycastManager>();
                
                // Assign Plane Prefab
                var planeManager = originGO.GetComponent<ARPlaneManager>();
                if (planeManager.planePrefab == null)
                {
                   planeManager.planePrefab = CreatePlanePrefab();
                }
            }

            // 3. Fairy Spawner
            var spawner = Object.FindObjectOfType<ARFairySpawner>();
            if (spawner == null)
            {
                spawner = xrOrigin.gameObject.AddComponent<ARFairySpawner>();
                Undo.RegisterCreatedObjectUndo(spawner, "Add Fairy Spawner");
            }

            // Assign Fairy Placeholder if missing
            SerializedObject spawnerSO = new SerializedObject(spawner);
            if (spawnerSO.FindProperty("fairyPrefab").objectReferenceValue == null)
            {
                spawnerSO.FindProperty("fairyPrefab").objectReferenceValue = CreateFairyPlaceholder();
            }

            // Connect Spawner dependencies
            spawnerSO.FindProperty("planeManager").objectReferenceValue = xrOrigin.GetComponent<ARPlaneManager>();
            spawnerSO.FindProperty("raycastManager").objectReferenceValue = xrOrigin.GetComponent<ARRaycastManager>();
            spawnerSO.ApplyModifiedProperties();
            
            // Connect ColorMissionCondition dependencies
            var colorCond = xrOrigin.Camera.GetComponent<ColorMissionCondition>();
            if (colorCond != null)
            {
                SerializedObject colorSO = new SerializedObject(colorCond);
                // We will link MissionManager later
                colorSO.ApplyModifiedProperties();
            }
        }

        private static void SetupGameSystem()
        {
            GameObject systemGO = GameObject.Find("GameSystem");
            if (systemGO == null)
            {
                systemGO = new GameObject("GameSystem");
                Undo.RegisterCreatedObjectUndo(systemGO, "Create GameSystem");
            }

            var missionManager = systemGO.GetComponent<MissionManager>();
            if (missionManager == null)
            {
                missionManager = systemGO.AddComponent<MissionManager>();
            }

            // Add Default Missions
            SerializedObject mmSO = new SerializedObject(missionManager);
            var poolProp = mmSO.FindProperty("missionPool");
            if (poolProp.arraySize == 0)
            {
                poolProp.arraySize = 3;
                
                var m1 = poolProp.GetArrayElementAtIndex(0);
                m1.FindPropertyRelative("id").stringValue = "m_color_green";
                m1.FindPropertyRelative("type").enumValueIndex = (int)MissionType.ColorGreen;
                m1.FindPropertyRelative("threshold").floatValue = 0.15f;
                m1.FindPropertyRelative("description").stringValue = "緑色のものを映そう！";

                var m2 = poolProp.GetArrayElementAtIndex(1);
                m2.FindPropertyRelative("id").stringValue = "m_color_sky";
                m2.FindPropertyRelative("type").enumValueIndex = (int)MissionType.SkyBlue;
                m2.FindPropertyRelative("threshold").floatValue = 0.15f;
                m2.FindPropertyRelative("description").stringValue = "青空を探そう！";

                var m3 = poolProp.GetArrayElementAtIndex(2);
                m3.FindPropertyRelative("id").stringValue = "m_smile";
                m3.FindPropertyRelative("type").enumValueIndex = (int)MissionType.Smile;
                m3.FindPropertyRelative("threshold").floatValue = 0.7f;
                m3.FindPropertyRelative("description").stringValue = "妖精に笑顔を見せよう！";
            }
            mmSO.ApplyModifiedProperties();

            // Link Mission Manager to Color Condition
            var xrOrigin = Object.FindObjectOfType<XROrigin>();
            if (xrOrigin && xrOrigin.Camera)
            {
                var colorCond = xrOrigin.Camera.GetComponent<ColorMissionCondition>();
                if (colorCond)
                {
                    SerializedObject ccSO = new SerializedObject(colorCond);
                    ccSO.FindProperty("missionManager").objectReferenceValue = missionManager;
                    ccSO.ApplyModifiedProperties();
                }
            }
        }

        private static void SetupUI()
        {
            // Canvas
            var canvasGO = GameObject.Find("UI Canvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("UI Canvas");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGO.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create UI Canvas");
            }
            
            // EventSystem
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }

            // UI Panel
            var panelTransform = canvasGO.transform.Find("MissionPanel");
            GameObject panelGO;
            if (panelTransform == null)
            {
                panelGO = new GameObject("MissionPanel");
                panelGO.transform.SetParent(canvasGO.transform, false);
                var rect = panelGO.AddComponent<RectTransform>();
                
                // Top Anchor
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.anchoredPosition = new Vector2(0, -50);
                rect.sizeDelta = new Vector2(-40, 150);
                
                var img = panelGO.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.5f);
            }
            else
            {
                panelGO = panelTransform.gameObject;
            }

            // Mission Text
            var textTransform = panelGO.transform.Find("MissionText");
            Text missionTextComp;
            if (textTransform == null)
            {
                var textGO = new GameObject("MissionText");
                textGO.transform.SetParent(panelGO.transform, false);
                missionTextComp = textGO.AddComponent<Text>();
                missionTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                missionTextComp.alignment = TextAnchor.MiddleCenter;
                missionTextComp.color = Color.white;
                missionTextComp.fontSize = 24;
                missionTextComp.rectTransform.anchoredPosition = new Vector2(0, 30);
                missionTextComp.rectTransform.sizeDelta = new Vector2(0, 50);
                missionTextComp.rectTransform.anchorMin = Vector2.zero;
                missionTextComp.rectTransform.anchorMax = Vector2.one;
            }
            else
            {
                missionTextComp = textTransform.GetComponent<Text>();
            }

            // Slider
            var sliderTransform = panelGO.transform.Find("ProgressSlider");
            Slider sliderComp;
            if (sliderTransform == null)
            {
                // Create basic slider structure
                var sliderGO = new GameObject("ProgressSlider");
                sliderGO.transform.SetParent(panelGO.transform, false);
                sliderComp = sliderGO.AddComponent<Slider>();
                var sliderRect = sliderGO.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0.1f, 0.2f);
                sliderRect.anchorMax = new Vector2(0.9f, 0.4f);
                sliderRect.sizeDelta = Vector2.zero;

                // Background
                var bgGO = new GameObject("Background");
                bgGO.transform.SetParent(sliderGO.transform, false);
                var bgImg = bgGO.AddComponent<Image>();
                bgImg.color = Color.gray;
                bgGO.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                bgGO.GetComponent<RectTransform>().anchorMax = Vector2.one;

                // Fill Area
                var fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(sliderGO.transform, false);
                fillArea.AddComponent<RectTransform>().anchorMin = Vector2.zero;
                fillArea.GetComponent<RectTransform>().anchorMax = Vector2.one;

                // Fill
                var fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                var fillImg = fill.AddComponent<Image>();
                fillImg.color = Color.green;
                fill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                fill.GetComponent<RectTransform>().anchorMax = Vector2.one;

                sliderComp.targetGraphic = bgImg;
                sliderComp.fillRect = fill.GetComponent<RectTransform>();
                sliderComp.direction = Slider.Direction.LeftToRight;
            }
            else
            {
                sliderComp = sliderTransform.GetComponent<Slider>();
            }

            // Binder
            var binder = canvasGO.GetComponent<MissionUIBinder>();
            if (binder == null)
            {
                binder = canvasGO.AddComponent<MissionUIBinder>();
            }

            SerializedObject binderSO = new SerializedObject(binder);
            binderSO.FindProperty("missionManager").objectReferenceValue = Object.FindObjectOfType<MissionManager>();
            binderSO.FindProperty("missionText").objectReferenceValue = missionTextComp;
            binderSO.FindProperty("progressBar").objectReferenceValue = sliderComp;
            binderSO.ApplyModifiedProperties();
        }

        private static GameObject CreatePlanePrefab()
        {
            var prefabPath = "Assets/Resources/ARPlaneDebug.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            var go = new GameObject("ARPlaneDebug");
            go.AddComponent<ARPlane>();
            go.AddComponent<ARPlaneMeshVisualizer>();
            
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default")); // Simple semi-transparent look
            renderer.material.color = new Color(1, 0.9f, 0, 0.3f); // Yellowish tint

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.material.color = Color.yellow;
            
            return PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
        }

        private static GameObject CreateFairyPlaceholder()
        {
            var prefabPath = "Assets/Resources/FairyPlaceholder.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            var go = new GameObject("FairyPlaceholder");
            
            // Visuals
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(go.transform, false);
            capsule.transform.localPosition = new Vector3(0, 0.5f, 0); // Stand on ground
            capsule.transform.localScale = Vector3.one * 0.3f;
            
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.magenta;
            capsule.GetComponent<Renderer>().material = mat;

            // Wings (simple quads)
            var wingL = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingL.transform.SetParent(capsule.transform, false);
            wingL.transform.localPosition = new Vector3(-0.5f, 0.5f, -0.2f);
            wingL.transform.localRotation = Quaternion.Euler(0, -30, 0);
            
            var wingR = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingR.transform.SetParent(capsule.transform, false);
            wingR.transform.localPosition = new Vector3(0.5f, 0.5f, -0.2f);
            wingR.transform.localRotation = Quaternion.Euler(0, 30, 0);

            // Essential Scripts
            go.AddComponent<Animator>(); // Required by FairyAvatar
            go.AddComponent<FairyAvatar>();
            go.AddComponent<FairyInteract>();
            go.AddComponent<FairyLookAtCamera>();
            go.AddComponent<BoxCollider>().size = Vector3.one; // For interaction

            return PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
        }
    }
}

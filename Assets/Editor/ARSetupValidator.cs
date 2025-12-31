using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using UnityEditor.XR.ARCore;
using System.Linq;

namespace YouseiAR.Editor
{
    [InitializeOnLoad]
    public static class ARSetupValidator
    {
        static ARSetupValidator()
        {
            EditorApplication.delayCall += ValidateAndFix;
        }

        [MenuItem("Tools/YouSei/Force Fix AR Settings")]
        public static void ValidateAndFix()
        {
            // 1. Ensure ARCore Loader is in XR Plugin Management
            FixXRLoader();

            // 2. Ensure OpenGLES3
            FixGraphicsAPI();

            Debug.Log("AR Setup Validation Run Complete.");
        }

        private static void FixXRLoader()
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings == null) return;

            var manager = settings.Manager;
            if (manager == null) return;

            // Find valid loaders
            var loaders = AssetDatabase.FindAssets("t:ARCoreLoader");
            if (loaders.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(loaders[0]);
                var loader = AssetDatabase.LoadAssetAtPath<XRLoader>(path);
                
                if (loader != null && !manager.activeLoaders.Contains(loader))
                {
                    if (manager.TryAddLoader(loader))
                    {
                        Debug.Log("Validation: Added missing ARCoreLoader.");
                        EditorUtility.SetDirty(manager);
                        EditorUtility.SetDirty(settings);
                    }
                }
            }
        }

        private static void FixGraphicsAPI()
        {
            // No strict check, just force it
            if (PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0] != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                    UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 
                });
                Debug.Log("Validation: Forced OpenGLES3.");
            }
        }
    }
}

using UnityEngine;
using UnityEditor;
using YouseiAR.Fairy;
using YouseiAR.AR;
using Unity.XR.CoreUtils;

namespace YouseiAR.Editor
{
    public class FairyMaker
    {
        [MenuItem("Tools/YouSei/Generate Cute Fairy Model")]
        public static void GenerateAndAssign()
        {
            // 1. Create Materials with Safe Shaders
            var bodyShader = Shader.Find("Mobile/Diffuse");
            if (!bodyShader) bodyShader = Shader.Find("Legacy Shaders/Diffuse"); // Fallback
            var bodyMat = new Material(bodyShader);
            bodyMat.color = new Color(1f, 0.9f, 0.4f); // Light Gold
            AssetDatabase.CreateAsset(bodyMat, "Assets/Resources/FairyBodyMat.mat");

            var wingShader = Shader.Find("Sprites/Default"); // Very safe for transparent
            var wingMat = new Material(wingShader);
            wingMat.color = new Color(0.5f, 0.8f, 1f, 0.6f); // Cyan Transp
            AssetDatabase.CreateAsset(wingMat, "Assets/Resources/FairyWingMat.mat");

            // 2. Create Object Structure
            var root = new GameObject("CuteFairy");
            
            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * 0.2f;
            body.GetComponent<Renderer>().material = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>()); // Remove collider from visual parts

            // Eyes
            var eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeL.transform.SetParent(body.transform, false);
            eyeL.transform.localPosition = new Vector3(-0.15f, 0.1f, 0.35f);
            eyeL.transform.localScale = Vector3.one * 0.15f;
            eyeL.GetComponent<Renderer>().material.color = Color.black;
            Object.DestroyImmediate(eyeL.GetComponent<Collider>());

            var eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeR.transform.SetParent(body.transform, false);
            eyeR.transform.localPosition = new Vector3(0.15f, 0.1f, 0.35f);
            eyeR.transform.localScale = Vector3.one * 0.15f;
            eyeR.GetComponent<Renderer>().material.color = Color.black;
            Object.DestroyImmediate(eyeR.GetComponent<Collider>());

            // Wings Root
            var wingsRoot = new GameObject("WingsRoot");
            wingsRoot.transform.SetParent(body.transform, false);
            wingsRoot.transform.localPosition = new Vector3(0, 0.1f, -0.2f);

            // Wing L
            var wingL = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingL.name = "WingL";
            wingL.transform.SetParent(wingsRoot.transform, false);
            wingL.transform.localPosition = new Vector3(-0.6f, 0, 0); 
            wingL.transform.localRotation = Quaternion.Euler(0, 0, 0);
            wingL.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
            wingL.GetComponent<Renderer>().material = wingMat;
            Object.DestroyImmediate(wingL.GetComponent<Collider>());

            // Wing R
            var wingR = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingR.name = "WingR";
            wingR.transform.SetParent(wingsRoot.transform, false);
            wingR.transform.localPosition = new Vector3(0.6f, 0, 0);
            wingR.transform.localRotation = Quaternion.Euler(0, 0, 0);
            wingR.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
            wingR.GetComponent<Renderer>().material = wingMat;
            Object.DestroyImmediate(wingR.GetComponent<Collider>());

            // Particle System
            var pSysObj = new GameObject("Sparkles");
            pSysObj.transform.SetParent(root.transform, false);
            var ps = pSysObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startSize = 0.05f;
            main.startLifetime = 1.0f;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Color.yellow);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 10f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            var renderer = pSysObj.GetComponent<ParticleSystemRenderer>();
            // Create Particle Texture
            var texture = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                    float alpha = Mathf.Clamp01(1.0f - (dist / 16.0f));
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            texture.Apply();
            AssetDatabase.CreateAsset(texture, "Assets/Resources/FairyParticle.asset");

            var particleShader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (!particleShader) particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended"); // Fallback
            var particleMat = new Material(particleShader);
            particleMat.mainTexture = texture;
            AssetDatabase.CreateAsset(particleMat, "Assets/Resources/FairyParticleMat.mat");

            renderer.material = particleMat;

            // 3. Components
            root.AddComponent<Animator>(); // Required by FairyAvatar
            root.AddComponent<FairyAvatar>();
            root.AddComponent<FairyInteract>();
            root.AddComponent<FairyLookAtCamera>();
            var col = root.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = Vector3.one * 0.3f;

            // Motion
            var motion = root.AddComponent<SimpleFairyMotion>();
            // Assign wings
            SerializedObject so = new SerializedObject(motion);
            so.FindProperty("wingL").objectReferenceValue = wingL.transform;
            so.FindProperty("wingR").objectReferenceValue = wingR.transform;
            so.ApplyModifiedProperties();

            // 4. Save Prefab
            var path = "Assets/Resources/CuteFairy.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
            GameObject.DestroyImmediate(root);

            Debug.Log("Created Cute Fairy Prefab at " + path);

            // 5. Assign to Spawner
            var spawner = Object.FindObjectOfType<ARFairySpawner>();
            if (spawner != null)
            {
                SerializedObject spawnerSO = new SerializedObject(spawner);
                spawnerSO.FindProperty("fairyPrefab").objectReferenceValue = prefab;
                spawnerSO.ApplyModifiedProperties();
                Debug.Log("Assigned Cute Fairy to ARFairySpawner!");
            }
            else
            {
                Debug.LogWarning("ARFairySpawner not found in scene.");
            }
        }
    }
}

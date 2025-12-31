using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace YouseiAR.AR
{
    /// <summary>
    /// 平面検出後に妖精プレハブを自動スポーンし、最初に見つかった平面に追従させる。
    /// </summary>
    public class ARFairySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject fairyPrefab;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private bool autoSpawnOnFirstPlane = true;

        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private GameObject spawnedFairy;

        private void OnEnable()
        {
            Debug.Log("[ARFairySpawner] OnEnable called.");

            if (planeManager != null)
            {
                planeManager.planesChanged += OnPlanesChanged;
            }
            else
            {
                Debug.LogWarning("[ARFairySpawner] PlaneManager is not assigned!");
            }

            if (raycastManager == null) Debug.LogWarning("[ARFairySpawner] RaycastManager is not assigned!");
            if (fairyPrefab == null) Debug.LogWarning("[ARFairySpawner] FairyPrefab is not assigned!");
        }

        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.planesChanged -= OnPlanesChanged;
            }
        }

        private void Update()
        {
            if (!autoSpawnOnFirstPlane || spawnedFairy != null)
            {
                return;
            }

            TrySpawnAtScreenCenter();
        }

        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            if (!autoSpawnOnFirstPlane || spawnedFairy != null)
            {
                return;
            }

            Debug.Log($"[ARFairySpawner] Planes Changed. Added: {args.added.Count}, Updated: {args.updated.Count}");

            if (args.added != null && args.added.Count > 0)
            {
                Debug.Log("[ARFairySpawner] New plane detected, trying to spawn...");
                TrySpawnAtPlane(args.added[0]);
            }
        }

        private void TrySpawnAtScreenCenter()
        {
            if (raycastManager == null || fairyPrefab == null)
            {
                return;
            }

            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon) && hits.Count > 0)
            {
                Debug.Log($"[ARFairySpawner] Raycast hit {hits.Count} planes at center.");
                SpawnFairy(hits[0].pose, hits[0].trackable as ARPlane);
            }
        }

        private void TrySpawnAtPlane(ARPlane plane)
        {
            if (raycastManager == null || fairyPrefab == null || plane == null)
            {
                Debug.LogError("[ARFairySpawner] Cannot spawn at plane: References missing.");
                return;
            }

            var center = plane.center;
            // Note: ARPlane uses local space for center in newer versions? Need to verify. 
            // Usually TransformPoint(center) is correct for converting local center to world.
            var pose = new Pose(plane.transform.TransformPoint(center), plane.transform.rotation);
            Debug.Log($"[ARFairySpawner] Spawning at detected plane center: {pose.position}");
            SpawnFairy(pose, plane);
        }

        private void SpawnFairy(Pose pose, ARPlane plane)
        {
            if (spawnedFairy != null)
            {
                return;
            }

            Debug.Log($"[ARFairySpawner] Instantiating Fairy at {pose.position}");
            spawnedFairy = Instantiate(fairyPrefab, pose.position, pose.rotation);
            if (plane != null)
            {
                spawnedFairy.transform.SetParent(plane.transform, true);
                Debug.Log("[ARFairySpawner] Parented to plane.");
            }
        }

        public void ForceRespawn()
        {
            Debug.Log("[ARFairySpawner] ForceRespawn called (Debug Mode).");
            if (spawnedFairy != null)
            {
                Destroy(spawnedFairy);
            }

            if (fairyPrefab == null)
            {
                Debug.LogError("[ARFairySpawner] Fairy Prefab is null!");
                return;
            }

            Transform camTransform = Camera.main.transform;
            Vector3 spawnPos = camTransform.position + camTransform.forward * 1.0f;
            Quaternion spawnRot = Quaternion.LookRotation(camTransform.forward, Vector3.up);

            spawnedFairy = Instantiate(fairyPrefab, spawnPos, spawnRot);
            Debug.Log($"[ARFairySpawner] Fairy spawned at {spawnPos} (Debug Force).");
            
            // Add a simple logic component to the fairy if it doesn't have one, just to make sure it does something
            // (Optional, depending on what the prefab has)
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 300, 150), "AR Debug Info");
            
            string status = "Waiting...";
            if (planeManager == null) status = "PlaneManager NULL";
            else status = $"Planes: {planeManager.trackables.count}";

            GUI.Label(new Rect(20, 40, 280, 20), $"Status: {status}");
            GUI.Label(new Rect(20, 60, 280, 20), $"Fairy: {(spawnedFairy ? "Spawned" : "Null")}");

            if (GUI.Button(new Rect(50, 90, 200, 50), "DEBUG SPAWN (1m Front)"))
            {
                ForceRespawn();
            }
        }
    }
}

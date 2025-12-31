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
            if (planeManager != null)
            {
                planeManager.planesChanged += OnPlanesChanged;
            }
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

            if (args.added != null && args.added.Count > 0)
            {
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
                SpawnFairy(hits[0].pose, hits[0].trackable as ARPlane);
            }
        }

        private void TrySpawnAtPlane(ARPlane plane)
        {
            if (raycastManager == null || fairyPrefab == null || plane == null)
            {
                return;
            }

            var center = plane.center;
            var pose = new Pose(plane.transform.TransformPoint(center), plane.transform.rotation);
            SpawnFairy(pose, plane);
        }

        private void SpawnFairy(Pose pose, ARPlane plane)
        {
            if (spawnedFairy != null)
            {
                return;
            }

            spawnedFairy = Instantiate(fairyPrefab, pose.position, pose.rotation);
            if (plane != null)
            {
                spawnedFairy.transform.SetParent(plane.transform, true);
            }
        }

        public void ForceRespawn()
        {
            if (spawnedFairy != null)
            {
                Destroy(spawnedFairy);
                spawnedFairy = null;
            }

            TrySpawnAtScreenCenter();
        }
    }
}

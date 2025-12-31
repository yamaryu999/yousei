using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace YouseiAR.Missions
{
    /// <summary>
    /// カメラ画像から特定色（緑/青）の占有率を測り、ミッション進捗を報告する。
    /// </summary>
    [RequireComponent(typeof(ARCameraManager))]
    public class ColorMissionCondition : MonoBehaviour
    {
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private int downsample = 6;
        [SerializeField] private float hueRange = 0.08f;
        [SerializeField] private float minSaturation = 0.35f;
        [SerializeField] private float minValue = 0.2f;
        [SerializeField] private float checkInterval = 0.3f;

        private ARCameraManager cameraManager;
        private float nextCheckTime;

        private void Awake()
        {
            cameraManager = GetComponent<ARCameraManager>();
        }

        private void OnEnable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        private void OnDisable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            if (missionManager == null || missionManager.Current == null)
            {
                return;
            }

            if (missionManager.Current.type != MissionType.ColorGreen && missionManager.Current.type != MissionType.SkyBlue)
            {
                return;
            }

            if (Time.time < nextCheckTime)
            {
                return;
            }

            if (!cameraManager.TryAcquireLatestCpuImage(out var cpuImage))
            {
                return;
            }

            using (cpuImage)
            {
                EvaluateImage(cpuImage, missionManager.Current);
            }

            nextCheckTime = Time.time + checkInterval;
        }

        private void EvaluateImage(XRCpuImage image, MissionDefinition mission)
        {
            var width = Mathf.Max(1, image.width / downsample);
            var height = Mathf.Max(1, image.height / downsample);

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(width, height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorX
            };

            var size = image.GetConvertedDataSize(conversionParams);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);

            try
            {
                image.Convert(conversionParams, buffer);
                var ratio = CalculateRatio(buffer, mission.type);
                var progress = Mathf.Clamp01(ratio / Mathf.Max(0.01f, mission.threshold));
                missionManager.ReportProgress(progress);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Color mission convert failed: {e.Message}");
            }
            finally
            {
                if (buffer.IsCreated)
                {
                    buffer.Dispose();
                }
            }
        }

        private float CalculateRatio(NativeArray<byte> rgba, MissionType type)
        {
            var targetHue = type == MissionType.ColorGreen ? 0.33f : 0.55f;
            var matchCount = 0;
            var total = rgba.Length / 4;

            for (int i = 0; i < rgba.Length; i += 4)
            {
                var r = rgba[i] / 255f;
                var g = rgba[i + 1] / 255f;
                var b = rgba[i + 2] / 255f;

                Color.RGBToHSV(new Color(r, g, b), out var h, out var s, out var v);
                var hueDelta = Mathf.Abs(Mathf.DeltaAngle(h * 360f, targetHue * 360f)) / 360f;

                if (hueDelta <= hueRange && s >= minSaturation && v >= minValue)
                {
                    matchCount++;
                }
            }

            return total == 0 ? 0f : (float)matchCount / total;
        }
    }
}

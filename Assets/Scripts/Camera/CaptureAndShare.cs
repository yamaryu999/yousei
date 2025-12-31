using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace YouseiAR.CameraTools
{
    /// <summary>
    /// 画面をキャプチャし、端末のギャラリーに保存・共有できるようにする。
    /// </summary>
    public class CaptureAndShare : MonoBehaviour
    {
        [SerializeField] private string filePrefix = "fairy_mission";
        [SerializeField, Range(30, 100)] private int jpgQuality = 90;
        [SerializeField] private UnityEvent<string> onSaved; // パスを渡す
        [SerializeField] private UnityEvent<string> onFailed;

        private bool isCapturing;

        public void Capture()
        {
            if (!gameObject.activeInHierarchy || isCapturing)
            {
                return;
            }

            StartCoroutine(CaptureRoutine());
        }

        private IEnumerator CaptureRoutine()
        {
            isCapturing = true;
            yield return new WaitForEndOfFrame();

            Texture2D tex = null;
            try
            {
                tex = ScreenCapture.CaptureScreenshotAsTexture();
                var bytes = tex.EncodeToJPG(jpgQuality);
                var filename = $"{filePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
                var path = Path.Combine(Application.persistentDataPath, filename);
                File.WriteAllBytes(path, bytes);
                NotifyAndroidGallery(path);
                onSaved?.Invoke(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Screenshot failed: {e}");
                onFailed?.Invoke(e.Message);
            }
            finally
            {
                if (tex != null)
                {
                    Destroy(tex);
                }

                isCapturing = false;
            }
        }

        private void NotifyAndroidGallery(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var mediaScanner = new AndroidJavaClass("android.media.MediaScannerConnection");
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                mediaScanner.CallStatic("scanFile", context, new[] { path }, null, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"MediaScanner scanFile failed: {e.Message}");
            }
#endif
        }
    }
}

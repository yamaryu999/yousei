using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace YouseiAR.Missions
{
    /// <summary>
    /// UI要素（テキスト/スライダー/ボタン）をミッション進捗に合わせて更新する。
    /// </summary>
    public class MissionUIBinder : MonoBehaviour
    {
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private Text missionText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button captureButton;
        [SerializeField] private UnityEvent onCompleted;

        private void OnEnable()
        {
            if (missionManager == null)
            {
                return;
            }

            missionManager.OnMissionChanged += HandleMissionChanged;
            missionManager.OnMissionProgress += HandleProgress;
            missionManager.OnMissionCompleted += HandleCompleted;
        }

        private void OnDisable()
        {
            if (missionManager == null)
            {
                return;
            }

            missionManager.OnMissionChanged -= HandleMissionChanged;
            missionManager.OnMissionProgress -= HandleProgress;
            missionManager.OnMissionCompleted -= HandleCompleted;
        }

        private void HandleMissionChanged(MissionDefinition mission)
        {
            if (missionText != null)
            {
                missionText.text = BuildMissionText(mission);
            }

            if (progressBar != null)
            {
                progressBar.value = 0f;
            }

            if (captureButton != null)
            {
                captureButton.interactable = false;
            }
        }

        private void HandleProgress(float value)
        {
            if (progressBar != null)
            {
                progressBar.value = value;
            }

            if (captureButton != null)
            {
                captureButton.interactable = value >= 1f;
            }
        }

        private void HandleCompleted(MissionDefinition _)
        {
            if (captureButton != null)
            {
                captureButton.interactable = true;
            }

            onCompleted?.Invoke();
        }

        private static string BuildMissionText(MissionDefinition mission)
        {
            if (mission == null)
            {
                return "ミッションなし";
            }

            var sb = new StringBuilder();
            sb.Append("今日のお題: ");
            sb.Append(mission.description);
            return sb.ToString();
        }
    }
}

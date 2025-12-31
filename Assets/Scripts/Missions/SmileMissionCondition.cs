using UnityEngine;

namespace YouseiAR.Missions
{
    /// <summary>
    /// ML Kit などネイティブ側の表情推定結果を受け取り、ミッション進捗として送信するブリッジ。
    /// Native Plugin 側から SubmitSmileScore を呼び出してください。
    /// </summary>
    public class SmileMissionCondition : MonoBehaviour
    {
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private float smooth = 0.2f;

        private float currentScore;

        /// <summary>
        /// 0.0 - 1.0 の笑顔スコアを渡す。ML Kit Face Detection の smileProbability を想定。
        /// </summary>
        public void SubmitSmileScore(float score)
        {
            if (missionManager == null || missionManager.Current == null || missionManager.Current.type != MissionType.Smile)
            {
                return;
            }

            score = Mathf.Clamp01(score);
            currentScore = Mathf.Lerp(currentScore, score, smooth);

            var progress = Mathf.Clamp01(currentScore / Mathf.Max(0.01f, missionManager.Current.threshold));
            missionManager.ReportProgress(progress);
        }
    }
}

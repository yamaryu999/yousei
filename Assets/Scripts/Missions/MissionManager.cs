using System;
using System.Collections.Generic;
using UnityEngine;

namespace YouseiAR.Missions
{
    public enum MissionType
    {
        ColorGreen,
        SkyBlue,
        Smile
    }

    [Serializable]
    public class MissionDefinition
    {
        public string id;
        public MissionType type;
        [Range(0f, 1f)] public float threshold = 0.3f; // 条件達成とみなす閾値（色占有率やスコア）
        [TextArea] public string description;
        public string rewardId;
    }

    [Serializable]
    public class MissionState
    {
        public string id;
        public long expiresAtTicks;
        public float progress;
        public bool completed;
    }

    public class MissionManager : MonoBehaviour
    {
        [SerializeField] private List<MissionDefinition> missionPool = new List<MissionDefinition>();
        [SerializeField] private int rotateSeedOffset = 0;

        public MissionDefinition Current { get; private set; }
        public MissionState State { get; private set; }

        public event Action<MissionDefinition> OnMissionChanged;
        public event Action<float> OnMissionProgress;
        public event Action<MissionDefinition> OnMissionCompleted;

        private const string PrefsKey = "FAIRY_MISSION_STATE";

        private void Awake()
        {
            LoadOrAssignMission();
        }

        private void LoadOrAssignMission()
        {
            if (!TryLoadState(out var savedState) || new DateTime(savedState.expiresAtTicks, DateTimeKind.Utc) <= DateTime.UtcNow)
            {
                AssignNewMission();
                return;
            }

            var def = missionPool.Find(m => m.id == savedState.id);
            if (def == null)
            {
                AssignNewMission();
                return;
            }

            Current = def;
            State = savedState;
            OnMissionChanged?.Invoke(Current);
            OnMissionProgress?.Invoke(State.progress);
            if (State.completed)
            {
                OnMissionCompleted?.Invoke(Current);
            }
        }

        private void AssignNewMission()
        {
            if (missionPool.Count == 0)
            {
                Debug.LogWarning("Mission pool is empty.");
                return;
            }

            var index = GetDailyIndex();
            Current = missionPool[index];
            State = new MissionState
            {
                id = Current.id,
                expiresAtTicks = DateTime.UtcNow.Date.AddDays(1).Ticks,
                progress = 0f,
                completed = false
            };

            SaveState();
            OnMissionChanged?.Invoke(Current);
            OnMissionProgress?.Invoke(0f);
        }

        private int GetDailyIndex()
        {
            var dayIndex = (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays + rotateSeedOffset;
            var clamped = Mathf.Abs(dayIndex % missionPool.Count);
            return clamped;
        }

        public void ReportProgress(float normalized)
        {
            if (Current == null || State == null || State.completed)
            {
                return;
            }

            var clamped = Mathf.Clamp01(normalized);
            if (Mathf.Approximately(clamped, State.progress) || clamped < State.progress)
            {
                return;
            }

            State.progress = clamped;
            SaveState();
            OnMissionProgress?.Invoke(State.progress);

            if (State.progress >= 1f)
            {
                CompleteCurrentMission();
            }
        }

        private void CompleteCurrentMission()
        {
            if (State == null || State.completed)
            {
                return;
            }

            State.completed = true;
            SaveState();
            OnMissionCompleted?.Invoke(Current);
        }

        private void SaveState()
        {
            try
            {
                var serialized = JsonUtility.ToJson(State);
                PlayerPrefs.SetString(PrefsKey, serialized);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Mission state save failed: {e}");
            }
        }

        private bool TryLoadState(out MissionState state)
        {
            state = null;
            if (!PlayerPrefs.HasKey(PrefsKey))
            {
                return false;
            }

            try
            {
                var json = PlayerPrefs.GetString(PrefsKey, string.Empty);
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }

                state = JsonUtility.FromJson<MissionState>(json);
                return state != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Mission state load failed: {e}");
                state = null;
                return false;
            }
        }
    }
}

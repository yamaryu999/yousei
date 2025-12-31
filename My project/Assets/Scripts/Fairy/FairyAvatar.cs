using System.Collections;
using UnityEngine;

namespace YouseiAR.Fairy
{
    /// <summary>
    /// 妖精モデルのアニメーションと演出をまとめて制御する。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class FairyAvatar : MonoBehaviour
    {
        [Header("Animator Parameters")]
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string happyTrigger = "Happy";
        [SerializeField] private string thanksTrigger = "Thanks";

        [Header("VFX")]
        [SerializeField] private GameObject happyVfx;
        [SerializeField] private GameObject thanksVfx;
        [SerializeField] private Transform vfxSpawnPoint;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void PlayIdle()
        {
            Trigger(idleTrigger);
        }

        public void PlayHappy()
        {
            Trigger(happyTrigger);
            SpawnVfx(happyVfx);
        }

        public void PlayThanks()
        {
            Trigger(thanksTrigger);
            SpawnVfx(thanksVfx);
        }

        private void Trigger(string parameter)
        {
            if (animator == null || string.IsNullOrEmpty(parameter))
            {
                return;
            }

            animator.ResetTrigger(idleTrigger);
            animator.ResetTrigger(happyTrigger);
            animator.ResetTrigger(thanksTrigger);
            animator.SetTrigger(parameter);
        }

        private void SpawnVfx(GameObject vfxPrefab)
        {
            if (vfxPrefab == null)
            {
                return;
            }

            var point = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            var rotation = vfxSpawnPoint != null ? vfxSpawnPoint.rotation : Quaternion.identity;
            var instance = Instantiate(vfxPrefab, point, rotation);
            Destroy(instance, 4f);
        }

        public IEnumerator PlayHappySequence()
        {
            PlayHappy();
            yield return new WaitForSeconds(1.2f);
            PlayThanks();
        }
    }
}

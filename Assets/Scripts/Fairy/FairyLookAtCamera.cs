using UnityEngine;

namespace YouseiAR.Fairy
{
    /// <summary>
    /// 常にカメラの方を向く（Y軸回転のみ）
    /// </summary>
    public class FairyLookAtCamera : MonoBehaviour
    {
        private Transform mainCameraTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                mainCameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (mainCameraTransform == null)
            {
                if (Camera.main != null) mainCameraTransform = Camera.main.transform;
                return;
            }

            var targetPos = mainCameraTransform.position;
            targetPos.y = transform.position.y; // Keep vertical alignment

            var direction = targetPos - transform.position;
            if (direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }
}

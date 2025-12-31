using UnityEngine;

namespace YouseiAR.Fairy
{
    public class SimpleFairyMotion : MonoBehaviour
    {
        [Header("Floating")]
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float floatAmount = 0.1f;

        [Header("Wings")]
        [SerializeField] private Transform wingL;
        [SerializeField] private Transform wingR;
        [SerializeField] private float flapSpeed = 20f;
        [SerializeField] private float flapAngle = 45f;

        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.localPosition;
        }

        private void Update()
        {
            // Floating
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

            // Flapping
            if (wingL != null && wingR != null)
            {
                float angle = Mathf.Sin(Time.time * flapSpeed) * flapAngle;
                
                // Assuming wings are initially flat or vertical, rotate around Z or X depending on setup.
                // Here we assume simple X rotation for flapping
                wingL.localRotation = Quaternion.Euler(0, 0, angle);
                wingR.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }
    }
}

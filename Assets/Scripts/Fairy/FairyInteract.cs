using UnityEngine;
using UnityEngine.EventSystems;

namespace YouseiAR.Fairy
{
    [RequireComponent(typeof(FairyAvatar))]
    [RequireComponent(typeof(Collider))]
    public class FairyInteract : MonoBehaviour, IPointerClickHandler
    {
        private FairyAvatar avatar;

        private void Awake()
        {
            avatar = GetComponent<FairyAvatar>();
        }

        // This requires a PhysicsRaycaster on the Camera or EventSystem setup for 3D objects
        // For simplicity in AR, we can also use OnMouseDown if the object has a collider
        private void OnMouseDown()
        {
            Interact();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Interact();
        }

        public void Interact()
        {
            if (avatar != null)
            {
                StartCoroutine(avatar.PlayHappySequence());
            }
        }
    }
}

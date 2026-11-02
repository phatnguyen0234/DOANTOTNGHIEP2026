using UnityEngine;

public class InteractController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Camera mainCamera;

    [Header("Settings")]
    [SerializeField] private LayerMask interactableMask;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, Mathf.Infinity, interactableMask);

        if (hit.collider != null)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null && interactable.CanInteract(gameObject))
            {
                // Pass the Player gameObject so the interactable object knows who is interacting
                interactable.Interact(gameObject);
            }
        }
    }
}
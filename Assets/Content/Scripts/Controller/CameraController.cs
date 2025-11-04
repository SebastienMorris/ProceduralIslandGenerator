using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameObject referenceObj;
    [SerializeField] private float moveSpeed = 50f;
    
    [SerializeField] private float distance = 10f;
    public float Distance {
        get => distance;
        set => distance = Mathf.Clamp(value, minDistance, maxDistance);
    }
    
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 50f;

    private Vector2 moveInput;
    private float currentAngleX = 0f;
    private float currentAngleY = 0f;

    private void Start()
    {
        if (referenceObj != null)
        {
            // Initialize angles based on current position
            Vector3 direction = transform.position - referenceObj.transform.position;
            distance = direction.magnitude;
            currentAngleY = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            currentAngleX = Mathf.Asin(direction.y / distance) * Mathf.Rad2Deg;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<Vector2>().y;
        Debug.Log($"Scroll Vector: {scrollValue}");
        distance -= scrollValue * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void Update()
    {
        if (referenceObj == null) return;

        // Update angles based on WASD input
        currentAngleY -= moveInput.x * moveSpeed * Time.deltaTime;
        currentAngleX += moveInput.y * moveSpeed * Time.deltaTime;

        // Clamp vertical angle to avoid flipping
        currentAngleX = Mathf.Clamp(currentAngleX, -89f, 89f);

        // Calculate new position
        Quaternion rotation = Quaternion.Euler(currentAngleX, currentAngleY, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
        
        transform.position = referenceObj.transform.position + offset;
        transform.LookAt(referenceObj.transform.position);
    }
}
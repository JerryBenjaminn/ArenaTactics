using UnityEngine;

/// <summary>
/// Provides TFT-style camera controls with zoom, pan, and rotation.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera mainCamera;

    [Header("Position")]
    [SerializeField] private Vector3 focusPoint = Vector3.zero;
    [SerializeField] private float startHeight = 12f;
    [SerializeField] private float startDistance = 10f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    private float currentZoom;

    [Header("Pan")]
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private Vector2 panBoundsMin = new Vector2(-5f, -5f);
    [SerializeField] private Vector2 panBoundsMax = new Vector2(5f, 5f);

    [Header("Mouse Pan")]
    [SerializeField] private bool enableMiddleMousePan = true;
    [SerializeField] private float mousePanSpeed = 0.5f;

    [Header("Mouse Rotation")]
    [SerializeField] private bool enableRightMouseRotation = true;
    [SerializeField] private float mouseRotationSpeed = 2f;
    [SerializeField] private float minRotationDrag = 5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private bool allowRotation = true;
    [SerializeField] private float currentRotationY = 45f;

    [Header("Angle")]
    [SerializeField] private float cameraAngle = 45f;
    [SerializeField] private bool lockAngle = true;

    private Vector3 lastMousePosition;
    private bool isMiddleMousePanning;
    private bool isRightMouseRotating;
    private Vector3 lastRotationMousePos;
    private Vector3 rightClickStartPos;

    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = transform;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        currentRotationY = 0f;
        currentZoom = startDistance;

        PositionCameraForDeployment();
    }

    private void PositionCameraForDeployment()
    {
        if (GridManager.Instance != null)
        {
            int gridHeight = GridManager.Instance.GridHeight;
            int gridWidth = GridManager.Instance.GridWidth;
            float cellSize = GridManager.Instance.CellSize;

            float deploymentCenterZ = ((0f + 1f) / 2f - gridHeight / 2f) * cellSize;
            focusPoint = new Vector3(0f, 0f, deploymentCenterZ);

            Debug.Log($"CameraController: Focused on player deployment at {focusPoint}");
        }
        else
        {
            focusPoint = new Vector3(0f, 0f, -3f);
        }

        UpdateCameraPosition();
    }

    private void Update()
    {
        HandleZoom();
        HandlePan();
        HandleMiddleMousePan();
        HandleRotation();
        HandleRightMouseRotation();
        UpdateCameraPosition();
    }

    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput == 0f)
        {
            return;
        }

        currentZoom -= scrollInput * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }

    private void HandlePan()
    {
        Vector3 panInput = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            panInput.z += 1f;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            panInput.z -= 1f;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            panInput.x -= 1f;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            panInput.x += 1f;
        }

        if (panInput.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 rotatedPan = Quaternion.Euler(0f, currentRotationY, 0f) * panInput;
        focusPoint += rotatedPan * panSpeed * Time.deltaTime;

        focusPoint.x = Mathf.Clamp(focusPoint.x, panBoundsMin.x, panBoundsMax.x);
        focusPoint.z = Mathf.Clamp(focusPoint.z, panBoundsMin.y, panBoundsMax.y);
    }

    private void HandleRotation()
    {
        if (!allowRotation)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            currentRotationY -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            currentRotationY += rotationSpeed * Time.deltaTime;
        }

        if (currentRotationY < 0f)
        {
            currentRotationY += 360f;
        }
        if (currentRotationY >= 360f)
        {
            currentRotationY -= 360f;
        }
    }

    private void HandleRightMouseRotation()
    {
        if (!enableRightMouseRotation || !allowRotation)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            isRightMouseRotating = true;
            lastRotationMousePos = Input.mousePosition;
            rightClickStartPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))
        {
            float dragDistance = Vector3.Distance(Input.mousePosition, rightClickStartPos);
            if (dragDistance < minRotationDrag)
            {
                isRightMouseRotating = false;
                return;
            }

            isRightMouseRotating = false;
        }

        if (!isRightMouseRotating)
        {
            return;
        }

        Vector3 mouseDelta = Input.mousePosition - lastRotationMousePos;
        lastRotationMousePos = Input.mousePosition;

        if (Mathf.Abs(mouseDelta.x) <= 0.1f)
        {
            return;
        }

        currentRotationY += mouseDelta.x * mouseRotationSpeed;

        if (currentRotationY < 0f)
        {
            currentRotationY += 360f;
        }
        if (currentRotationY >= 360f)
        {
            currentRotationY -= 360f;
        }
    }

    public bool IsRotating()
    {
        return isRightMouseRotating;
    }
    private void HandleMiddleMousePan()
    {
        if (!enableMiddleMousePan)
        {
            return;
        }

        if (Input.GetMouseButtonDown(2))
        {
            isMiddleMousePanning = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
        {
            isMiddleMousePanning = false;
        }

        if (!isMiddleMousePanning)
        {
            return;
        }

        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        lastMousePosition = Input.mousePosition;

        if (mouseDelta.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 pan = new Vector3(-mouseDelta.x, 0f, -mouseDelta.y);
        pan = Quaternion.Euler(0f, currentRotationY, 0f) * pan;
        pan *= mousePanSpeed * (currentZoom / startDistance);

        focusPoint += pan * Time.deltaTime;

        focusPoint.x = Mathf.Clamp(focusPoint.x, panBoundsMin.x, panBoundsMax.x);
        focusPoint.z = Mathf.Clamp(focusPoint.z, panBoundsMin.y, panBoundsMax.y);
    }

    private void UpdateCameraPosition()
    {
        float angleRad = cameraAngle * Mathf.Deg2Rad;
        float horizontalDistance = currentZoom * Mathf.Cos(angleRad);
        float verticalDistance = currentZoom * Mathf.Sin(angleRad);

        Vector3 offset = Quaternion.Euler(0f, currentRotationY, 0f) * new Vector3(0f, 0f, -horizontalDistance);
        offset.y = verticalDistance;

        cameraTransform.position = focusPoint + offset;
        cameraTransform.LookAt(focusPoint);

        if (lockAngle)
        {
            Vector3 euler = cameraTransform.rotation.eulerAngles;
            cameraTransform.rotation = Quaternion.Euler(cameraAngle, euler.y, 0f);
        }
    }

    public void SetFocusPoint(Vector3 point)
    {
        focusPoint = point;
    }

    public void FocusOnGladiator(Gladiator gladiator)
    {
        if (gladiator != null)
        {
            focusPoint = gladiator.transform.position;
        }
    }

    public void ResetToDeploymentView()
    {
        PositionCameraForDeployment();
        currentRotationY = 45f;
        currentZoom = startDistance;
    }
}

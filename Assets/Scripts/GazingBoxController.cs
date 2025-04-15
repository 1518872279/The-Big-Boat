using UnityEngine;

public class GazingBoxController : MonoBehaviour
{
    [Header("Control Settings")]
    public float rotationSensitivity = 0.1f; // How much the box rotates per pixel of mouse movement
    public float maxTilt = 30.0f;            // Maximum allowed tilt (degrees)
    public float returnSpeed = 2.0f;         // Speed at which the box returns to neutral when not dragging

    [Header("Collision Settings")]
    public string boatLayerName = "Boat";    // Layer name for the boat
    public string boxColliderLayerName = "BoxBoundary"; // Layer name for box colliders
    
    private Vector3 targetRotationOffset = Vector3.zero;
    private Quaternion neutralRotation;
    private Vector3 previousMousePosition;
    private bool isDragging = false;
    private BoxCollider[] boundaryColliders;

    void Start()
    {
        // Cache the initial rotation as the neutral position.
        neutralRotation = transform.rotation;
        
        // Set up the boundary colliders
        SetupBoundaryColliders();
    }
    
    void SetupBoundaryColliders()
    {
        // Get all box colliders on this object and its children
        boundaryColliders = GetComponentsInChildren<BoxCollider>();
        
        // Ensure we have a layer for the box boundaries
        if (LayerMask.NameToLayer(boxColliderLayerName) == -1)
        {
            Debug.LogWarning("Layer '" + boxColliderLayerName + "' not found. Please create this layer in the Unity Editor.");
        }
        else
        {
            // Set all boundary colliders to the box collider layer
            foreach (BoxCollider collider in boundaryColliders)
            {
                collider.gameObject.layer = LayerMask.NameToLayer(boxColliderLayerName);
                
                // Create a physics material if none exists
                if (collider.material == null)
                {
                    PhysicMaterial boxPhysicsMaterial = new PhysicMaterial("BoxPhysicsMaterial");
                    boxPhysicsMaterial.dynamicFriction = 0.1f;
                    boxPhysicsMaterial.staticFriction = 0.1f;
                    boxPhysicsMaterial.bounciness = 0.5f;
                    boxPhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
                    boxPhysicsMaterial.bounceCombine = PhysicMaterialCombine.Maximum;
                    collider.material = boxPhysicsMaterial;
                }
            }
            
            // Set up physics matrix to ensure boat and box colliders interact properly
            int boatLayer = LayerMask.NameToLayer(boatLayerName);
            int boxLayer = LayerMask.NameToLayer(boxColliderLayerName);
            
            if (boatLayer != -1)
            {
                // Set collision matrix if both layers exist
                Physics.IgnoreLayerCollision(boatLayer, boatLayer, true); // Ignore boat-to-boat collisions
                Physics.IgnoreLayerCollision(boxLayer, boxLayer, true);   // Ignore box-to-box collisions
                Physics.IgnoreLayerCollision(boatLayer, boxLayer, false); // Ensure boat-to-box collisions are detected
            }
        }
    }

    void Update()
    {
        // Begin dragging on mouse button press
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            previousMousePosition = Input.mousePosition;
        }
        // End dragging on mouse button release
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // Process rotation during drag
        if (isDragging)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 mouseDelta = currentMousePosition - previousMousePosition;

            // Calculate rotation offsets based on mouse movement
            float tiltAroundX = mouseDelta.y * rotationSensitivity;  // Forward/backward tilt
            float tiltAroundZ = -mouseDelta.x * rotationSensitivity; // Side-to-side tilt

            targetRotationOffset.x += tiltAroundX;
            targetRotationOffset.z += tiltAroundZ;

            // Clamp the rotation offsets
            targetRotationOffset.x = Mathf.Clamp(targetRotationOffset.x, -maxTilt, maxTilt);
            targetRotationOffset.z = Mathf.Clamp(targetRotationOffset.z, -maxTilt, maxTilt);

            previousMousePosition = currentMousePosition;
        }
        else
        {
            // Gradually return to a neutral rotation when not dragging
            targetRotationOffset = Vector3.Lerp(targetRotationOffset, Vector3.zero, Time.deltaTime * returnSpeed);
        }

        // Apply the rotation offset
        Quaternion rotationOffset = Quaternion.Euler(targetRotationOffset);
        Quaternion targetRotation = neutralRotation * rotationOffset;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5.0f);
    }
    
    public void ResetRotation()
    {
        // Reset the box rotation to the neutral state
        targetRotationOffset = Vector3.zero;
        transform.rotation = neutralRotation;
    }
} 
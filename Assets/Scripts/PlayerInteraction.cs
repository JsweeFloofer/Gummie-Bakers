using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    private PlayerMovement playerMovement;
    
    [SerializeField] private IngredientCrate crate;

    InputAction interactAction;

    [Header("Automated Testing")]
    public bool useTestInteraction;
    private bool testInteractPressed;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();

        crate = FindFirstObjectByType<IngredientCrate>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interactAction = InputSystem.actions.FindAction("Interact");
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (playerMovement != null && playerMovement.IsMovementLocked)
        {
            testInteractPressed = false;
            return;
        }

        bool interactPressed = false;

        if (useTestInteraction)
        {
            interactPressed = testInteractPressed;
            testInteractPressed = false;
        }
        else if (interactAction != null)
        {
            interactPressed = interactAction.WasPressedThisFrame();
        }



        if ((interactAction != null && interactAction.WasPressedThisFrame() && crate != null) || interactPressed)
        {
            // Debug.Log("AAA"); // AAAAAAAAAAAAAAAAAAA anyways hi
            crate.Interact();

            if (crate.HasBeenInteractedWith)
            {
                Debug.Log("Interact works!");
            }
            else
            {
                Debug.LogError("Interact FAILED!");
            }
        }
    }

    public void TriggerTestInteraction()
    {
        testInteractPressed = true;
    }
}

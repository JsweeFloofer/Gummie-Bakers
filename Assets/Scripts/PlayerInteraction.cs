using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    private PlayerMovement playerMovement;
    
    [SerializeField] 
    private IngredientCrate crate;

    InputAction interactAction;

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
            return;
        }

        if (interactAction != null && interactAction.WasPressedThisFrame() && crate != null)
        {
            Debug.Log("AAA"); // AAAAAAAAAAAAAAAAAAA anyways hi
            crate.Interact();
        }
    }

}

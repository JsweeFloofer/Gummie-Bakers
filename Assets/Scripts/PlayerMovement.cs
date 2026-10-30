using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private Rigidbody rb;

    [SerializeField]
    private Animator anim;

    public float moveSpeed = 5f;

    InputAction moveAction;
    private Vector2 moveDir;

    public bool IsMovementLocked { get; private set; }

    InputAction pickUpAction;

    public GameObject pickUpCollider;

    [Header("Automated Testing")]
    public bool useTestInput;

    private Vector2 testMoveDir;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!IsOwner) return;

        moveAction = InputSystem.actions.FindAction("Move");
        pickUpAction = InputSystem.actions.FindAction("Interact");

        if (moveAction != null && !moveAction.enabled)
        {
            moveAction.Enable();
        }

        Debug.Log(
            "Player initialized. IsOwner: " + IsOwner +
            " | Move Action Found: " + (moveAction != null) +
            " | Move Action Enabled: " + (moveAction != null && moveAction.enabled)
        );


        if (anim != null)
        {
            anim.SetFloat("VerticalInput", 0);
            anim.SetFloat("HorizontalInput", 0);
            anim.SetBool("Walking", false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (IsMovementLocked)
        {
            moveDir = Vector2.zero;
            return;
        }

        if (useTestInput)
        {
            moveDir = testMoveDir;
        }
        else if (moveAction != null)
        {
            moveDir = moveAction.ReadValue<Vector2>();
        }

        if (pickUpAction.WasPressedThisFrame())
        {
            StartCoroutine(ActivatePickUpCollider());
        }

    }

    void FixedUpdate()
    {
        if (!IsOwner || rb == null) return;
        if (IsMovementLocked)
        {
            moveDir = Vector2.zero;

            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (anim != null)
            {
                anim.SetFloat("VerticalInput", 0);
                anim.SetFloat("HorizontalInput", 0);
                anim.SetBool("Walking", false);
            }

            return;
        }

        rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.y * moveSpeed);

        if (anim != null)
        {
            anim.SetFloat("HorizontalInput", moveDir.x);
            anim.SetFloat("VerticalInput", moveDir.y);
            anim.SetBool("Walking", moveDir.x != 0 || moveDir.y != 0);
        }
    }

    public void SetMovementLocked(bool locked)
    {
        if (!IsOwner) return;

        IsMovementLocked = locked;
        
        if (locked)
        {
            moveDir = Vector2.zero;
        }
    }

    private System.Collections.IEnumerator ActivatePickUpCollider()
    {
        pickUpCollider.SetActive(true);

        //wait one physics frame so trigger detection can occur
        yield return new WaitForFixedUpdate();

        pickUpCollider.SetActive(false);
    }

    public void SetTestMovementInput(Vector2 input)
    {
        testMoveDir = input;
    }
}

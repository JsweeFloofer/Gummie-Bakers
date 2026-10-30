
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class AutomatedGameTester : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private GameObject buttons;
    [SerializeField] private Button[] ingredientButtons = new Button[3];

    [Header("Test Settings")]
    [SerializeField] private float initialDelay = 1f;
    [SerializeField] private float movementDuration = 1f;
    [SerializeField] private float movementThreshold = 0.2f;
    [SerializeField] private float networkTimeout = 15f;

    private NetworkObject player;

    private PlayerMovement playerMovement;
    private PlayerInteraction playerInteraction;

    private IEnumerator Start()
    {
        Debug.Log("-- STARTING AUTOMATED GAME TESTS --");


        yield return new WaitForSeconds(initialDelay);

        // Spawn player and host a lobby
        Debug.Log("Testing Host Button...");

        if (hostButton == null)
        {
            Fail("Host button reference is missing!");
            yield break;
        }

        hostButton.onClick.Invoke();

        Debug.Log("Host button clicked!");

        // Waiting for the server
        Debug.Log("Waiting for server to start...");

        float timer = 0f;

        while (timer < networkTimeout)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening &&
                NetworkManager.Singleton.IsServer)
            {
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening ||
            !NetworkManager.Singleton.IsServer)
        {
            Fail("Server failed to start!");
            yield break;
        }

        Debug.Log("Server started successfully!");

        // Check if player is spaned
        Debug.Log("Testing Player Spawn...");

        timer = 0f;

        while (timer < networkTimeout)
        {
            if (NetworkManager.Singleton.LocalClient != null)
            {
                player = NetworkManager.Singleton.LocalClient.PlayerObject;
            }

            if (player != null && player.IsSpawned)
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        if (player == null || !player.IsSpawned)
        {
            Fail("Player failed to spawn!");
            yield break;
        }

        if (!player.IsOwner)
        {
            Fail("Player is not spawned on the server!");
            yield break;
        }

        Debug.Log("Player spawned successfully on the server!");
        
        playerMovement = player.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            Fail("PlayerMovement component not found!");
            yield break;
        }

        playerMovement.useTestInput = true;

        Debug.Log("Automated movement control enabled!");

        playerInteraction = player.GetComponent<PlayerInteraction>();

        if (playerInteraction == null)
        {
            Fail("PlayerInteraction component not found!");
            yield break;
        }

        playerInteraction.useTestInteraction = true;

        Debug.Log("Automated interaction control enabled!");

        yield return new WaitForSeconds(0.5f);

        Debug.Log("-- TESTING MOVEMENT --");


        yield return StartCoroutine(TestMovement("Moving Up", Vector2.up, Vector3.forward));

        if (player == null) yield break;

        yield return StartCoroutine(TestMovement("Moving Down", Vector2.down, Vector3.back));

        if (player == null) yield break;

        yield return StartCoroutine(TestMovement("Moving Left", Vector2.left, Vector3.left));

        if (player == null) yield break;

        yield return StartCoroutine(TestMovement("Moving Right", Vector2.right, Vector3.right));
        yield return StartCoroutine(TestMovement("Moving Right", Vector2.right, Vector3.right));

        yield return new WaitForSeconds(0.5f);

        Debug.Log("-- TESTING INTERACTION --");

        

        yield return StartCoroutine(TestInteraction());

        yield return new WaitForSeconds(0.5f);

        SetIngredientButtons();

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Testing ingredient spawn ID: " + 0);
        ingredientButtons[0].onClick.Invoke();

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestInteraction());

        yield return new WaitForSeconds(0.5f);

        SetIngredientButtons();

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Testing ingredient spawn ID: " + 1);
        ingredientButtons[1].onClick.Invoke();

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestInteraction());

        yield return new WaitForSeconds(0.5f);

        SetIngredientButtons();

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Testing ingredient spawn ID: " + 2);
        ingredientButtons[2].onClick.Invoke();

        Debug.Log("-- ALL AUTOMATED TESTS COMPLETED! --");

    }

    private IEnumerator TestMovement(string testName, Vector2 inputDirection, Vector3 expectedDirection)
    {
        Debug.Log("Testing " + testName + "...");

        if (player == null || !player.IsSpawned)
        {
            Fail("Player disappeared!");
            yield break;
        }

        Vector3 startingPosition = player.transform.position;

        // Apply movement input
        playerMovement.SetTestMovementInput(inputDirection);

        // Hold the input for set duration
        yield return new WaitForSeconds(movementDuration);

        // Release movement input
        playerMovement.SetTestMovementInput(Vector2.zero);

        yield return new WaitForFixedUpdate();

        Vector3 endingPosition = player.transform.position;

        Vector3 movement = endingPosition - startingPosition;

        float distanceMoved = Vector3.Dot(
            movement,
            expectedDirection
        );

        if (distanceMoved >= movementThreshold)
        {
            Debug.Log(
                testName + " works! Moved " +
                distanceMoved.ToString("F2") + " units."
            );
        }
        else
        {
            Debug.LogError(
                testName + " FAILED! Actual movement: " + movement
            );
        }
    }

    private IEnumerator TestInteraction()
    {
        Debug.Log("Testing Interact...");

        if (player == null || !player.IsSpawned)
        {
            Fail("Player disappeared!");
            yield break;
        }

        if (playerInteraction == null)
        {
            Fail("PlayerInteraction component missing!");
            yield break;
        }

        // Trigger the interaction
        playerInteraction.TriggerTestInteraction();

        // Wait for Update() to process the interaction
        yield return null;

        // Give any resulting gameplay logic time to execute
        yield return new WaitForSeconds(0.2f);

        Debug.Log("Interact test completed!");
    }

    private void SetIngredientButtons()
    {
        for (int i = 0; i < ingredientButtons.Length; i++)
        {
            
            ingredientButtons[i] = buttons.transform.GetChild(i).GetComponent<Button>();
        }
    }

    private void Fail(string message)
    {
        Debug.LogError("[AUTOMATED TEST FAILED] " + message);
    }
}

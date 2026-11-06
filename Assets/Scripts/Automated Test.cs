
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

    [Header("Test Settings")]
    [SerializeField] private float initialDelay = 1f;
    [SerializeField] private float movementDuration = 1f;
    [SerializeField] private float movementThreshold = 0.2f;
    [SerializeField] private float networkTimeout = 15f;

    private Keyboard virtualKeyboard;
    private NetworkObject player;

    private PlayerMovement playerMovement;

    private IEnumerator Start()
    {
        Debug.Log("-- STARTING AUTOMATED GAME TESTS --");

        // Create a virtual keyboard for simulated inputs
        virtualKeyboard = InputSystem.AddDevice<Keyboard>();

        TestKeyboardInput();

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


        yield return new WaitForSeconds(0.5f);

        Debug.Log("-- TESTING MOVEMENT --");


        yield return StartCoroutine(TestMovement(
            "Moving Up", Key.W, Vector3.forward));

        if (player == null) yield break;

        yield return StartCoroutine(TestMovement(
            "Moving Down", Key.S, Vector3.back));

        if (player == null) yield break;

        yield return StartCoroutine(TestMovement(
            "Moving Left", Key.A, Vector3.left));

        if (player == null) yield break;

        yield return StartCoroutine(TestMovement(
            "Moving Right", Key.D, Vector3.right));



        ReleaseKeyboard();


        Debug.Log("-- ALL AUTOMATED TESTS COMPLETED! --");

    }

    private IEnumerator TestMovement(
        string testName,
        Key key,
        Vector3 expectedDirection)
    {
        Debug.Log("Testing " + testName + "...");

        if (player == null || !player.IsSpawned)
        {
            Fail("Player disappeared during movement test!");
            yield break;
        }

        Vector3 startingPosition = player.transform.position;

        // Press the simulated keyboard key.
        InputSystem.QueueStateEvent(
            virtualKeyboard,
            new KeyboardState(key)
        );

        // Hold the key for the configured duration.
        yield return new WaitForSeconds(movementDuration);

        // Release all keys.
        ReleaseKeyboard();

        // Allow physics to finish processing.
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
                testName + " FAILED! Expected movement in direction " +
                expectedDirection +
                " but actual movement was " + movement
            );
        }
    }

    private void ReleaseKeyboard()
    {
        if (virtualKeyboard == null)
            return;

        InputSystem.QueueStateEvent(
            virtualKeyboard,
            new KeyboardState()
        );
    }

    private void Fail(string message)
    {
        Debug.LogError("[AUTOMATED TEST FAILED] " + message);
        ReleaseKeyboard();
    }

    private void OnDestroy()
    {
        ReleaseKeyboard();

        if (virtualKeyboard != null &&
            virtualKeyboard.added)
        {
            InputSystem.RemoveDevice(virtualKeyboard);
        }
    }

    private void TestKeyboardInput()
    {
        InputSystem.QueueStateEvent(
            virtualKeyboard,
            new KeyboardState(Key.W)
        );

        InputSystem.Update();

        var moveAction = InputSystem.actions.FindAction("Move");

        if (moveAction == null)
        {
            Debug.LogError("Move action NOT FOUND!");
            return;
        }

        Debug.Log("Simulated Move Input: " + moveAction.ReadValue<Vector2>());

        ReleaseKeyboard();
        InputSystem.Update();
    }
}

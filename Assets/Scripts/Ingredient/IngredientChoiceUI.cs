
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class IngredientChoiceUI : MonoBehaviour
{
    public static IngredientChoiceUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Button cancel;

    private IngredientCrate currentCrate;
    private PlayerMovement localPlayerMovement;
    private readonly List<GameObject> spawnedButtons = new();

    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (choicePanel != null)
            choicePanel.SetActive(false);

        cancel.onClick.AddListener(() => ChooseItem(-1));
    }

    public void Open(IngredientCrate crate)
    {
        if (crate == null || choicePanel == null || buttonPrefab == null)
            return;

        currentCrate = crate;

        // Find this clients own playerr object thingyyy
        var networkManager = NetworkManager.Singleton;

        if (networkManager != null &&
            networkManager.LocalClient != null &&
            networkManager.LocalClient.PlayerObject != null)
        {
            localPlayerMovement =
                networkManager.LocalClient.PlayerObject
                    .GetComponent<PlayerMovement>();
        }

        if (localPlayerMovement != null)
            localPlayerMovement.SetMovementLocked(true);


        previousCursorVisible = Cursor.visible;
        previousCursorLockMode = Cursor.lockState;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ClearButtons();

        for (int i = 0; i < crate.IngredientCount; i++)
        {
            int choiceIndex = i;

            Button button = Instantiate(buttonPrefab, buttonContainer);

            spawnedButtons.Add(button.gameObject);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();

            if (label != null)
                label.text = crate.GetIngredientName(i);

            Image[] images = button.GetComponentsInChildren<Image>();

            if (images.Length > 1 && crate.GetIngredientIcon(i) != null)
                images[1].sprite = crate.GetIngredientIcon(i);

            button.onClick.AddListener(() => ChooseItem(choiceIndex));
        }

        choicePanel.SetActive(true);
    }

    private void ChooseItem(int choiceIndex)
    {
        if (currentCrate == null)
            return;

        currentCrate.SelectItem(choiceIndex);

    }

    public void Close()
    {
        choicePanel.SetActive(false);
        ClearButtons();

        if (localPlayerMovement != null)
            localPlayerMovement.SetMovementLocked(false);

        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockMode;

        currentCrate = null;
        localPlayerMovement = null;
    }

    private void ClearButtons()
    {
        foreach (GameObject button in spawnedButtons)
        {
            if (button != null)
                Destroy(button);
        }

        spawnedButtons.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

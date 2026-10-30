using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class IngredientCrate : NetworkBehaviour
{
    [Header("Ingredient Choices")]
    [SerializeField]
    private IngredientChoice[] ingredients;

    [Header("Misc")]
    [SerializeField] private Ingredient ingredientPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float interactionRange = 3f;

    private const ulong NoPlayer = ulong.MaxValue;

    private readonly NetworkVariable<ulong> currentUser = new NetworkVariable<ulong>(NoPlayer, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int IngredientCount => ingredients.Length;

    public bool HasBeenInteractedWith { get; private set; }

    public string GetIngredientName(int id)
    {
        return ingredients[id].displayName;
    }
    public Sprite GetIngredientIcon(int index)
    {
        return ingredients[index].icon;
    }

    public void Interact()
    {
        if (!IsClient || !IsSpawned) return;

        RequestOpenRpc();

        HasBeenInteractedWith = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestOpenRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Bunchaaa checksssss
        if (currentUser.Value != NoPlayer)
        {
            Debug.Log("Returned at curren user isn't a no player thingy majigyma");
            return;
        }


        if (ingredients == null || ingredients.Length == 0)
        {
            Debug.Log("Returned at ingredients");
            return;
        }

        if(!NetworkManager.ConnectedClients.TryGetValue(senderId, out var client))
        {
            Debug.Log("Returned at that one network manager line");
            return;
        }

        if (client.PlayerObject == null)
        {
            Debug.Log("Returned at Client Player Object");
            return;
        }


        //interaction
        float distance = Vector3.Distance(client.PlayerObject.transform.position, transform.position);

        Debug.Log(distance);

        if (distance > interactionRange)
        {
            return;
        }

        currentUser.Value = senderId;

        OpenUIRpc(RpcTarget.Single(senderId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void OpenUIRpc(RpcParams rpcParams = default)
    {
        IngredientChoiceUI ui = IngredientChoiceUI.Instance;

        if (ui == null)
        {
            RequestCancelRpc();
            return;
        }

        ui.Open(this);
    }

    public void SelectItem(int choiceIndex)
    {
        if (!IsClient || !IsSpawned)
            return;

        SelectItemRpc(choiceIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SelectItemRpc(int choiceIndex, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (currentUser.Value != senderId)
        {
            return;
        }

        if (choiceIndex >= ingredients.Length)
        {
            return;
        }

        if (ingredientPrefab == null)
        {
            return;
        }

        if (!NetworkManager.ConnectedClients.TryGetValue(senderId, out var client))
        {
            return;
        }

        if (client.PlayerObject == null)
        {
            return;
        }

        if (Vector3.Distance(client.PlayerObject.transform.position, transform.position) > interactionRange)
        {
            ReleaseCrate(senderId, false);
            return;
        }

        if (choiceIndex >= 0)
        {
            int ingredientID = ingredients[choiceIndex].IngredientID;

            Vector3 position = dropPoint != null ? dropPoint.position : transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

            Quaternion rotation = dropPoint != null ? dropPoint.rotation : Quaternion.identity;

            Ingredient droppedIngredient = Instantiate(ingredientPrefab, position, rotation);

            droppedIngredient.NetworkObject.Spawn();

            droppedIngredient.SetIngredientID(ingredientID);

            droppedIngredient.BlastOff();
        }


        ReleaseCrate(senderId, true);
    }

    private void ReleaseCrate(ulong clientId, bool successful)
    {
        if (currentUser.Value != clientId)
        {
            return;
        }

        currentUser.Value = NoPlayer;

        if (successful)
        {
            CloseUIRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
        else
        {
            CloseUIRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void CloseUIRpc(RpcParams rpcParams = default)
    {
        if (IngredientChoiceUI.Instance != null)
            IngredientChoiceUI.Instance.Close();
    }

    // Called if the UI is missing or the player cancels.
    public void CancelInteraction()
    {
        if (!IsClient || !IsSpawned)
            return;

        RequestCancelRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestCancelRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (currentUser.Value != senderId)
            return;

        ReleaseCrate(senderId, false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (currentUser.Value == clientId)
            currentUser.Value = NoPlayer;
    }
}

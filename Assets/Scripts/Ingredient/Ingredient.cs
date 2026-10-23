
using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class IngredientVisual
{
    public int ingredientID;
    public Sprite sprite;
}

public class Ingredient : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private IngredientVisual[] visuals;

    Rigidbody rb;

    private readonly NetworkVariable<int> ingredientID =
        new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public int IngredientID => ingredientID.Value;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        
        ingredientID.OnValueChanged += OnIngredientIDChanged;
        UpdateAppearance(ingredientID.Value);
    }

    public override void OnNetworkDespawn()
    {
        ingredientID.OnValueChanged -= OnIngredientIDChanged;
    }

    public void SetIngredientID(int id)
    {
        if (!IsServer)
            return;

        ingredientID.Value = id;
        UpdateAppearance(id);
    }

    private void OnIngredientIDChanged(int oldValue, int newValue)
    {
        UpdateAppearance(newValue);
    }

    private void UpdateAppearance(int id)
    {
        if (spriteRenderer == null || visuals == null)
            return;

        foreach (IngredientVisual visual in visuals)
        {
            if (visual.ingredientID == id)
            {
                spriteRenderer.sprite = visual.sprite;
                return;
            }
        }

        Debug.LogWarning($"No sprite configured for ingredient ID {id}.");
    }

    public void BlastOff()
    {
        if (!IsServer)
        {
            return;
        }

        if (rb == null)
        {
            Debug.LogError("No rigidbody found on ingredieenttt");
            return;
        }

        if (rb.isKinematic)
        {
            Debug.LogError("Ingredients rigidbody is set to Kinematic, big sad, make me so sad");
            return;
        }

        Vector3 forceAmount = new Vector3(
            UnityEngine.Random.Range(-50f, 0f),
            UnityEngine.Random.Range(25f, 50f),
            UnityEngine.Random.Range(25f, 50f)
        );

        rb.AddForce(forceAmount, ForceMode.Impulse);
    }
}

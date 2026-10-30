
using System;
using System.Collections.Generic;
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

    public bool isHeavy = false;

    public List<GameObject> carriedByPlayers = new List<GameObject>();

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
        if (spriteRenderer == null || visuals == null || id == -1)
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

    void Update()
    {
        Carried();
    }

    void OnTriggerEnter(Collider triggerInfo)
    {
        PickedUp(triggerInfo);
    }

    public void PickedUp(Collider triggerInfo)
    {
        if (triggerInfo.CompareTag("PickUpCollider"))
        {
            GameObject player = triggerInfo.transform.parent.gameObject;

            if (carriedByPlayers.Contains(player))
            {
                carriedByPlayers.Remove(player);
            }
            else if (carriedByPlayers.Count < 2)
            {
                carriedByPlayers.Add(player);
            }
            return;
        }

        if(triggerInfo.CompareTag("StationCollider"))
        {
            GameObject station = triggerInfo.transform.parent.gameObject;
            carriedByPlayers.Clear();
            transform.position = station.transform.position;
        }
    }

    public void Carried()
    {
        if (carriedByPlayers.Count == 0)
        {
            return;
        }

        if (carriedByPlayers.Count == 1)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                carriedByPlayers[0].transform.position,
                0.03f
            );
        }

        
    }

    private Vector3 GetMeanVector(Vector3[] positions)
    {
        if (positions.Length == 0)
            return Vector3.zero;
        float x = 0f;
        float y = 0f;
        float z = 0f;
        foreach (Vector3 pos in positions)
        {
            x += pos.x;
            y += pos.y;
            z += pos.z;
        }
        return new Vector3(x / positions.Length, y / positions.Length, z / positions.Length);
    }
}

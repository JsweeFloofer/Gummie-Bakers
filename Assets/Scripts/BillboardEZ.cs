using UnityEngine;

public class BillboardEZ : MonoBehaviour
{

    void Update() 
    {
        transform.LookAt(Camera.main.transform.position, Vector3.up);
    }
}

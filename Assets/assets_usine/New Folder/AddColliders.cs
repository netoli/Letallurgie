using UnityEngine;

public class AddColliders : MonoBehaviour
{
    [ContextMenu("Add Mesh Colliders to Children")]
    void AddMeshColliders()
    {
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshes)
        {
            if (mf.GetComponent<Collider>() == null)
            {
                mf.gameObject.AddComponent<MeshCollider>();
            }
        }
        Debug.Log($"{meshes.Length} colliders ajoutés !");
    }
}
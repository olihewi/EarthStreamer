using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SaveMesh : MonoBehaviour
{
    public string filePath = "Assets/mesh.asset";

    #if UNITY_EDITOR
    [ContextMenu("Save Mesh")]
    public void Save()
    {
        AssetDatabase.CreateAsset(GetComponent<MeshFilter>().sharedMesh, filePath);
        AssetDatabase.SaveAssets();
    }
    #endif
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressableMeshLoader : MonoBehaviour
{
    [SerializeField] private AssetReference mesh;

    private void Awake()
    {
        Load();
    }

    private void OnDestroy()
    {
        Unload();
    }

    private /*async*/ void Load()
    {
        
    }

    private void Unload()
    {
        
    }
}

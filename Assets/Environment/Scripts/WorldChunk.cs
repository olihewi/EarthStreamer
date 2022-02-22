using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Environment
{
  public class WorldChunk : MonoBehaviour
  {
    [Serializable]
    public class ChunkLOD
    {
      public AssetReference assetReference;
    }

    [SerializeField] private ChunkLOD[] levelsOfDetail = new ChunkLOD[0];
    
    private int currentLevelOfDetail = -1;
    private GameObject lodObject;
    private KeyValuePair<AsyncOperationHandle<GameObject>, GameObject> tracker;

    public void UpdateLOD(int _newLevelOfDetail)
    {
      if (_newLevelOfDetail == currentLevelOfDetail) return;
      currentLevelOfDetail = _newLevelOfDetail;
      LoadLOD(currentLevelOfDetail);
    }

    private async void LoadLOD(int _levelOfDetail)
    {
      UnloadPreviousLOD();
      if (_levelOfDetail >= 0)
      {
        AssetReference newLOD = levelsOfDetail[Math.Min(_levelOfDetail, levelsOfDetail.Length - 1)].assetReference;
        AsyncOperationHandle<GameObject> handle = newLOD.InstantiateAsync(transform.position, transform.rotation, transform);
        await handle.Task;
        lodObject = handle.Result;
        tracker = new KeyValuePair<AsyncOperationHandle<GameObject>, GameObject>(handle, lodObject);
      }
    }

    private void UnloadPreviousLOD()
    {
      if (tracker.Value == null) return;
      Addressables.ReleaseInstance(tracker.Key);
      Addressables.ReleaseInstance(tracker.Value);
    }

    #if UNITY_EDITOR
    private Object prefabObject;
    public void OpenPrefab()
    {
      if (prefabObject != null) return;
      prefabObject = PrefabUtility.InstantiatePrefab(levelsOfDetail[0].assetReference.editorAsset, transform);
    }
    public bool ClosePrefab()
    {
      if (prefabObject == null) return false;
      PrefabUtility.ApplyObjectOverride(prefabObject,AssetDatabase.GetAssetPath(levelsOfDetail[0].assetReference.editorAsset), InteractionMode.AutomatedAction);
      DestroyImmediate(prefabObject);
      return true;
    }
    #endif
  }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
      LoadLOD();
    }

    private async void LoadLOD()
    {
      UnloadPreviousLOD();
      if (currentLevelOfDetail >= 0)
      {
        AssetReference newLOD = levelsOfDetail[Math.Min(currentLevelOfDetail, levelsOfDetail.Length - 1)].assetReference;
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
  }
}
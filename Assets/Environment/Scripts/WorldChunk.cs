using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
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

    [SerializeField] private List<ChunkLOD> levelsOfDetail = new List<ChunkLOD>();
    
    private int currentLevelOfDetail = -1;
    private GameObject lodObject;
    private Dictionary<AsyncOperationHandle<GameObject>, GameObject> trackers = new Dictionary<AsyncOperationHandle<GameObject>, GameObject>();

    public void UpdateLOD(int _newLevelOfDetail)
    {
      if (_newLevelOfDetail == currentLevelOfDetail) return;
      LoadLOD(_newLevelOfDetail);
    }

    private async void LoadLOD(int _levelOfDetail)
    {
      if (_levelOfDetail >= 0)
      {
        AssetReference newLOD = levelsOfDetail[Math.Min(_levelOfDetail, levelsOfDetail.Count - 1)].assetReference;
        AsyncOperationHandle<GameObject> handle = newLOD.InstantiateAsync(transform.position, transform.rotation, transform);
        await handle.Task;
        UnloadPreviousLOD();
        lodObject = handle.Result;
        trackers.Add(handle, lodObject);
      }
      else
      {
        UnloadPreviousLOD();
      }
      currentLevelOfDetail = _levelOfDetail;
    }

    private void UnloadPreviousLOD()
    {
      foreach (KeyValuePair<AsyncOperationHandle<GameObject>, GameObject> tracker in trackers)
      {
        if (tracker.Value == null) continue;
        Addressables.ReleaseInstance(tracker.Key);
        Addressables.ReleaseInstance(tracker.Value);
      }
      trackers.Clear();
    }

    #if UNITY_EDITOR
    [HideInInspector] public Object prefabObject;
    public Object OpenPrefab(int _lod)
    {
      if (prefabObject != null) return prefabObject;
      if (!levelsOfDetail[_lod].assetReference.IsValid())
      {
        GameObject temp = new GameObject(gameObject.name);
        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(temp, "Assets/Environment/Chunks/LOD" + _lod + "/" + gameObject.name + ".prefab");
        DestroyImmediate(temp);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.DefaultGroup;
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newPrefab)), group, false, false);
        entry.address = AssetDatabase.GetAssetPath(newPrefab);
        AssetDatabase.Refresh();
        levelsOfDetail[_lod].assetReference = new AssetReference(entry.guid);
      }
      prefabObject = PrefabUtility.InstantiatePrefab(levelsOfDetail[_lod].assetReference.editorAsset, transform);
      return prefabObject;
    }
    public bool ClosePrefab()
    {
      if (prefabObject == null) return false;
      GameObject go = (GameObject) prefabObject;
      go.transform.parent = null;
      PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
      DestroyImmediate(prefabObject);
      return true;
    }
    #endif
  }
}
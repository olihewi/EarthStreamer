using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Environment
{
  public class WorldStreamer : MonoBehaviour
  {
    [SerializeField] private Camera mainCamera;
    [Header("Settings")]
    public float[] chunkLodRanges = new float[0];
    public bool ignoreY = true;

    [Header("Auto-Chunking")]
    public WorldChunk chunkPrefab;
    public float chunkSize;
    public Vector3 chunkOffset;

    private Dictionary<Vector3Int, WorldChunk> chunks = new Dictionary<Vector3Int, WorldChunk>();

    public static WorldStreamer INSTANCE;

    private void OnEnable()
    {
      if (INSTANCE != null)
      {
        Destroy(gameObject);
        return;
      }
      INSTANCE = this;
      chunks = new Dictionary<Vector3Int, WorldChunk>();
      foreach (WorldChunk chunk in FindObjectsOfType<WorldChunk>())
      {
        Vector3 pos = chunk.transform.position/ chunkSize - chunkOffset;
        Vector3Int key = Vector3Int.FloorToInt(pos);
        if (chunks.ContainsKey(key))
        {
          Debug.Log("Duplicate Chunk? " + key);
          continue;
        }
        chunks.Add(key,chunk);
      }
      mainCamera = Camera.main;
    }
    private void Update()
    {
      foreach (KeyValuePair<Vector3Int,WorldChunk> chunkPair in chunks)
      {
        WorldChunk chunk = chunkPair.Value;
        Vector3 camPos = mainCamera.transform.position;
        Vector3 chunkPos = chunk.transform.position;
        if (ignoreY) camPos.y = chunkPos.y = 0;
        float sqrDistance = Vector3.SqrMagnitude(camPos - chunkPos);
        int lod = -1;
        for (int i = 0; i < chunkLodRanges.Length; i++)
        {
          if (sqrDistance > chunkLodRanges[i] * chunkLodRanges[i]) continue;
          lod = i;
          break;
        }
        chunk.UpdateLOD(lod);
      }

      if (Input.GetKeyDown(KeyCode.Space))
        Resources.UnloadUnusedAssets();
    }
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
      if (mainCamera == null) return;
      foreach (float lod in chunkLodRanges)
      {
        Gizmos.DrawWireSphere(mainCamera.transform.position,lod);
      }
    }

    [ContextMenu("Auto Chunk")]
    public void AutoChunk()
    {
      foreach (GameObject go in GameObject.FindGameObjectsWithTag("LOD0"))
      {
        ChunkObject(go, 0);
      }
      foreach (GameObject go in GameObject.FindGameObjectsWithTag("LOD1"))
      {
        ChunkObject(go, 1);
      }
    }

    private void ChunkObject(GameObject _go, int _lod)
    {
      if (_go.GetComponent<WorldChunk>() != null) return;
      Vector3 pos = _go.transform.position / chunkSize - chunkOffset;
      Vector3Int key = Vector3Int.RoundToInt(pos);
      if (!chunks.ContainsKey(key))
      {
        chunks.Add(key, Instantiate(chunkPrefab, (key + chunkOffset) * chunkSize, Quaternion.identity, transform));
        chunks[key].name = "Chunk " + key.x + ", " + key.y + ", " + key.z;
      }
      WorldChunk chunk = chunks[key];
      chunk.OpenPrefab(_lod);
      _go.transform.parent = ((GameObject) chunk.prefabObject).transform;
      chunk.ClosePrefab();
    }
    #endif
  }
}
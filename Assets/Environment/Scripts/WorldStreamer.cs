using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Environment
{
  public class WorldStreamer : MonoBehaviour
  {
    [Header("Settings")]
    public float[] chunkLodRanges = new float[0];
    public bool ignoreY = true;

    [Header("Auto-Chunking")]
    public WorldChunk chunkPrefab;
    public float chunkSize;
    public Vector3 chunkOffset;

    private Dictionary<Vector3Int, WorldChunk> chunks = new Dictionary<Vector3Int, WorldChunk>();
    private Camera mainCamera;

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
    }

    private void Start()
    {
      mainCamera = Camera.main;
    }
    
    void Update()
    {
      foreach (KeyValuePair<Vector3Int,WorldChunk> chunkPair in chunks)
      {
        WorldChunk chunk = chunkPair.Value;
        Vector3 camPos = mainCamera.transform.position;
        Vector3 chunkPos = chunk.transform.position;
        if (ignoreY)
        {
          camPos.y = 0;
          chunkPos.y = 0;
        }
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

    [ContextMenu("Auto Chunk")]
    public void AutoChunk()
    {
      for (int i = 0; i < transform.childCount; i++)
      {
        Transform child = transform.GetChild(i);
        if (child.GetComponent<WorldChunk>() != null) continue;
        Vector3 pos = child.position / chunkSize - chunkOffset;
        Vector3Int key = Vector3Int.RoundToInt(pos);
        if (!chunks.ContainsKey(key))
        {
          chunks.Add(key, Instantiate(chunkPrefab, (key + chunkOffset) * chunkSize, Quaternion.identity, transform));
          chunks[key].name = "Chunk " + key.x + ", " + key.y + ", " + key.z;
        }
        WorldChunk chunk = chunks[key];
        chunk.OpenPrefab();
        child.parent = ((GameObject) chunk.prefabObject).transform;
        i--;
        chunk.ClosePrefab();
      }
    }
  }
}
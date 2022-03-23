using System;
using System.Collections;
using System.Collections.Generic;
using Maps;
using UnityEngine;

namespace Maps
{
  public class OSMStreamer : MonoBehaviour
  {
    public Vector2 startLatLong;
    public OSMChunk chunkPrefab;
    [Header("Chunks")]
    public float chunkSize = 0.025F;
    [SerializeField] private Transform target;
    public float LODStep = 1.5F;
    public int maxLOD;
    private static Dictionary<Vector2Int, OSMChunk> chunks = new Dictionary<Vector2Int, OSMChunk>();

    private void Update()
    {
      Vector2 playerLatLong = new Vector2(target.position.x, target.position.z) / 111319.444F + startLatLong;
      Vector2Int playerChunk = Vector2Int.RoundToInt(playerLatLong / chunkSize);
      int range = Mathf.FloorToInt((maxLOD+1) * LODStep);
      for (int x = playerChunk.x - range; x < playerChunk.x + range; x++)
      {
        for (int y = playerChunk.y - range; y < playerChunk.y + range; y++)
        {
          Vector2Int thisChunkPos = new Vector2Int(x,y);
          float dist = (thisChunkPos - playerLatLong / chunkSize).magnitude;
          if (dist > range) continue;
          if (!chunks.ContainsKey(thisChunkPos))
          {
            Vector2 thisLatLong = new Vector2(thisChunkPos.x,thisChunkPos.y) * chunkSize;
            Vector2 thisWorldPos = (thisLatLong - startLatLong) * 111319.444F;
            Vector3 position = new Vector3(thisWorldPos.x,0.0F,thisWorldPos.y);
            OSMChunk elevationChunk = Instantiate(chunkPrefab, position, Quaternion.identity, transform);
            elevationChunk.boundingBox = new Rect(thisLatLong - Vector2.one * (chunkSize * 0.5F), Vector2.one * chunkSize);
            elevationChunk.currentLOD = -1;
            elevationChunk.filePath = $"{Application.streamingAssetsPath}/OSM/{elevationChunk.boundingBox.xMin}-{elevationChunk.boundingBox.xMax}-{elevationChunk.boundingBox.yMin}-{elevationChunk.boundingBox.yMax}.xml";
            chunks.Add(thisChunkPos, elevationChunk);
          }
          OSMChunk thisChunk = chunks[thisChunkPos];
          //dist = Mathf.Sqrt(dist);
          int thisLOD = Mathf.FloorToInt(dist / LODStep);
          thisChunk.UpdateLOD(thisLOD);
        }
      }
      ClearChunks();
    }
    
    private void ClearChunks()
    {
      Vector2 playerLatLong = new Vector2(target.position.x, target.position.z) / 111319.444F + startLatLong;
      List<Vector2Int> toRemove = new List<Vector2Int>();
      foreach (KeyValuePair<Vector2Int, OSMChunk> chunkPair in chunks)
      {
        if ((chunkPair.Value.boundingBox.center - playerLatLong).sqrMagnitude > Mathf.Pow((maxLOD + 2) * LODStep * chunkSize,2)) toRemove.Add(chunkPair.Key);
      }
      foreach (Vector2Int remove in toRemove)
      {
        Destroy(chunks[remove].gameObject);
        chunks.Remove(remove);
      }
    }
  }
}


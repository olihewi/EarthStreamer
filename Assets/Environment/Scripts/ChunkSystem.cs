using System.Collections;
using UnityEngine;

namespace Environment
{
  public class ChunkSystem : MonoBehaviour
  {
    [Header("Settings")]
    [SerializeField] private float[] chunkLodRanges = new float[0];

    [SerializeField] private GameObject testChunkObject;
    
    private WorldChunk[] chunks;
    private Camera mainCamera;

    private void Start()
    {
      chunks = FindObjectsOfType<WorldChunk>();
      mainCamera = Camera.main;
    }

    void Update()
    {
      foreach (WorldChunk chunk in chunks)
      {
        float sqrDistance = Vector3.SqrMagnitude(mainCamera.transform.position - chunk.transform.position);
        int lod = -1;
        for (int i = 0; i < chunkLodRanges.Length; i++)
        {
          if (sqrDistance > chunkLodRanges[i] * chunkLodRanges[i]) continue;
          lod = i;
          break;
        }
        chunk.UpdateLOD(lod);
      }
    }

    [ContextMenu("Generate Test Chunks")]
    public void GenerateTestChunks()
    {
      for (int x = -5; x <= 5; x++)
      {
        for (int y = -5; y <= 5; y++)
        {
          GameObject go = Instantiate(testChunkObject, new Vector3(x * 100, 0, y * 100), Quaternion.identity, transform);
          go.name = "Chunk " + x + ", " + y;
        }
      }
    }
  }
}
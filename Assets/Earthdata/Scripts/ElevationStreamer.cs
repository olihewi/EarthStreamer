using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Earthdata
{
  public class ElevationStreamer : MonoBehaviour
  {
    public static ElevationStreamer INSTANCE;
    
    public Vector2 startLatLong;
    
    [SerializeField] private EarthdataStreamer earthdataStreamer;
    [SerializeField] private ElevationChunk chunkPrefab;
    [Header("Chunks")]
    public float chunkSize = 0.025F;
    [SerializeField] private Transform target;
    public float LODStep = 1.5F;
    public int maxLOD;
    private static Dictionary<Vector2Int, short[]> heightData = new Dictionary<Vector2Int, short[]>();
    private static Dictionary<Vector2Int, ElevationChunk> chunks = new Dictionary<Vector2Int, ElevationChunk>();
    private static List<Vector2Int> loadingChunks = new List<Vector2Int>();

    private void Awake()
    {
      if (INSTANCE != null && INSTANCE != this) DestroyImmediate(this);
      else INSTANCE = this;
    }

    private void Start()
    {
      LoadElevation(Vector2Int.FloorToInt(startLatLong));
      target.position = new Vector3(target.position.x, GetHeightAt(startLatLong) + 100.0F, target.position.z);
    }

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
            ElevationChunk elevationChunk = Instantiate(chunkPrefab, position, Quaternion.identity, transform);
            elevationChunk.elevationRect = new Rect(thisLatLong - Vector2.one * (chunkSize * 0.5F), Vector2.one * chunkSize);
            elevationChunk.currentLOD = -1;
            chunks.Add(thisChunkPos, elevationChunk);
          }
          ElevationChunk thisChunk = chunks[thisChunkPos];
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
      foreach (KeyValuePair<Vector2Int, ElevationChunk> chunkPair in chunks)
      {
        if ((chunkPair.Value.elevationRect.center - playerLatLong).sqrMagnitude > Mathf.Pow((maxLOD + 1) * LODStep,2)) toRemove.Add(chunkPair.Key);
      }
      foreach (Vector2Int remove in toRemove)
      {
        Destroy(chunks[remove].gameObject);
        chunks.Remove(remove);
      }
    }

    public static float GetUnblerpedHeightAt(Vector2 _latLong)
    {
      Vector2Int indexCoords = Vector2Int.FloorToInt(_latLong);
      short[] heights = heightData[indexCoords];
      _latLong -= indexCoords;
      _latLong *= 3600.0F;
      Vector2Int pos = Vector2Int.FloorToInt(_latLong);
      return heights[pos.x + pos.y * 3601];
    }
    public static float GetHeightAt(Vector2 _latLong)
    {
      Vector2Int indexCoords = Vector2Int.FloorToInt(_latLong);
      if (!heightData.ContainsKey(indexCoords)) return 0.0F;
      short[] heights = heightData[indexCoords];
      _latLong -= indexCoords;
      _latLong.y = 1.0F - _latLong.y;
      _latLong *= 3600.0F;
      Vector2Int pos = Vector2Int.FloorToInt(_latLong);
      _latLong -= pos;
      // Bilinear Interpolation
      short x0y0 = heights[pos.x + pos.y * 3601];
      short x1y0 = heights[pos.x + 1 + pos.y * 3601];
      short x0y1 = heights[pos.x + (pos.y + 1) * 3601];
      short x1y1 = heights[pos.x + 1 + (pos.y + 1) * 3601];
      return Mathf.Lerp(Mathf.Lerp(x0y0, x1y0, _latLong.x), Mathf.Lerp(x0y1, x1y1, _latLong.x), _latLong.y);
    }

    public static bool IsElevationReady(Vector2 _latLong)
    {
      Vector2Int key = Vector2Int.FloorToInt(_latLong);
      bool ready = heightData.ContainsKey(key);
      bool loading = loadingChunks.Contains(key);
      if (!ready && !loading)
      {
        loadingChunks.Add(key);
        Task.Run(() => LoadElevation(Vector2Int.FloorToInt(_latLong)));
      }
      return ready && !loading;
    }

    [ContextMenu("Download Elevation")]
    private void DownloadElevation()
    {
      Task.Run(() => LoadElevation(Vector2Int.FloorToInt(startLatLong)));
    }

    private static async void LoadElevation(Vector2Int _latLong)
    {
      string latLongString = "";
      latLongString += _latLong.y >= 0 ? $"N{_latLong.y}" : $"S{Math.Abs(_latLong.y)}";
      latLongString += _latLong.x >= 0 ? $"E{_latLong.x:000}" : $"W{Math.Abs(_latLong.x):000}";
      string filePath = $"{Application.streamingAssetsPath}/SRTM/{latLongString}.hgt";
      Stream stream;
      if (File.Exists(filePath)) stream = File.OpenRead(filePath);
      else
      {
        stream = await Task.Run(() => EarthdataStreamer.INSTANCE.GetResource($"https://e4ftl01.cr.usgs.gov//DP133/SRTM/SRTMGL1.003/2000.02.11/{latLongString}.SRTMGL1.hgt.zip"));
        FileStream fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);
        fileStream.Close();
      }
      heightData[_latLong] = new short[12967201];
      BinaryReader reader = new BinaryReader(stream);
      for (int i = 0; i < heightData[_latLong].Length; i++)
      {
        // SRTM Data is Big-Endian so the bytes need to be flipped.
        byte firstByte = reader.ReadByte();
        heightData[_latLong][i] = BitConverter.ToInt16(new[] {reader.ReadByte(), firstByte}, 0);
      }
      loadingChunks.Remove(_latLong);
    }
  }
}


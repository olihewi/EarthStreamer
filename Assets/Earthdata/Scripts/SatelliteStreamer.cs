using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Earthdata
{
  public class SatelliteStreamer : MonoBehaviour
  {
    public class TextureData
    {
      public Color[] colors;
      public Vector2Int size;
      public int refCount = 0;
      public Vector3Int position;

      public Color GetPixel(Vector2 _uv)
      {
        //_uv.y = 1.0F - _uv.y;
        return colors[Mathf.FloorToInt(_uv.x * size.x) + ((size.y - 1 - Mathf.FloorToInt(_uv.y * size.y)) * size.x)];
      }

      public int ClampListIndex(int _i, int _size)
      {
        return (_i + _size) % _size;
      }
    }
    public static SatelliteStreamer INSTANCE;
    private static Dictionary<Vector3Int, TextureData> textures = new Dictionary<Vector3Int, TextureData>();
    private static Dictionary<Vector3Int, Task<TextureData>> currentlyDownloading = new Dictionary<Vector3Int, Task<TextureData>>();
    private string accessToken;
    public int maxZoomLevel = 15;
    public string tilesetName = "mapbox.satellite";

    private async void Awake()
    {
      if (INSTANCE != null && INSTANCE != this) DestroyImmediate(this);
      else INSTANCE = this;
      accessToken = PlayerPrefs.GetString("mapbox_token");
      await DownloadTexture(Vector3Int.zero);
    }


    public static Vector2 LatLongToMercator(Vector2 _latLong)
    {
      float latRad = _latLong.y * Mathf.Deg2Rad;
      return new Vector2(
        (_latLong.x + 180.0F) / 360.0F,
        (1.0F - Mathf.Log(Mathf.Tan(latRad) + (1.0F / Mathf.Cos(latRad))) / Mathf.PI) / 2.0F
        );
    }
    public static Vector3Int MercatorToTileID(Vector2 _mercator, int _zoom)
    {
      int zoomFactor = (int) Math.Pow(2, _zoom);
      return new Vector3Int(Mathf.FloorToInt(_mercator.x * zoomFactor), Mathf.FloorToInt(_mercator.y * zoomFactor), _zoom);
    }
    public static Vector3Int LatLongToTileID(Vector2 _latLong, int _zoom)
    {
      return MercatorToTileID(LatLongToMercator(_latLong), _zoom);
    }
    
    public static async Task ReadyResources(Rect _latLong, int _lod)
    {
      int zoom = INSTANCE.maxZoomLevel - _lod;
      Vector3Int ul = LatLongToTileID(_latLong.min, zoom);
      Vector3Int br = LatLongToTileID(_latLong.max, zoom);
      List<Task<TextureData>> tasks = new List<Task<TextureData>>();
      for (int x = ul.x; x <= br.x; x++)
      {
        for (int y = br.y; y <= ul.y; y++)
        {
          Vector3Int xyz = new Vector3Int(x,y,zoom);
          if (!textures.ContainsKey(xyz))
          {
            if (!currentlyDownloading.ContainsKey(xyz))
            {
              currentlyDownloading.Add(xyz,DownloadTexture(xyz));
            }
            tasks.Add(currentlyDownloading[xyz]);
          }
        }
      }
      await Task.WhenAll(tasks);
      foreach (Task<TextureData> task in tasks)
      {
        TextureData result = task.Result;
        if (result == null) continue;
        currentlyDownloading.Remove(result.position);
        textures.Add(result.position, result);
      }
      for (int x = ul.x; x <= br.x; x++)
      {
        for (int y = br.y; y <= ul.y; y++)
        {
          Vector3Int xyz = new Vector3Int(x, y, zoom);
          textures[xyz].refCount++;
        }
      }
    }

    public static void UnloadResources(Rect _latLong, int _lod)
    {
      int zoom = INSTANCE.maxZoomLevel - _lod;
      Vector3Int ul = LatLongToTileID(_latLong.min, zoom);
      Vector3Int br = LatLongToTileID(_latLong.max, zoom);
      for (int x = ul.x; x <= br.x; x++)
      {
        for (int y = br.y; y <= ul.y; y++)
        {
          Vector3Int xyz = new Vector3Int(x, y, zoom);
          if (!textures.ContainsKey(xyz)) continue;
          if (--textures[xyz].refCount <= 0) textures.Remove(xyz);
        }
      }
    }
    public static Color GetColourAt(Vector2 _latLong, int _lod)
    {
      int zoom = INSTANCE.maxZoomLevel - _lod;
      int zoomFactor = (int) Math.Pow(2, zoom);
      Vector2 mercator = LatLongToMercator(_latLong);
      Vector3Int tile = MercatorToTileID(mercator, zoom);
      Vector2 uv = new Vector2((mercator.x * zoomFactor) % 1.0F, (mercator.y * zoomFactor) % 1.0F);
      //if (uv.x < 0.0F || uv.y < 0.0F || uv.x >= 1.0F || uv.y >= 1.0F) return Color.magenta;
      return textures[tile].GetPixel(uv);
    }
    
    private static async Task<TextureData> DownloadTexture(Vector3Int _texturePos)
    {
      string tile = $"{INSTANCE.tilesetName}.{_texturePos.z}.{_texturePos.x}.{_texturePos.y}.jpg";
      string filePath = $"{Application.streamingAssetsPath}/Satellite/{tile}";
      Texture2D texture = new Texture2D(2,2);
      if (File.Exists(filePath))
      {
        byte[] fileData = File.ReadAllBytes(filePath);
        texture.LoadImage(fileData);
      }
      else
      {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture($"https://api.mapbox.com/v4/{INSTANCE.tilesetName}/{_texturePos.z}/{_texturePos.x}/{_texturePos.y}.jpg90?access_token={INSTANCE.accessToken}");
        www.SendWebRequest();
        while (!www.isDone) await Task.Delay(100);
        if (www.result != UnityWebRequest.Result.Success) return null;
        texture = DownloadHandlerTexture.GetContent(www);
        File.WriteAllBytes(filePath,texture.EncodeToJPG(90));
      }
      return new TextureData { colors = texture.GetPixels(), size = new Vector2Int(texture.width, texture.height), position = _texturePos };
    }
  } 
}

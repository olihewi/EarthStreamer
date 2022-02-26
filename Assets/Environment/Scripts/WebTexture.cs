using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebTexture : MonoBehaviour
{
    [Range(0,14)] public int zoomLevel;
    public Vector2Int coords;
    private int prevZoom = -1;
    private Vector2Int prevCoords = -Vector2Int.one;
    
    public float heightScale = 1.0F;

    private Texture2D texture;

    private void Start()
    {
        StartCoroutine(Load());
    }

    private void Update()
    {
        if (zoomLevel != prevZoom || coords != prevCoords)
        {
            coords.x = (int) Mathf.Clamp(coords.x, 0, Mathf.Pow(2.0F, zoomLevel) - 1.0F);
            coords.y = (int) Mathf.Clamp(coords.y, 0, Mathf.Pow(2.0F, zoomLevel) - 1.0F);
            StopAllCoroutines();
            StartCoroutine(Load());
            prevZoom = zoomLevel;
            prevCoords = coords;
        }
    }

    private IEnumerator Load()
    {
        string address = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/" + zoomLevel + "/" + coords.x + "/" + coords.y + ".png";
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(address);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) yield break;
        texture = DownloadHandlerTexture.GetContent(www);
        
        List<Vector3> vertices = new List<Vector3>();
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color pixel = texture.GetPixel(x, y);
                float height = ((pixel.r * 65536.0F + pixel.g * 256.0F + pixel.b) - 32768.0F) / 256.0F;
                vertices.Add(new Vector3((x / (float)texture.width - 0.5F) * 10.0F, height * heightScale, (y / (float)texture.height - 0.5F) * 10.0F));
                texture.SetPixel(x,y, new Color(height,height,height,1.0F));
            }
        }
        int[] triangles = new int[(texture.width - 1) * (texture.height - 1) * 6];
        for (int ti = 0, vi = 0, y = 0; y < texture.height - 1; y++, vi++)
        {
            for (int x = 0; x < texture.width - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + texture.width;
                triangles[ti + 5] = vi + texture.width + 1;
            }
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        texture.Apply();
        texture.filterMode = FilterMode.Point;
        GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
    }
}

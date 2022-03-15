using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EarthChunk : MonoBehaviour
{
    [Range(0,15)] public int zoomLevel;
    public Vector2Int coords;
    private int prevZoom = -1;
    private Vector2Int prevCoords = -Vector2Int.one;
    public float seaLevel = 0.0F;
    
    
    public float heightScale = 1.0F;

    private Texture2D texture;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;

    private void Update()
    {
        UpdateZoom();
    }

    public void UpdateZoom()
    {
        if (zoomLevel != prevZoom || coords != prevCoords)
        {
            zoomLevel = Math.Max(Math.Min(zoomLevel, 15), 0);
            coords.x = (int) Mathf.Clamp(coords.x, 0, Mathf.Pow(2.0F, zoomLevel) - 1.0F);
            coords.y = (int) Mathf.Clamp(coords.y, 0, Mathf.Pow(2.0F, zoomLevel) - 1.0F);
            Load();
            prevZoom = zoomLevel;
            prevCoords = coords;
        }
    }

    public void Load()
    {
        StopAllCoroutines();
        StartCoroutine(I_Load());
    }

    private IEnumerator I_Load()
    {
        string address = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/" + zoomLevel + "/" + coords.x + "/" + coords.y + ".png";
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(address);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) yield break;
        texture = DownloadHandlerTexture.GetContent(www);

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color pixel = texture.GetPixel(x, y);
                float height = ((pixel.r * 65536.0F + pixel.g * 256.0F + pixel.b) - 32768.0F) / 256.0F;
                vertices.Add(new Vector3((x / (float)texture.width - 0.5F) * 10.0F, height * heightScale * (zoomLevel + 1), (y / (float)texture.height - 0.5F) * 10.0F));
                uvs.Add(new Vector2(x / (float)texture.width, y / (float)texture.width));
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
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;

        texture.Apply();
        /*float hDiff = highestH - lowestH;
        Debug.Log(highestH + ", " + lowestH);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color p = texture.GetPixel(x, y);
                float c = p.r;
                c = (c - lowestH) / hDiff;
                texture.SetPixel(x,y,new Color(c,c,c,1.0F));
            }
        }*/
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}

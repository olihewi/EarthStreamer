using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using Earthdata;
using Maps.Features;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Maps
{
  public class OSMChunk : MonoBehaviour
  {
    [Header("Overpass API Query")]
    public string filePath;
    public Rect boundingBox;

    [Header("Mesh Generation")]
    public HighwayNetwork highwayNetwork;
    public MeshFilter newMeshPrefab;

    public Node[] nodes;
    public Way[] ways;
    public Relation[] relations;

    public int currentLOD = -1;

    private void Awake()
    {
      Generate();
    }

    public void UpdateLOD(int _newLOD)
    {
      if (currentLOD == _newLOD || !ElevationStreamer.IsElevationReady(boundingBox.min) || !ElevationStreamer.IsElevationReady(boundingBox.max) ) return;
      currentLOD = _newLOD;
      if (currentLOD == 0) Generate();
    }

    public void ClearMesh()
    {
      while (transform.childCount != 0)
      {
        DestroyImmediate(transform.GetChild(0).gameObject);
      }
    }

    [ContextMenu("Generate")]
    public async void Generate()
    {
      XDocument document = await Task.Run(GetOrRequestData);
      MapFeature.RegisterFeatureGenerators();
      await Task.Run(() => GenerateData(document));
      Dictionary<MapFeature, MapFeature.FeatureMeshData> featureTypes = new Dictionary<MapFeature, MapFeature.FeatureMeshData>();
      await Task.Run(() => GenerateMeshes(featureTypes));
      ClearMesh();
      // Create a new mesh GameObject for each feature generator type
      foreach (KeyValuePair<MapFeature, MapFeature.FeatureMeshData> featurePair in featureTypes)
      {
        MeshFilter meshFilter = Instantiate(newMeshPrefab, transform.position, Quaternion.identity, transform);
        meshFilter.gameObject.name = featurePair.Key.name;
        MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
        MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
        meshRenderer.sharedMaterials = featurePair.Key.materials;
        // Create the new mesh
        Mesh mesh = new Mesh {indexFormat = IndexFormat.UInt32, vertices = featurePair.Value.vertices.ToArray(), triangles = featurePair.Value.triangles.ToArray(), uv = featurePair.Value.uvs.ToArray()};
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        foreach (KeyValuePair<GameObject, Vector3> prefab in featurePair.Value.prefabsToInstantiate)
        {
          GameObject go = Instantiate(prefab.Key, meshFilter.transform);
          go.transform.localPosition = prefab.Value;
          go.transform.rotation = Quaternion.Euler(0.0F, Random.Range(0.0F,360.0F), 0.0F);
        }
      }
      // TODO: Make this async too
      highwayNetwork.GenerateNetwork(ways);
      highwayNetwork.GenerateMeshes();
      StartCoroutine(PopIn(1.0F));
    }

    private async Task<XDocument> GetOrRequestData()
    {
      if (File.Exists(filePath)) return XDocument.Load(filePath);
      string bounds = $"{boundingBox.yMin},{boundingBox.xMin},{boundingBox.yMax},{boundingBox.xMax}";
      HttpWebRequest request = WebRequest.CreateHttp($"http://overpass-api.de/api/interpreter?data=[out:xml][timeout:25];(node({bounds});way({bounds});relation({bounds}););out body;>;out skel qt;");
      request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
      HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
      Stream stream = response.GetResponseStream();
      StreamReader reader = new StreamReader(stream);
      XDocument doc = XDocument.Parse(await reader.ReadToEndAsync());
      doc.Save(filePath);
      return doc;
    }

    public void GenerateData(XDocument _document)
    {
      XElement response = _document.Element("osm");
      Vector2 centrePoint = boundingBox.center;
      // Nodes
      Dictionary<long, Node> nodeDict = new Dictionary<long, Node>();
      foreach (XElement nodeElement in response.Elements("node"))
      {
        Node node = new Node(nodeElement, centrePoint);
        if (!nodeDict.ContainsKey(node.id)) nodeDict.Add(node.id, node);
      }
      // Ways
      Dictionary<long, Way> wayDict = new Dictionary<long, Way>();
      foreach (XElement wayElement in response.Elements("way"))
      {
        Way way = new Way(wayElement, nodeDict);
        if (!wayDict.ContainsKey(way.id)) wayDict.Add(way.id, way);
      }
      // Relations
      List<Relation> relationList = new List<Relation>();
      foreach (XElement relationElement in response.Elements("relation"))
      {
        relationList.Add(new Relation(relationElement, nodeDict, wayDict));
      }
      // To Arrays
      int i = 0;
      nodes = new Node[nodeDict.Count];
      foreach (KeyValuePair<long, Node> nodePair in nodeDict)
      {
        nodes[i++] = nodePair.Value;
      }
      i = 0;
      ways = new Way[wayDict.Count];
      foreach (KeyValuePair<long, Way> wayPair in wayDict)
      {
        ways[i++] = wayPair.Value;
      }
      relations = relationList.ToArray();
      
      highwayNetwork.GenerateNetwork(ways);
    }
    
    public void GenerateMeshes(Dictionary<MapFeature, MapFeature.FeatureMeshData> featureTypes)
    {
      foreach (Way way in ways)
      {
        MapFeature generator = MapFeature.GetFeatureGenerator(way);
        if (generator == null || generator.elementType != MapFeature.MapElement.Way) continue;
        if (!featureTypes.ContainsKey(generator)) featureTypes.Add(generator, new MapFeature.FeatureMeshData());
        MapFeature.FeatureMeshData meshData = featureTypes[generator];
        MapFeature.FeatureMeshData newData = generator.GetMesh(way, meshData.triOffset);
        meshData.vertices.AddRange(newData.vertices);
        meshData.triangles.AddRange(newData.triangles);
        meshData.uvs.AddRange(newData.uvs);
        meshData.triOffset = meshData.vertices.Count;
      }
      foreach (Node node in nodes)
      {
        MapFeature generator = MapFeature.GetFeatureGenerator(node);
        if (generator == null || generator.elementType != MapFeature.MapElement.Node) continue;
        if (!featureTypes.ContainsKey(generator)) featureTypes.Add(generator, new MapFeature.FeatureMeshData());
        MapFeature.FeatureMeshData meshData = featureTypes[generator];
        MapFeature.FeatureMeshData newData = generator.GetMesh(node, meshData.triOffset);
        meshData.vertices.AddRange(newData.vertices);
        meshData.triangles.AddRange(newData.triangles);
        meshData.uvs.AddRange(newData.uvs);
        meshData.triOffset = meshData.vertices.Count;
        meshData.prefabsToInstantiate.AddRange(newData.prefabsToInstantiate);
      }
      foreach (Relation relation in relations)
      {
        MapFeature generator = MapFeature.GetFeatureGenerator(relation);
        if (generator == null || generator.elementType != MapFeature.MapElement.Relation) continue;
        if (!featureTypes.ContainsKey(generator)) featureTypes.Add(generator, new MapFeature.FeatureMeshData());
        MapFeature.FeatureMeshData meshData = featureTypes[generator];
        MapFeature.FeatureMeshData newData = generator.GetMesh(relation, meshData.triOffset);
        meshData.vertices.AddRange(newData.vertices);
        meshData.triangles.AddRange(newData.triangles);
        meshData.uvs.AddRange(newData.uvs);
        meshData.triOffset = meshData.vertices.Count;
      }
    }

    private IEnumerator PopIn(float _time)
    {
      Vector3 end = new Vector3(transform.position.x,0.0F,transform.position.z);
      Vector3 start = transform.position + Vector3.down * 10.0F;
      float t = 0.0F;
      while (t < 1.0F)
      {
        transform.position = Vector3.Lerp(start, end, t);
        t += Time.deltaTime / _time;
        yield return null;
      }
      transform.position = end;
    }

    [ContextMenu("Request New Data from Overpass")]
    public async void RequestNewData()
    {
      HttpWebRequest request = WebRequest.CreateHttp($"http://overpass-api.de/api/interpreter?data=[out:xml][timeout:25];(node({boundingBox.y - boundingBox.height},{boundingBox.x},{boundingBox.y},{boundingBox.x + boundingBox.width});way({boundingBox.y - boundingBox.height},{boundingBox.x},{boundingBox.y},{boundingBox.x + boundingBox.width});relation({boundingBox.y - boundingBox.height},{boundingBox.x},{boundingBox.y},{boundingBox.x + boundingBox.width}););out body;>;out skel qt;");
      request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
      HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
      Stream stream = response.GetResponseStream();
      StreamReader reader = new StreamReader(stream);
      XDocument doc = XDocument.Parse(await reader.ReadToEndAsync());
      doc.Save(filePath);
    }
  }
}
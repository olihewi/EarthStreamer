using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Maps.Features;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Maps
{
  public class HighwayNetwork : MonoBehaviour
  {
    public class HighwayElement
    {
      public Node node;
      public List<HighwayConnection> connections = new List<HighwayConnection>();
      public List<Vector3> normals = new List<Vector3>();
      public Path pathType;
      public float distSincePrev = 0.0F;
      // TODO: Add multiple lanes and one-ways.
    }

    public class HighwayConnection
    {
      public HighwayElement element;
      public bool traversable = true;
    }
    
    [Serializable]
    public class HighwayLayer
    {
      public List<Path> connections;
    }
    
    // TODO: Make this persistent
    [HideInInspector] public List<HighwayElement> highwayNetwork;
    
    [Header("Network Generation Settings")]
    public List<HighwayLayer> networkLayers = new List<HighwayLayer>();
    
    public void GenerateNetwork(IEnumerable<Way> _ways)
    {
      Dictionary<HighwayLayer, Dictionary<Node, HighwayElement>> layerNetwork = new Dictionary<HighwayLayer, Dictionary<Node, HighwayElement>>();
      foreach (HighwayLayer layer in networkLayers)
      {
        layerNetwork.Add(layer,new Dictionary<Node, HighwayElement>());
      }

      foreach (Way way in _ways)
      {
        if (!way.tags.ContainsKey("highway")) continue;
        bool oneWay = way.tags.ContainsKey("oneway") && way.tags["oneway"] == "yes";
        Path pathGenerator = (Path) MapFeature.GetFeatureGenerator(way);
        if (pathGenerator == null) continue;
        Vector3 prevPos = way.nodes[0].chunkPos;
        for (int i = 0; i < way.nodes.Length; i++)
        {
          foreach (KeyValuePair<HighwayLayer, Dictionary<Node, HighwayElement>> layerPair in layerNetwork)
          {
            if (!layerPair.Key.connections.Contains(pathGenerator)) continue;
            Node node = way.nodes[i];
            if (layerPair.Value.ContainsKey(node))
            {
              if (layerPair.Value[node].pathType.priority > pathGenerator.priority) continue;
              layerPair.Value[node].pathType = pathGenerator;
              continue;
            }
            Vector3 pos = node.chunkPos + Vector3.up * pathGenerator.yOffset;
            layerPair.Value.Add(node, new HighwayElement{node = node, pathType = pathGenerator, distSincePrev = Vector3.Distance(pos, prevPos)});
            prevPos = pos;
          }
        }

        foreach (KeyValuePair<HighwayLayer, Dictionary<Node, HighwayElement>> layerPair in layerNetwork)
        {
          if (!layerPair.Key.connections.Contains(pathGenerator)) continue;
          // Update the connections for the first and last nodes
          layerPair.Value[way.nodes[0]].connections.Add(new HighwayConnection{element = layerPair.Value[way.nodes[1]]});
          layerPair.Value[way.nodes[way.nodes.Length-1]].connections.Add(new HighwayConnection{element = layerPair.Value[way.nodes[way.nodes.Length-2]], traversable = !oneWay});
          // Update the connections for the middle nodes
          for (int i = 1; i < way.nodes.Length - 1; i++)
          {
            HighwayElement element = layerPair.Value[way.nodes[i]];
            element.connections.Add(new HighwayConnection{element = layerPair.Value[way.nodes[i-1]], traversable = !oneWay});
            element.connections.Add(new HighwayConnection{element = layerPair.Value[way.nodes[i+1]]});
          }
        }
      }
      
      highwayNetwork = new List<HighwayElement>();
      foreach (KeyValuePair<HighwayLayer, Dictionary<Node, HighwayElement>> layerPair in layerNetwork)
      {
        foreach (KeyValuePair<Node, HighwayElement> elementPair in layerPair.Value)
        {
          elementPair.Value.connections.Sort((_a, _b) =>
          {
            int a = (int) Vector3.SignedAngle(Vector3.forward, _b.element.node.chunkPos - elementPair.Value.node.chunkPos, Vector3.up);
            int b = (int) Vector3.SignedAngle(Vector3.forward, _a.element.node.chunkPos - elementPair.Value.node.chunkPos, Vector3.up);
            if (a < 0) a = 360 + a;
            if (b < 0) b = 360 + b;
            return b - a;
          });
          highwayNetwork.Add(elementPair.Value);
        }
      }
    }
    
    public void GenerateMeshes()
    {
      // Register Path Types
      Dictionary<Path, MapFeature.FeatureMeshData> pathTypes = new Dictionary<Path, MapFeature.FeatureMeshData>();
      Dictionary<Path, MeshFilter> pathFilters = new Dictionary<Path, MeshFilter>();
      foreach (Path pathType in Resources.LoadAll<Path>(""))
      {
        MeshRenderer meshRenderer = new GameObject(pathType.name,new [] {typeof(MeshFilter), typeof(MeshRenderer)}).GetComponent<MeshRenderer>();
        MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
        meshRenderer.sharedMaterials = pathType.materials;
        meshRenderer.transform.parent = transform;
        meshRenderer.transform.position = transform.position;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        pathTypes.Add(pathType, new MapFeature.FeatureMeshData());
        pathFilters.Add(pathType, meshFilter);
      }
      // Generate normals for each node (TODO: Look into putting this in GenerateNetwork)
      foreach (HighwayElement element in highwayNetwork)
      {
        if (element.pathType == null) continue;
        if (element.connections.Count == 1)
        {
          Vector3 forward = (element.node.chunkPos - element.connections[0].element.node.chunkPos).normalized;
          float width = element.pathType.width * 0.5F;
          element.normals.Add(new Vector3(forward.z * width, element.pathType.yOffset, -forward.x * width) + element.node.chunkPos);
          element.normals.Add(new Vector3(-forward.z * width, element.pathType.yOffset, forward.x * width) + element.node.chunkPos);
          continue;
        }
        for (int i = 0; i < element.connections.Count; i++)
        {
          Vector3 inDir = (element.node.chunkPos - element.connections[MapFeature.ClampListIndex(i - 1, element.connections.Count)].element.node.chunkPos).normalized;
          Vector3 outDir = (element.connections[MapFeature.ClampListIndex(i, element.connections.Count)].element.node.chunkPos - element.node.chunkPos).normalized;
          Vector3 forward = (inDir + outDir).normalized;
          float inAngle = Vector3.SignedAngle(inDir, forward, Vector3.up);

          float width = element.pathType.width * 0.5F * (Mathf.Max(Mathf.Sin(Mathf.Deg2Rad * -inAngle) * 0.66F, 0.0F) + 1.0F);
          element.normals.Add(new Vector3(-forward.z * width, element.pathType.yOffset, forward.x * width) + element.node.chunkPos);
        }
      }
      /* Triangulate the mesh, finally! */
      foreach (HighwayElement element in highwayNetwork)
      {
        if (element.pathType == null) continue;
        MapFeature.FeatureMeshData meshData = pathTypes[element.pathType];
        for (int i = 0; i < element.connections.Count; i++)
        {
          HighwayElement connection = element.connections[i].element;
          Vector3 diff = connection.node.chunkPos - element.node.chunkPos;
          if (diff.x < diff.y) continue; // Only do this in once direction.
          int j = 0;
          foreach (HighwayConnection otherConnection in connection.connections)
          {
            if (otherConnection.element == element) break;
            j++;
          }
          float dist = Vector3.Distance(element.node.chunkPos, connection.node.chunkPos);
          if (element.connections.Count <= 2 && connection.connections.Count <= 2)
          {
            meshData.vertices.AddRange(new []
            {
              element.normals[i], connection.normals[MapFeature.ClampListIndex(j+1, connection.normals.Count)], connection.normals[j], element.normals[MapFeature.ClampListIndex(i+1, element.normals.Count)]
            });
            meshData.triangles.AddRange(new []
            {
              meshData.triOffset, meshData.triOffset+1, meshData.triOffset+3,
              meshData.triOffset+1, meshData.triOffset+2, meshData.triOffset+3
            });
            meshData.uvs.AddRange(new []
            {
              new Vector2(0.0F, element.distSincePrev), new Vector2(0.0F, element.distSincePrev + dist), new Vector2(1.0F, element.distSincePrev + dist), new Vector2(1.0F, element.distSincePrev),   
            });
          }
          else
          {
            List<Vector3> nodes = new List<Vector3>();
            nodes.AddRange(new []
            {
              element.node.chunkPos, element.normals[i], connection.normals[MapFeature.ClampListIndex(j+1,connection.normals.Count)], connection.node.chunkPos, connection.normals[j], element.normals[MapFeature.ClampListIndex(i+1, element.normals.Count)]
            });
            MapFeature.TriangulateConvex(nodes, meshData);
            meshData.uvs.AddRange(new []
            {
              new Vector2(0.5F,element.distSincePrev), new Vector2(0.0F,element.distSincePrev), new Vector2(0.0F,element.distSincePrev + dist),
              new Vector2(0.5F,element.distSincePrev + dist), new Vector2(1.0F,element.distSincePrev + dist), new Vector2(1.0F,element.distSincePrev),  
            });
          }
          meshData.triOffset = meshData.vertices.Count;
        }
      }
      /* Apply the meshes, at last! */
      foreach (KeyValuePair<Path, MeshFilter> filterPair in pathFilters)
      {
        MapFeature.FeatureMeshData meshData = pathTypes[filterPair.Key];
        for (int i = 0; i < meshData.vertices.Count; i++)
        {
          meshData.vertices[i] += Vector3.up;
        }
        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertices.ToArray();
        mesh.triangles = meshData.triangles.ToArray();
        mesh.uv = meshData.uvs.ToArray();
        mesh.RecalculateNormals();
        filterPair.Value.sharedMesh = mesh;
      }
    }
    
    [Header("Debug")]
    [SerializeField] private bool visualize = false;
    private void OnDrawGizmosSelected()
    {
      if (!visualize || highwayNetwork == null) return;
      foreach (HighwayElement element in highwayNetwork)
      {
        Random.InitState(element.pathType.priority);
        Gizmos.color = Random.ColorHSV(0.0F, 1.0F, 1.0F,1.0F);
        foreach (HighwayConnection connection in element.connections)
        {
          Vector3 diff = connection.element.node.chunkPos - element.node.chunkPos;
          if (!connection.traversable) Gizmos.DrawLine(element.node.chunkPos, connection.element.node.chunkPos);
        }
      }
    }
  }
}
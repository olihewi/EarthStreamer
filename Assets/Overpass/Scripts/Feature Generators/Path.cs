using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Maps.Features
{
  [CreateAssetMenu(menuName = "Maps/Feature Generators/Path")]
  public class Path : MapFeature
  {
    public class HighwayElement
    {
      public Vector3 pos;
      public List<HighwayElement> connections = new List<HighwayElement>();
      public List<Vector3> normals = new List<Vector3>();
      public Path pathType;
    }

    public static Dictionary<long,HighwayElement> highwayNetwork = new Dictionary<long, HighwayElement>();

    // TODO: This function is probably pretty awful for performance. Look into Jobs / BurstCompile
    public static void GenerateHighwayNetwork(Dictionary<long, Vector3> _nodes, IEnumerable<XElement> _ways)
    {
      highwayNetwork = new Dictionary<long, HighwayElement>();
      foreach (XElement way in _ways)
      {
        /* If this is a highway... */
        bool isHighway = false;
        foreach (XElement tag in way.Elements("tag"))
        {
          if (tag.FirstAttribute.Value != "highway") continue;
          isHighway = true;
          break;
        }
        if (!isHighway) continue;
        /* Add this node to the highway network */
        XElement[] nodeReferences = way.Elements("nd").ToArray();
        long[] ids = new long[nodeReferences.Length];
        for (int i = 0; i < nodeReferences.Length; i++)
        {
          ids[i] = long.Parse(nodeReferences[i].FirstAttribute.Value);
        }
        foreach (long id in ids)
        {
          // TODO: Use priorities for the feature generators
          if (highwayNetwork.ContainsKey(id)) continue;
          Path pathGenerator = (Path) GetFeatureGenerator(way);
          highwayNetwork.Add(id, new HighwayElement{pos = _nodes[id], pathType = pathGenerator});
        }
        /* Update the connections for the first and last nodes */
        highwayNetwork[ids[0]].connections.Add(highwayNetwork[ids[1]]);
        highwayNetwork[ids[ids.Length - 1]].connections.Add(highwayNetwork[ids[ids.Length - 2]]);
        /* Update the connections for the middle nodes */
        for (int i = 1; i < ids.Length - 1; i++)
        {
          HighwayElement element = highwayNetwork[ids[i]];
          element.connections.Add(highwayNetwork[ids[i-1]]);
          element.connections.Add(highwayNetwork[ids[i+1]]);
        }
      }
      foreach (KeyValuePair<long, HighwayElement> elementPair in highwayNetwork)
      {
        elementPair.Value.connections.Sort((_a, _b) =>
        {
          int a = (int) Vector3.SignedAngle(Vector3.forward, _b.pos - elementPair.Value.pos, Vector3.up);
          int b = (int) Vector3.SignedAngle(Vector3.forward, _a.pos - elementPair.Value.pos, Vector3.up);
          if (a < 0) a = 360 + a;
          if (b < 0) b = 360 + b;
          return b - a;
        });
      }
      Debug.Log($"Successfully generated Highway Network ({highwayNetwork.Count} nodes)");
    }
    public static void GenerateHighwayMeshes(Transform _parent)
    {
      /* Register Path Types */
      Dictionary<Path, FeatureMeshData> pathTypes = new Dictionary<Path, FeatureMeshData>();
      Dictionary<Path, MeshFilter> pathFilters = new Dictionary<Path, MeshFilter>();
      foreach (Path pathType in Resources.LoadAll<Path>(""))
      {
        MeshRenderer meshRenderer = new GameObject(pathType.name,new [] {typeof(MeshFilter), typeof(MeshRenderer)}).GetComponent<MeshRenderer>();
        MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
        meshRenderer.sharedMaterials = pathType.materials;
        meshRenderer.transform.parent = _parent;
        pathTypes.Add(pathType, new FeatureMeshData());
        pathFilters.Add(pathType, meshFilter);
      }
      /* Generate normals for each node */
      foreach (KeyValuePair<long, HighwayElement> nodePair in highwayNetwork)
      {
        if (nodePair.Value.pathType == null) continue;
        HighwayElement element = nodePair.Value;
        for (int i = 0; i < element.connections.Count; i++)
        {
          // TODO: account for concave angles
          Vector3 inDir = (element.pos - element.connections[ClampListIndex(i - 1, element.connections.Count)].pos).normalized;
          Vector3 outDir = (element.connections[ClampListIndex(i, element.connections.Count)].pos - element.pos).normalized;
          Vector3 forward = (inDir + outDir).normalized;
          float inAngle = Vector3.SignedAngle(inDir, forward, Vector3.up);

          float width = element.pathType.width * 0.5F * (Mathf.Max(Mathf.Sin(Mathf.Deg2Rad * -inAngle) * 0.66F, 0.0F) + 1.0F);
          element.normals.Add(new Vector3(-forward.z * width, 0.0F, forward.x * width) + element.pos);
        }
      }
      /* Triangulate the mesh, finally! */
      foreach (KeyValuePair<long, HighwayElement> nodePair in highwayNetwork)
      {
        if (nodePair.Value.pathType == null) continue;
        HighwayElement element = nodePair.Value;
        FeatureMeshData meshData = pathTypes[nodePair.Value.pathType];
        for (int i = 0; i < element.connections.Count; i++)
        {
          HighwayElement connection = element.connections[i];
          Vector3 diff = connection.pos - element.pos;
          if (diff.x < diff.y) continue; // Only do this in once direction.
          int j = 0;
          foreach (HighwayElement otherConnection in connection.connections)
          {
            if (otherConnection == element) break;
            j++;
          }
          List<Vector3> nodes = new List<Vector3>();
          nodes.AddRange(new []
          {
            element.pos, element.normals[i], connection.normals[ClampListIndex(j+1,connection.normals.Count)], connection.pos, connection.normals[j], element.normals[ClampListIndex(i+1, element.normals.Count)]
          });
          TriangulateConvex(nodes, meshData);
          meshData.uvs.AddRange(new []
          {
            new Vector2(0.5F,0.0F), new Vector2(0.0F,0.0F), new Vector2(0.0F,1.0F),
            new Vector2(0.5F,1.0F), new Vector2(1.0F,1.0F), new Vector2(1.0F,0.0F),  
          });
          meshData.triOffset = meshData.vertices.Count;
        }
      }
      /* Apply the meshes, at last! */
      foreach (KeyValuePair<Path, MeshFilter> filterPair in pathFilters)
      {
        FeatureMeshData meshData = pathTypes[filterPair.Key];
        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertices.ToArray();
        mesh.triangles = meshData.triangles.ToArray();
        mesh.uv = meshData.uvs.ToArray();
        mesh.RecalculateNormals();
        filterPair.Value.sharedMesh = mesh;
      }

    }

    [Header("Default Generation Settings")]
    public float width = 1.0F;
    public float yOffset = 0.02F;

    public override FeatureMeshData GetMesh(XElement _way, List<Vector3> _nodes, int _triOffset)
    {
      return new FeatureMeshData();
      //Vector3 offset = Vector3.up * yOffset;
      //for (int i = 0; i < _nodes.Count; i++)
      //{
      //  _nodes[i] += offset;
      //}
      //
      ///* Getting Road Info */
      //float pathWidth = width;
      //bool definedWidth = false;
      //
      //foreach (XElement tag in _way.Elements("tag"))
      //{
      //  switch (tag.Attribute("k").Value)
      //  {
      //    case "width":
      //      pathWidth = float.Parse(tag.Attribute("v").Value);
      //      definedWidth = true;
      //      break;
      //    case "highway":
      //      if (definedWidth) break;
      //      foreach (HighwayDefaults type in widths)
      //      {
      //        if (type.typeIdentifier == tag.Attribute("v").Value)
      //        {
      //          pathWidth = type.width;
      //        }
      //      }
      //      break;
      //  }
      //}
      //
      //FeatureMeshData meshData = new FeatureMeshData();
      //meshData.triOffset = _triOffset;
      //TriangulatePath(_nodes, meshData, pathWidth);
      //return meshData;
    }

    protected void TriangulatePath(List<Vector3> _nodes, FeatureMeshData _meshData, float _width)
    {
      // TODO: Work out how to handle branching roads (this will be hard!)
      int triOffsetBefore = _meshData.triOffset - _meshData.vertices.Count;
      float totalDist = 0;
      for (int i = 0; i < _nodes.Count; i++)
      {
        Vector3 forward = Vector3.zero;
        if (i < _nodes.Count - 1)
        {
          forward += _nodes[i + 1] - _nodes[i];
          _meshData.triangles.AddRange(new []
          {
            _meshData.triOffset + i*2,
            _meshData.triOffset + i*2 + 2,
            _meshData.triOffset + i*2 + 1,
            _meshData.triOffset + i*2 + 1,
            _meshData.triOffset + i*2 + 2,
            _meshData.triOffset + i*2 + 3,
          });
          
        }
        if (i > 0)
        {
          forward += _nodes[i] - _nodes[i - 1];
        }

        if ((i == 0 || i == _nodes.Count - 1) && _nodes[0] == _nodes[_nodes.Count - 1])
        {
          forward = (_nodes[1] - _nodes[0]) + (_nodes[0] - _nodes[_nodes.Count - 2]);
        }
        Vector3 left = new Vector3(-forward.z, 0.0F, forward.x);
        left.Normalize();
        _meshData.vertices.Add(_nodes[i] + left * _width * 0.5F);
        _meshData.vertices.Add(_nodes[i] - left * _width * 0.5F);
        _meshData.uvs.Add(new Vector2(0.0F,totalDist));
        _meshData.uvs.Add(new Vector2(1.0F, totalDist));
        totalDist += forward.magnitude;
      }
      _meshData.triOffset = triOffsetBefore + _meshData.vertices.Count;
    }
  }
}
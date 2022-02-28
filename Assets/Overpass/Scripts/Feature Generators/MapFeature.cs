using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Maps.Features
{
  public abstract class MapFeature : ScriptableObject
  {
    public enum MapElement
    {
      Node,
      Way,
      Relation
    }

    [Serializable]
    public struct GenerationCondition
    {
      public string key;
      public string value;
    }

    [Tooltip("The type of map element this will generate on. See https://wiki.openstreetmap.org/wiki/Elements")]
    public MapElement elementType = MapElement.Way;
    [Tooltip("Higher values will take precedence over lower values.")]
    public int priority = 0;
    [Tooltip("A list of cases for which this feature will generate.")]
    public List<GenerationCondition> generationConditions;

    [Header("Materials")] 
    public Material[] materials = new Material[1];

    public class FeatureMeshData
    {
      public List<Vector3> vertices = new List<Vector3>();
      public List<int> triangles = new List<int>();
      public List<Vector2> uvs = new List<Vector2>();
      public int triOffset = 0;
    }

    public class TriangleVertexData
    {
      public int index;
      public Vector3 pos;
      public bool convex = false;
      public TriangleVertexData next;
      public TriangleVertexData prev;
    }
    
    private static List<MapFeature> GENERATOR_TYPES = new List<MapFeature>();

    public static void RegisterFeatureGenerators()
    {
      GENERATOR_TYPES = new List<MapFeature>();
      GENERATOR_TYPES.AddRange(Resources.LoadAll<MapFeature>(""));
      MapFeature[] mapFeatures = new MapFeature[1];
      // TODO: Make sure this sorts the right way.
      GENERATOR_TYPES.Sort((_a, _b) => _b.priority - _a.priority);
    }

    public static MapFeature GetFeatureGenerator(XElement _way)
    {
      Dictionary<string,string> wayTags = new Dictionary<string, string>();
      foreach (XElement tag in _way.Elements("tag"))
      {
        KeyValuePair<string, string> thisTag = new KeyValuePair<string, string>(tag.Attribute("k").Value, tag.Attribute("v").Value);
        wayTags.Add(thisTag.Key, thisTag.Value);
      }
      foreach (MapFeature generator in GENERATOR_TYPES)
      {
        bool conditionsMet = true;
        foreach (GenerationCondition condition in generator.generationConditions)
        {
          // TODO: Make all conditions need to be met.
          if (!(wayTags.ContainsKey(condition.key) && (condition.value == "" || wayTags[condition.key].Contains(condition.value))))
          {
            conditionsMet = false;
            break;
          }
        }
        if (conditionsMet)
          return generator;
      }
      return null;
    }
    
    public abstract FeatureMeshData GetMesh(XElement _way, List<Vector3> _nodes, int _triOffset);
    
    protected static void ExtrudeWalls(List<Vector3> _nodes, FeatureMeshData _meshData, float _floorHeight, float _height)
    {
      int triOffsetBefore = _meshData.triOffset - _meshData.vertices.Count;
      if (!IsPolygonClockwise(_nodes)) _nodes.Reverse();
      Vector3 floorPos = Vector3.up * _floorHeight;
      Vector3 roofPos = Vector3.up * _height;
      float uvX = 0.0F;
      for (int i = 0; i < _nodes.Count - 1; i++)
      {
        _meshData.vertices.AddRange(new[]
        {
          _nodes[i] + floorPos,
          _nodes[i] + roofPos,
          _nodes[i + 1] + roofPos,
          _nodes[i + 1] + floorPos
        });
        _meshData.triangles.AddRange(new[]
        {
          _meshData.triOffset,
          _meshData.triOffset + 3,
          _meshData.triOffset + 1,
          _meshData.triOffset + 1,
          _meshData.triOffset + 3,
          _meshData.triOffset + 2
        });
        float dist = Vector3.Distance(_nodes[i], _nodes[i + 1]);
        _meshData.uvs.AddRange(new []
        {
          new Vector2(uvX,_floorHeight),
          new Vector2(uvX,_height),
          new Vector2(uvX + dist, _height),
          new Vector2(uvX + dist, _floorHeight), 
        });
        uvX += dist;
        _meshData.triOffset += 4;
      }

      _meshData.triOffset = triOffsetBefore + _meshData.vertices.Count;
    }

    protected static void TriangulatePolygon(List<Vector3> _nodes, FeatureMeshData _meshData, bool facingDown = false)
    {
      int triOffsetBefore = _meshData.triOffset - _meshData.vertices.Count;
      if (IsPolygonClockwise(_nodes) == facingDown) _nodes.Reverse();
      foreach (Vector3 node in _nodes)
      {
        // TODO: Use material scaling
        _meshData.uvs.Add(new Vector2(node.x, node.z));
      }
      if (IsPolygonConvex(_nodes)) TriangulateConvex(_nodes, _meshData);
      else TriangulateConcave(_nodes, _meshData);
      _meshData.triOffset = triOffsetBefore + _meshData.vertices.Count;
    }

    protected static void TriangulateConvex(List<Vector3> _nodes, FeatureMeshData _meshData)
    {
      _meshData.vertices.AddRange(_nodes);
      for (int i = 2; i < _nodes.Count; i++)
      {
        _meshData.triangles.AddRange(new[]
        {
          _meshData.triOffset,
          _meshData.triOffset + i - 1,
          _meshData.triOffset + i
        });
      }
    }

    protected static void TriangulateConcave(List<Vector3> _nodes, FeatureMeshData _meshData)
    {
      _meshData.vertices.AddRange(_nodes);
      /* Create List of TriangleVertexData */
      List<TriangleVertexData> vertices = _nodes.Select((t, i) => new TriangleVertexData {index = i, pos = t}).ToList();
      /* Register Vertex Neighbours */
      foreach (TriangleVertexData vertex in vertices)
      {
        vertex.prev = vertices[ClampListIndex(vertex.index - 1, vertices.Count)];
        vertex.next = vertices[ClampListIndex(vertex.index + 1, vertices.Count)];
      }

      /* Work out whether each vertex is convex */
      foreach (TriangleVertexData vertex in vertices)
      {
        vertex.convex = IsVertexConvex(vertex);
      }

      /* Check if the vertex is an ear */
      List<TriangleVertexData> earVertices = vertices.Where(vertex => IsEar(vertex, vertices)).ToList();
      /* Triangulation */
      while (vertices.Count > 3 && earVertices.Count > 0)
      {
        /* Make a Triangle of the first ear */
        TriangleVertexData earVertex = earVertices[0];
        TriangleVertexData earPrev = earVertex.prev;
        TriangleVertexData earNext = earVertex.next;
        _meshData.triangles.AddRange(new[]
        {
          earPrev.index + _meshData.triOffset,
          earVertex.index + _meshData.triOffset,
          earNext.index + _meshData.triOffset,
        });
        earVertices.Remove(earVertex);
        vertices.Remove(earVertex);
        /* Update the previous and next vertices */
        earPrev.next = earNext;
        earNext.prev = earPrev;
        /* Find if we have created a new ear */
        earPrev.convex = IsVertexConvex(earPrev);
        earNext.convex = IsVertexConvex(earNext);

        earVertices.Remove(earPrev);
        earVertices.Remove(earNext);
        if (IsEar(earPrev, vertices)) earVertices.Add(earPrev);
        if (IsEar(earNext, vertices)) earVertices.Add(earNext);
      }

      _meshData.triangles.AddRange(IsVertexConvex(vertices[0])
        ? new[]
        {
          vertices[0].prev.index + _meshData.triOffset,
          vertices[0].index + _meshData.triOffset,
          vertices[0].next.index + _meshData.triOffset,
        }
        : new[]
        {
          vertices[0].index + _meshData.triOffset,
          vertices[0].prev.index + _meshData.triOffset,
          vertices[0].next.index + _meshData.triOffset,
        });
    }

    protected static bool IsTriangleClockwise(Vector3 _a, Vector3 _b, Vector3 _c)
    {
      float determinant = _a.x * _b.z + _c.x * _a.z + _b.x * _c.z - _a.x * _c.z - _c.x * _b.z - _b.x * _a.z;
      return determinant < 0.0F;
    }

    protected static bool IsPolygonClockwise(List<Vector3> _points)
    {
      float edgeSum = 0.0F;
      for (int i = 0; i < _points.Count - 1; i++)
      {
        edgeSum += (_points[i + 1].x - _points[i].x) * (_points[i + 1].z + _points[i].z);
      }

      return edgeSum > 0.0F;
    }

    protected static bool IsPolygonConvex(List<Vector3> _points)
    {
      // TODO: Check if making this physically accurate improves performance
      return _points.Count == 4;
    }

    protected static bool IsPointInTriangle(Vector3 _a, Vector3 _b, Vector3 _c, Vector3 _p)
    {
      float denominator = ((_b.z - _c.z) * (_a.x - _c.x) + (_c.x - _b.x) * (_a.z - _c.z));

      float a = ((_b.z - _c.z) * (_p.x - _c.x) + (_c.x - _b.x) * (_p.z - _c.z)) / denominator;
      float b = ((_c.z - _a.z) * (_p.x - _c.x) + (_a.x - _c.x) * (_p.z - _c.z)) / denominator;
      float c = 1.0F - a - b;

      return a > 0.0F && a < 1.0F && b > 1.0F && b < 1.0F && c > 0.0F && c < 1.0F;
    }

    protected static bool IsVertexConvex(TriangleVertexData _vertex)
    {
      return IsTriangleClockwise(_vertex.prev.pos, _vertex.pos, _vertex.next.pos);
    }

    protected static bool IsEar(TriangleVertexData _v, List<TriangleVertexData> _vertices)
    {
      if (!_v.convex) return false;
      foreach (TriangleVertexData vertex in _vertices)
      {
        if (vertex.convex) continue;
        if (IsPointInTriangle(_v.prev.pos, _v.pos, _v.next.pos, vertex.pos))
        {
          return false;
        }
      }

      return true;
    }

    protected static int ClampListIndex(int _i, int _size)
    {
      return (_i + _size) % _size;
    }
  }
}
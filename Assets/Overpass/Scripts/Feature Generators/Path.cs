using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Maps.Features
{
  [CreateAssetMenu(menuName = "Maps/Feature Generators/Path")]
  public class Path : MapFeature
  {
    // TODO: Remove these once proper feature determination is done
    [Serializable]
    public struct HighwayDefaults
    {
      public string typeIdentifier;
      public float width;
    }
    
    [Header("Default Generation Settings")]
    // TODO: Remove these once proper feature determination is done
    public float width = 3.6F;
    public List<HighwayDefaults> widths = new List<HighwayDefaults>();
    
    public float yOffset = 0.02F;

    public override FeatureMeshData GetMesh(XElement _way, List<Vector3> _nodes, int _triOffset)
    {
      Vector3 offset = Vector3.up * yOffset;
      for (int i = 0; i < _nodes.Count; i++)
      {
        _nodes[i] += offset;
      }
      
      /* Getting Road Info */
      float pathWidth = width;
      bool definedWidth = false;
      
      foreach (XElement tag in _way.Elements("tag"))
      {
        switch (tag.Attribute("k").Value)
        {
          case "width":
            pathWidth = float.Parse(tag.Attribute("v").Value);
            definedWidth = true;
            break;
          case "highway":
            if (definedWidth) break;
            foreach (HighwayDefaults type in widths)
            {
              if (type.typeIdentifier == tag.Attribute("v").Value)
              {
                pathWidth = type.width;
              }
            }
            break;
        }
      }
      
      FeatureMeshData meshData = new FeatureMeshData();
      meshData.triOffset = _triOffset;
      TriangulatePath(_nodes, meshData, pathWidth);
      return meshData;
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
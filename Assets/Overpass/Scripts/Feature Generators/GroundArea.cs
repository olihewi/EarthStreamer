using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Maps.Features
{
  
  [CreateAssetMenu(menuName = "Maps/Feature Generators/Ground Area")]
  public class GroundArea : MapFeature
  {
    public float yOffset = 0.01F;
    public override FeatureMeshData GetMesh(XElement _way, List<Vector3> _nodes, int _triOffset)
    {
      Vector3 offset = Vector3.up * yOffset;
      for (int i = 0; i < _nodes.Count; i++)
      {
        _nodes[i] += offset;
      }
      FeatureMeshData meshData = new FeatureMeshData();
      meshData.triOffset = _triOffset;
      TriangulatePolygon(_nodes, meshData);
      return meshData;
    }
  }
}
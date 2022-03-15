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
    public override FeatureMeshData GetMesh(Way _way, int _triOffset)
    {
      Vector3 offset = Vector3.up * yOffset;
      List<Vector3> nodes = new List<Vector3>();
      foreach (Node node in _way.nodes)
      {
        nodes.Add(node.chunkPos + offset);
      }
      FeatureMeshData meshData = new FeatureMeshData();
      meshData.triOffset = _triOffset;
      //TriangulatePolygon(nodes, meshData);
      TriangulateDelaunay(nodes, meshData);
      return meshData;
    }

    public override FeatureMeshData GetMesh(Node _node, int _triOffset)
    {
      return new FeatureMeshData();
    }
    public override FeatureMeshData GetMesh(Relation _relation, int _triOffset)
    {
      return new FeatureMeshData();
    }
  }
}
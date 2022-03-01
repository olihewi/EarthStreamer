using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Maps.Features
{
  [CreateAssetMenu(menuName = "Maps/Feature Generators/Building")]
  public class Building : MapFeature
  {
    [Header("Default Generation Settings")]
    [Tooltip("The number of visible levels (floors) in the building")]
    public int defaultLevels = 1;
    [Tooltip("The height of the building in meters.")]
    public float defaultHeight = 6.0F;

    public override FeatureMeshData GetMesh(Way _way, int _triOffset)
    {
      // Get Building Data
      int buildingLevels = defaultLevels;
      float buildingHeight = defaultHeight;
      float buildingMinHeight = 0.0F;

      if (_way.tags.ContainsKey("height")) buildingHeight = float.Parse(_way.tags["height"]);
      if (_way.tags.ContainsKey("min_height")) buildingMinHeight = float.Parse(_way.tags["min_height"]);
      if (_way.tags.ContainsKey("building:levels")) buildingLevels = int.Parse(_way.tags["building:levels"]);
      if (buildingHeight == defaultHeight && buildingLevels != defaultLevels) buildingHeight *= buildingLevels / 2.0F;

      // Extruding Walls
      List<Vector3> nodes = new List<Vector3>();
      foreach (Node node in _way.nodes)
      {
        nodes.Add(node.position);
      }
      if (!IsPolygonClockwise(nodes)) nodes.Reverse();
      FeatureMeshData meshData = new FeatureMeshData();
      meshData.triOffset = _triOffset;
      
      ExtrudeWalls(nodes, meshData, buildingMinHeight, buildingHeight);
      Vector3 roofPos = Vector3.up * buildingHeight;
      for (int i = 0; i < nodes.Count; i++)
      {
        nodes[i] += roofPos;
      }
      TriangulatePolygon(nodes, meshData);
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


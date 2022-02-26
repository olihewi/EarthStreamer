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

    public override FeatureMeshData GetMesh(XElement _way, List<Vector3> _nodes, int _triOffset)
    {
      /* Getting Building Info */
      int buildingLevels = defaultLevels;
      float buildingHeight = defaultHeight;
      float buildingMinHeight = 0.0F;
      foreach (XElement tag in _way.Elements("tag"))
      {
        switch (tag.Attribute("k").Value)
        {
          case "building:levels":
            buildingLevels = int.Parse(tag.Attribute("v").Value);
            break;
          case "height":
            buildingHeight = float.Parse(tag.Attribute("v").Value);
            break;
          case "min_height":
            buildingMinHeight = float.Parse(tag.Attribute("v").Value);
            break;
        }
      }
      if (buildingHeight == defaultHeight && buildingLevels != defaultLevels) buildingHeight *= buildingLevels / 2.0F;

      /* Extruding Vertices */
      if (!IsPolygonClockwise(_nodes)) _nodes.Reverse();
      FeatureMeshData meshData = new FeatureMeshData();
      meshData.triOffset = _triOffset;
      
      ExtrudeWalls(_nodes, meshData, buildingMinHeight, buildingHeight);
      Vector3 roofPos = Vector3.up * buildingHeight;
      for (int i = 0; i < _nodes.Count; i++)
      {
        _nodes[i] += roofPos;
      }
      TriangulatePolygon(_nodes, meshData);
      /*if (buildingMinHeight > 0.0F)
      {
        Vector3 roofMinusFloor = Vector3.up * (buildingHeight - buildingMinHeight);
        for (int i = 0; i < _nodes.Count; i++)
        {
          _nodes[i] -= roofMinusFloor;
        }
        TriangulatePolygon(_nodes, meshData, true);
      }*/
      return meshData;
    }
  }
}


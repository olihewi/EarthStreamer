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
    [Header("Default Generation Settings")]
    public float width = 1.0F;
    public float yOffset = 0.02F;

    public override FeatureMeshData GetMesh(Way _way, int _triOffset)
    {
      // This is handled by HighwayNetwork.
      return new FeatureMeshData();
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
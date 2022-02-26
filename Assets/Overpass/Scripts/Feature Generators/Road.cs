using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Maps.Features
{
  public class Road : Path
  {
    public int lanes = 2;
    public override FeatureMeshData GetMesh(XElement _way, List<Vector3> _nodes, int _triOffset)
    {
      return base.GetMesh(_way, _nodes, _triOffset);
    }
  }
}
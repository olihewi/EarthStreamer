using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Maps.Features
{
  [CreateAssetMenu(menuName = "Maps/Feature Generators/Node Prefab")]
  public class NodePrefab : MapFeature
  {
    public GameObject prefab;
    public override FeatureMeshData GetMesh(Way _way, int _triOffset)
    {
      return new FeatureMeshData();
    }

    public override FeatureMeshData GetMesh(Node _node, int _triOffset)
    {
      Instantiate(prefab, _node.position, Quaternion.identity);
      return new FeatureMeshData();
    }

    public override FeatureMeshData GetMesh(Relation _relation, int _triOffset)
    {
      return new FeatureMeshData();
    }
  }
}
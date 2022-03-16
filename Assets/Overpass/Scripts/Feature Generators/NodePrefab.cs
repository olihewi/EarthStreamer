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
      List<KeyValuePair<GameObject, Vector3>> prefabsToInstantiate = new List<KeyValuePair<GameObject, Vector3>>();
      prefabsToInstantiate.Add(new KeyValuePair<GameObject, Vector3>(prefab, _node.chunkPos));
      return new FeatureMeshData{prefabsToInstantiate = prefabsToInstantiate};
    }

    public override FeatureMeshData GetMesh(Relation _relation, int _triOffset)
    {
      return new FeatureMeshData();
    }
  }
}
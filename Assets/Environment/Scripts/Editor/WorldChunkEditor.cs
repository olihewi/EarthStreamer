using System;
using UnityEditor;
using UnityEngine;

namespace Environment
{
  [CustomEditor(typeof(WorldChunk), true)]
  public class WorldChunkEditor : Editor
  {
    private WorldChunk chunk
    {
      get { return (WorldChunk) target; }
    }
    public void OnEnable()
    {
      //if (!chunk.ClosePrefab()) chunk.OpenPrefab();
    }
  }
}

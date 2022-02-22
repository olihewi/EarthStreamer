using UnityEditor;

namespace Environment
{
  [CustomEditor(typeof(WorldChunk), true)]
  public class WorldChunkEditor : Editor
  {
    public void OnEnable()
    {
      WorldChunk chunk = (WorldChunk) target;
      if (!chunk.ClosePrefab()) chunk.OpenPrefab();
    }
  }
}

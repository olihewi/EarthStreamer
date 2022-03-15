using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Earthdata
{
  [CustomEditor(typeof(EarthdataStreamer))]
  public class EarthdataStreamerEditor : Editor
  {
    private EarthdataStreamer streamer
    {
      get { return (EarthdataStreamer) target; }
    }
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();
      streamer.password = EditorGUILayout.PasswordField("Password", streamer.password);
    }
  }
}


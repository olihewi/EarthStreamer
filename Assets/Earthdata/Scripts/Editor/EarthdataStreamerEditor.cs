using System;
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

    private string username = "";
    private string password = "";

    public void OnEnable()
    {
      username = PlayerPrefs.GetString("earthdata_username");
      password = PlayerPrefs.GetString("earthdata_password");
    }

    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();
      EditorGUILayout.LabelField("Earthdata Login Credentials", EditorStyles.boldLabel);
      string newUsername = EditorGUILayout.TextField("Username", username);
      if (newUsername != username)
      {
        username = newUsername;
        PlayerPrefs.SetString("earthdata_username", username);
      }
      string newPassword = EditorGUILayout.PasswordField("Password", password);
      if (newPassword != password)
      {
        password = newPassword;
        PlayerPrefs.SetString("earthdata_password", password);
      }
    }
  }
}


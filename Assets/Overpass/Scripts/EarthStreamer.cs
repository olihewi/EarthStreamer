using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthStreamer : MonoBehaviour
{
  public static EarthStreamer INSTANCE;
  
  [Header("Targets")]
  public Vector2 startLatLong;
  public Transform target;
  
  [Header("Chunks")]
  public float chunkSize = 0.025F;
  public float LODStep = 1.5F;
  public int maxLOD;
  
  private void Awake()
  {
    if (INSTANCE != null && INSTANCE != this) DestroyImmediate(this);
    else INSTANCE = this;
  }
}

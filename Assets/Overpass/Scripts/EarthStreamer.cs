using System;
using System.Collections;
using System.Collections.Generic;
using Earthdata;
using Maps;
using UnityEngine;

public class EarthStreamer : MonoBehaviour
{
  public static EarthStreamer INSTANCE;
  
  [Header("Targets")]
  public Vector2 startLatLong;
  public Transform target;

  [Header("Streamers")]
  public EarthdataStreamer earthdataStreamer;
  public ElevationStreamer elevationStreamer;
  public OSMStreamer osmStreamer;
  
  [Header("Chunks")]
  public float chunkSize = 0.025F;
  public float LODStep = 1.5F;
  public int maxLOD;
  
  private void Awake()
  {
    if (INSTANCE != null && INSTANCE != this) DestroyImmediate(this);
    else INSTANCE = this;

    startLatLong = new Vector2(PlayerPrefs.GetFloat("last_longitude", -2.54994893F),PlayerPrefs.GetFloat("last_latitude", 51.5010681F));
    elevationStreamer.startLatLong = startLatLong;
    osmStreamer.startLatLong = startLatLong;
  }
}

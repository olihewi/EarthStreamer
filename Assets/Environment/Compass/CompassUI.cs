using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompassUI : MonoBehaviour
{
  public Transform target;
  public RawImage compass;
  private void Update()
  {
    Rect rect = compass.uvRect;
    rect.x = target.rotation.eulerAngles.y / 360.0F;
    compass.uvRect = rect;
  }
}

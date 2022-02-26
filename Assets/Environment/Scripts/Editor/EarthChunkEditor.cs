using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EarthChunk))]
public class EarthChunkEditor : Editor
{
    private EarthChunk chunk
    {
        get { return (EarthChunk) target; }
    }

    private void OnSceneGUI()
    {
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit) && hit.transform != chunk.transform) return;
        Vector3 localPos = chunk.transform.InverseTransformPoint(hit.point);
        Vector3 selection = new Vector3(1.0F,0.0F,1.0F);
        if (localPos.x < 0.0F) selection.x *= -1;
        if (localPos.z < 0.0F) selection.z *= -1;
        Handles.color = Color.green;
        Vector3 inner = chunk.transform.position;
        Vector3 outer = chunk.transform.TransformPoint(selection * 5.0F);
        Handles.DrawLine(inner, new Vector3(outer.x,inner.y, inner.z), 3.0F);
        Handles.DrawLine(new Vector3(outer.x,inner.y, inner.z), outer, 3.0F);
        Handles.DrawLine(outer, new Vector3(inner.x, inner.y, outer.z), 3.0F);
        Handles.DrawLine( new Vector3(inner.x, inner.y, outer.z), inner, 3.0F);
        if (e.button == 0 && e.type == EventType.MouseDown)
        {
            // Zoom out
            if (e.shift)
            {
                chunk.zoomLevel--;
                chunk.coords = Vector2Int.FloorToInt(new Vector2(chunk.coords.x,chunk.coords.y) / 2.0F);
            }
            else
            {
                chunk.zoomLevel++;
                chunk.coords *= 2;
                if (selection.x > 0.0F) chunk.coords.x++;
                if (selection.z < 0.0F) chunk.coords.y++;
            }
            chunk.UpdateZoom();
        }
        Selection.activeGameObject = chunk.gameObject;
    }
}

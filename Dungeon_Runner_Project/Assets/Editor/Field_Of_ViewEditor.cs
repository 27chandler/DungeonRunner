using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Field_Of_View))]
public class FieldOfViewEditor : Editor
{

    void OnSceneGUI()
    {
        Field_Of_View fow = (Field_Of_View)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(fow.transform.position, Vector3.forward, Vector3.up, 360, fow.viewRadius);

        Handles.color = Color.red;
        foreach (Transform visibleTarget in fow.visibleTargets)
        {
            Handles.DrawLine(fow.transform.position, visibleTarget.position);
        }
    }

}
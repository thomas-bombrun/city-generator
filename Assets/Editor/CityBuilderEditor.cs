using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(CityBuilder))]
public class CityBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityBuilder cityBuilder = (CityBuilder)target;
        if (GUILayout.Button("Build City"))
        {
            cityBuilder.GenerateCity();
        }
        if (GUILayout.Button("Clear"))
        {
            cityBuilder.Clear();
        }
    }
}
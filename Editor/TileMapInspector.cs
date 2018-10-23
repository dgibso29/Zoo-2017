using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(World))]
public class TileMapInspector : Editor{

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        DrawDefaultInspector();
        if (GUILayout.Button("Regenerate"))
        {
            World world = (World)target;
            world.GenerateWorld();
        }
    }

}

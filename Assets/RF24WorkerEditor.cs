using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(RF24Worker))]
public class RF24WorkerEditor : Editor
{
    public override void OnInspectorGUI()
    {   
        DrawDefaultInspector();

        RF24Worker myrf24 = (RF24Worker)target;
        if(GUILayout.Button("Setup")){
            myrf24.setup();
        }
    }
}

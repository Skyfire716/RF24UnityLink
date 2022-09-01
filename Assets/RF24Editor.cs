using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(RF24))]
public class RF24Editor : Editor
{
    public override void OnInspectorGUI()
    {   
        DrawDefaultInspector();

        RF24 myrf24 = (RF24)target;
        if(GUILayout.Button("Start USB Service"))
        {
            myrf24.StartUSBService();
        }
        if(GUILayout.Button("Stop USB Service"))
        {
            myrf24.stopUSBService();
        }
        if(GUILayout.Button("Get Devices")){
            myrf24.ReadUSBDevices();
        }
        if(GUILayout.Button("Connect to RP20402")){
            myrf24.connectToCustomUSBDevice(0x0000, 0x0001);
        }
        if(GUILayout.Button("Communicate")){
            myrf24.talkToRP2040();
        }
        if(GUILayout.Button("BeginRF24")){
            myrf24.RF24Begin();
        }
        if(GUILayout.Button("ToggleRole")){
            myrf24.toggleRole();
        }
    }
}

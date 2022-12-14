using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(USBManagement))]
public class USBManagementEditor : Editor
{
    public override void OnInspectorGUI()
    {   
        DrawDefaultInspector();

        USBManagement usb = (USBManagement)target;
        if(GUILayout.Button("Get Devices")){
            usb.ReadUSBDevices();
        }
        if(GUILayout.Button("Connect to Device")){
            usb.connectToCustomUSBDevice(usb.deviceID[0], usb.deviceID[1]);
        }
        if(GUILayout.Button("Set BootMode")){
            usb.bootMode();
        }
    }
}

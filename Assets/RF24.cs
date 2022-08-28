using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEditor;


public class RF24 : MonoBehaviour
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PlugInCallBack(ushort idVendor, ushort idProduct, bool eventb);
    
    #if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    [DllImport ("Assets/RF24Lib/build/libRF24USBInterace.so")]
    private static extern void setPlugCallBack(PlugInCallBack func);
    [DllImport ("Assets/RF24Lib/build/libRF24USBInterace.so")]
    private static extern int usbTransfer(byte[] outBuf, byte outBufLength, out int bytesOut, byte[] inBuf, byte inBufLength, out int bytesIn);
    [DllImport ("Assets/RF24Lib/build/libRF24USBInterace.so")]
    private static extern int connectToUSBDecive(ushort idVendor, ushort idProduct);
    [DllImport ("Assets/RF24Lib/build/libRF24USBInterace.so")]
    private static extern void get_usb_devices(StringBuilder dst);
    [DllImport ("Assets/RF24Lib/build/libRF24USBInterace.so")]
    private static extern void stop();
    [DllImport ("Assets/RF24Lib/build/libRF24USBInterace.so")]
    private static extern int run();
    #endif
    
    #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    
    #endif
    
    private Thread usbRunnerThread;
    
    private PlugInCallBack callbackPointer;
    
    private int counter = 0;
    
    public String[] devices;
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("RF24 Start");
        StartUSBService();
        setHotPlugCallback();
        //Debug.Log("Main returns: " + main());
        //StringBuilder sb = new StringBuilder (1024);
        //get_usb_devices(sb);
        //Debug.Log("Devices? " + sb.ToString());
    }

    // Update is called once per frame
    void Update()
    {
    }
    
    public void setHotPlugCallback(){
        callbackPointer = USBHotPlugCallback;
        setPlugCallBack(callbackPointer);
    }
    
    public void StartUSBService(){
        if(usbRunnerThread != null){
            usbRunnerThread.Abort();
        }
        usbRunnerThread = new Thread(keepUSBFunctionalityAlive);
        usbRunnerThread.Start();
        Debug.Log("Started USB Runner Thread");
    }
    
    public void ReadUSBDevices(){
        StringBuilder sb = new StringBuilder (8192);
        get_usb_devices(sb);
        Debug.Log("Devices? " + sb.ToString());
        string[] subs = sb.ToString().Split('\n');
        devices = subs;
    }
    
    public void stopUSBService(){
        Debug.Log("Stop Runner Thread");
        stop();
        Debug.Log("Runnter Thread should Stop");
    }
    
    public bool connectToCustomUSBDevice(ushort idVendor, ushort idProduct){
        int ret = connectToUSBDecive(idVendor, idProduct);
        Debug.Log("Connected to Device? " + ret);
        return ret == 0;
    }
    
    public void talkToRP2040(){
        byte[] outBuf = new byte[64];
        for(int i = 0; i < 64; i++){
            outBuf[i] = (byte) i;
        }
        byte outBufLength = 64;
        int bytesOut;
        byte[] inBuf = new byte[64];
        byte inBufLength = 64;
        int bytesIn;
        usbTransfer(outBuf, outBufLength, out bytesOut, inBuf, inBufLength, out bytesIn);
    }
    
    void keepUSBFunctionalityAlive(){
        int returnValue = 0;
        try{
            returnValue = run();
        }catch(Exception e){
            Debug.LogException(e, this);
        }
        Debug.Log("LibUSB Run Thread finished with RetVal: " + returnValue);
    }
    
    static void USBHotPlugCallback(ushort idVendor, ushort idProduct, bool eventb){
        Debug.Log("Device: 0x" + idVendor.ToString("X4") + ":0x" + idProduct.ToString("X4") + " changed " + (eventb ? "Arrived" : "Left"));
    }
}

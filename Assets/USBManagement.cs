using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class USBManagement : MonoBehaviour
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
    
    public delegate void IsDeviceConnected(bool isUsable);
    public event IsDeviceConnected isDeviceConnectedCallback;
    
    private Thread usbRunnerThread;
    
    private Thread commThread;
    
    private PlugInCallBack callbackPointer;
    
    private static bool devicesChanged = false;
    
    public String[] devices;
    
    protected readonly byte BYTEARRAYTRANSFER = 0xAA;
    protected readonly byte BYTEARRAYTRANSFERSINLGE = 0xBB;
    protected readonly byte SETCEPIN = 0xCC;
    protected readonly byte SETCSNPIN = 0xDD;
    
    protected bool running = true;
    
    private bool USBServiceRunning = false;
    
    public ushort[] deviceID = new ushort[]{0x0000, 0x0001};
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("RF24 Start");
        StartUSBService();
        setHotPlugCallback();
    }
    
    // Update is called once per frame
    void Update()
    {
        if(devicesChanged){
            devicesChanged = false;
            ReadUSBDevices();
        }
    }
    
    void OnDestroy(){
        Debug.Log("Stopping USB");
        stopUSBService();
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
        USBServiceRunning = true;
        Debug.Log("Started USB Runner Thread");
    }
    
    public void ReadUSBDevices(){
        if(isUSBServiceRunning()){
            StringBuilder sb = new StringBuilder (8192);
            get_usb_devices(sb);
            Debug.Log("Devices? " + sb.ToString());
            devices = sb.ToString().Split('\n').Where(d => d.Contains(":")).ToArray();
            if(isDeviceConnectedCallback != null && !Array.Exists(devices, d => d.Contains(String.Format("{0:X4}:{1:X4}", deviceID[0], deviceID[1])))){
                isDeviceConnectedCallback(false);
            }
        }else{
            devices = new String[0];
            isDeviceConnectedCallback(false);
        }
    }
    
    public void stopUSBService(){
        Debug.Log("Stop Runner Thread");
        running = false;
        USBServiceRunning = false;
        stop();
        Debug.Log("Runnter Thread should Stop");
    }
    
    public bool isUSBServiceRunning(){
        return USBServiceRunning;
    }
    
    public bool connectToCustomUSBDevice(ushort idVendor, ushort idProduct){
        int ret = connectToUSBDecive(idVendor, idProduct);
        Debug.Log("Connected to Device? " + (ret == 0));
        isDeviceConnectedCallback(ret == 0);
        return ret == 0;
    }
    
    public void setCEPin(byte status){
        byte bufferLengths = 64;
        byte[] inBuf = new byte[bufferLengths];
        byte[] outBuf = new byte[bufferLengths];
        outBuf[63] = SETCEPIN;
        outBuf[0] = status;
        int bytesOut;
        int bytesIn;
        usbTransfer(outBuf, bufferLengths, out bytesOut, inBuf, bufferLengths, out bytesIn);
    }
    
    public void setCSNPin(byte status){
        byte bufferLengths = 64;
        byte[] inBuf = new byte[bufferLengths];
        byte[] outBuf = new byte[bufferLengths];
        outBuf[63] = SETCSNPIN;
        outBuf[0] = status;
        int bytesOut;
        int bytesIn;
        usbTransfer(outBuf, bufferLengths, out bytesOut, inBuf, bufferLengths, out bytesIn);
        if(Application.isEditor){
            float now = Time.realtimeSinceStartup * 1000 * 1000;
            while(Time.realtimeSinceStartup * 1000 * 1000 - now < 5){
                
            }
        }else if(Application.isPlaying){
            float now = Time.time * 1000 * 1000;
            while(Time.time * 1000 * 1000 - now < 5){
                
            }
        }
    }
    
    public static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        int i = 0;
        foreach (byte b in ba){
            hex.Append("," + i++ + ": 0x");
            hex.AppendFormat("{0:X2}", b);
        }
        return hex.ToString();
    }
    
    public Tuple<byte, byte[]> transferByteArrays(byte reg, byte[] dataout){
        byte bufferLengths = 64;
        byte[] inBuf = new byte[bufferLengths];
        byte[] outBuf = new byte[bufferLengths];
        if(dataout.Length == 0){
            outBuf[63] = BYTEARRAYTRANSFERSINLGE;
        }else{
            for(int i = 0; i < dataout.Length; i++){
                outBuf[i + 1] = dataout[i];
            }
            outBuf[63] = BYTEARRAYTRANSFER;
            outBuf[62] = (byte) (dataout.Length + 1);
        }
        outBuf[0] = reg;
        int bytesOut;
        int bytesIn;
        usbTransfer(outBuf, bufferLengths, out bytesOut, inBuf, bufferLengths, out bytesIn);
        //Debug.Log("Wrote: " + bytesOut + " Bytes\tRead: " + bytesIn + " Bytes");
        //Debug.Log("Wrote: " + ByteArrayToString(outBuf));
        //Debug.Log("Read: " + ByteArrayToString(inBuf));
        byte[] dataOut = new byte[dataout.Length];
        for(int i = 0; i < dataout.Length; i++){
            dataout[i] = inBuf[i + 1];
        }
        return Tuple.Create(inBuf[0], dataout);
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
        devicesChanged = true;
    }
    
   
}

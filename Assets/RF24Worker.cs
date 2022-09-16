using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEditor;

public class RF24Worker : MonoBehaviour
{
    
    private RF24 rf24;
    private USBManagement usbManagement;
    private bool isRP2040;
    
    
    
    byte[][] address = new byte[][]{new byte[]{0x00, 0x00, 0x00, 0x31, 0x4E, 0x6F, 0x64, 0x65}, new byte[]{0x00, 0x00, 0x00, 0x32, 0x4E, 0x6F, 0x64, 0x65}};
    // It is very helpful to think of an address as a path instead of as
    // an identifying device destination
    
    // to use different addresses on a pair of radios, we need a variable to
    // uniquely identify which address this radio will use to transmit
    bool radioNumber = true;  // 0 uses address[0] to transmit, 1 uses address[1] to transmit
    
    // Used to control whether this node is sending or receiving
    bool role = false;  // true = TX role, false = RX role
    
    // For this example, we'll be using a payload containing
    // a single float number that will be incremented
    // on every successful transmission
    float payload = 0.0f;
    bool rfSetup = false;
    bool toggle = false;
    DateTime start;
    
    
    // Start is called before the first frame update
    void Start()
    {
        usbManagement = gameObject.GetComponent<USBManagement>();
        rf24 = gameObject.GetComponent<RF24>();
        usbManagement.isDeviceConnectedCallback += new USBManagement.IsDeviceConnected(isConnectedToRP2040);
        start = DateTime.Now;
    }
    
    // Update is called once per frame
    void Update()
    {
        if((new TimeSpan(DateTime.Now.Ticks - start.Ticks)).TotalMilliseconds > 1000){
            if(rfSetup){
                start = DateTime.Now;
                if (role) {
                    // This device is a TX node
                    
                    Debug.LogWarning("Before Writing " + payload);
                    DateTime start = DateTime.Now;
                    foreach(byte b in BitConverter.GetBytes(payload)){
                        Debug.LogWarning(String.Format("{0:X2}", b));
                    }
                    bool report = rf24.write(BitConverter.GetBytes(payload), 4);  // transmit & save the report
                    DateTime end = DateTime.Now;
                    Debug.LogWarning("After Running " +report);
                    
                    if (report) {
                        Debug.Log("Transmission successful! ");
                        Debug.Log("Transmission successful! ");  // payload was delivered
                        Debug.Log("Time to transmit = " + (new TimeSpan(end.Ticks - start.Ticks)).TotalMilliseconds + " us. Sent: " + payload);  // print payload sent
                        payload += 0.01f;          // increment float payload
                    } else {
                        Debug.Log("Transmission failed or timed out");  // payload was not delivered
                    }
                } else {
                    // This device is a RX node
                    
                    byte pipe = 0;
                    if (rf24.available(ref pipe)) {              // is there a payload? get the pipe number that recieved it
                        byte bytes = rf24.getPayloadSize();  // get the size of the payload
                        byte[] payloadBuf = BitConverter.GetBytes(payload);
                        rf24.read(ref payloadBuf, bytes);             // fetch payload from FIFO
                        payload = BitConverter.ToSingle(payloadBuf);
                        Debug.Log("Received " + bytes +" bytes on pipe " + pipe + ": " +payload);  // print the payload's value
                    }else{
                        Debug.Log("No Receive");
                    }
                }  // role
                if(toggle){
                    if(role){
                        rf24.startListening();
                    }else{
                        rf24.stopListening();
                    }
                    role = !role;
                }
            }
        }
    }
    
    public void setup(){
        rf24.setRF24(0);
        rf24.SPIByteArrayTransfer += new RF24.SPITransferByteArraysCallbackHandler(usbManagement.transferByteArrays);
        rf24.SetCEPin += new RF24.SetPin(usbManagement.setCEPin);
        rf24.SetCSNPin += new RF24.SetPin(usbManagement.setCSNPin);
        rf24.begin();
        Debug.Log("Is Chip Connected? " + rf24.isChipConnected());
        rf24.setPALevel(RF24.rf24_pa_dbm_e.RF24_PA_LOW, true);  // RF24_PA_MAX is default.
        
        // save on transmission time by setting the radio to only transmit the
        // number of bytes we need to transmit a float
        rf24.setPayloadSize(4);  // float datatype occupies 4 bytes
        
        // set the TX address of the RX node into the TX pipe
        rf24.openWritingPipe(address[radioNumber ? 0 : 1]);  // always uses pipe 0
        
        // set the RX address of the TX node into a RX pipe
        rf24.openReadingPipe(1, BitConverter.ToUInt64(address[!radioNumber ? 0 : 1], 0));  // using pipe 1
        
        // additional setup specific to the node's role
        if (role) {
            rf24.stopListening();  // put radio in TX mode
        } else {
            rf24.startListening();  // put radio in RX mode
        }
        rfSetup = true;
        /*
         i f(com*mThread *!= null){
         commThread.Abort();
    }
    running = true;
    commThread = new Thread(commVoid);
    commThread.Start();
    Debug.Log("Started Comm Runner Thread");
    */
    }
    
    public void isConnectedToRP2040(bool isConnected){
        isRP2040 = isConnected;
        Debug.Log("Is NRF24Connected? " + isConnected);
        if(!isConnected){
            rfSetup = false;
        }
    }
    
    public void printPretty(){
        rf24.printDetails();
        rf24.printPretty();
    }
    
    public void Trigtoggle(){
        toggle = true;
    }
}

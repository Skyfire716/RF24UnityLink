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
    float start, timer;
    
    
    // Start is called before the first frame update
    void Start()
    {
        usbManagement = gameObject.GetComponent<USBManagement>();
        rf24 = gameObject.GetComponent<RF24>();
        usbManagement.isDeviceConnectedCallback += new USBManagement.IsDeviceConnected(isConnectedToRP2040);
        start = Time.unscaledTime;
        timer = 2;
    }
    
    // Update is called once per frame
    void Update()
    {
        if(rfSetup){
            if (role) {
                // Debug.Log("Time " + Time.unscaledTime);
                // Debug.Log("Time " + (Time.unscaledTime - start));
                if(timer <= 0){
                    // This device is a TX node
                    float start = Time.unscaledTime;
                    Debug.Log("(Write");
                    bool report = rf24.write(BitConverter.GetBytes(payload), 4);  // transmit & save the report
                    Debug.Log(" END Write)");
                    float end = Time.unscaledTime;
                    if (report) {
                        Debug.Log("Transmission successful! ");
                        Debug.Log("Transmission successful! ");  // payload was delivered
                        Debug.Log("Time to transmit = " + (end - start) + " us. Sent: " + payload);  // print payload sent
                        payload += 0.01f;          // increment float payload
                    } else {
                        Debug.Log("Transmission failed or timed out");  // payload was not delivered
                    }
                    start = Time.unscaledTime;
                    timer = 2;
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
                }
            }  // role
            if(toggle){
                toggle = false;
                if(role){
                    role = false;
                    rf24.startListening();
                }else{
                    role = true;
                    rf24.stopListening();
                }
                Debug.Log("TOGGLE");
            }
        }
        timer -= Time.deltaTime;
    }
    
    
    public void setup(){
        rf24.setRF24(0);
        rf24.SPIByteArrayTransfer += new RF24.SPITransferByteArraysCallbackHandler(usbManagement.transferByteArrays);
        rf24.SetCEPin += new RF24.SetPin(usbManagement.setCEPin);
        rf24.SetCSNPin += new RF24.SetPin(usbManagement.setCSNPin);
        Debug.Log("(Begin ");
        rf24.begin();
        Debug.Log(" END BEGIN)");
        Debug.Log("Is Chip Connected? " + rf24.isChipConnected());
        Debug.Log("(SetPALevel ");
        rf24.setPALevel(RF24.rf24_pa_dbm_e.RF24_PA_LOW, true);  // RF24_PA_MAX is default.
        Debug.Log(" End setPaLvel)");
        // save on transmission time by setting the radio to only transmit the
        // number of bytes we need to transmit a float
        Debug.Log("(SetPayload ");
        rf24.setPayloadSize(4);  // float datatype occupies 4 bytes
        Debug.Log(" END SETPAYLOAD)");
        //rf24.enableDynamicPayloads();
        //rf24.enableAckPayload();
        
        // set the TX address of the RX node into the TX pipe
        Debug.Log("(OpenWritingPipe");
        rf24.openWritingPipe(address[radioNumber ? 0 : 1]);  // always uses pipe 0
        Debug.Log(" End OpenWritingPipe)");
        
        // set the RX address of the TX node into a RX pipe
        Debug.Log("(OpenReadingPipe");
        rf24.openReadingPipe(1, BitConverter.ToUInt64(address[!radioNumber ? 0 : 1], 0));  // using pipe 1
        Debug.Log(" END OpenReadingPipe)");
        
        // additional setup specific to the node's role
        if (role) {
            Debug.Log("(StopListening ");
            rf24.stopListening();  // put radio in TX mode
            Debug.Log(" END StopListening)");
        } else {
            Debug.Log("(StartListening ");
            rf24.startListening();  // put radio in RX mode
            Debug.Log(" END StartListening)");
        }
        rfSetup = true;
        /*
         *     i f(com*mThread *!= null){
         *     commThread.Abort();
    }
    running = true;
    commThread = new Thread(commVoid);
    commThread.Start();
    Debug.Log("Started Comm Runner Thread");
    */
        Debug.Log("END SETUP");
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

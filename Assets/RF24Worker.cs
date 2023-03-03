using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEditor;

public class RF24Worker : MonoBehaviour
{
    
    private RF24 rf24;
    private USBManagement usbManagement;
    private bool isRP2040;
    private Transform transfom;
    public Transform DroneCenterTrans;
    protected readonly byte SOLENOID_UP = 0x01;
    protected readonly byte SOLENOID_DOWN = 0x02;
    protected readonly byte UPDATE = 0x04;
    private bool isSolenoidUp;
    private bool isSolenoidDown;
    private bool isBoost;   
    public bool TriggerUpOnCollision = true;
    public bool TriggerDownOnCollision = true;
    public bool TriggerBoostCollision = true;
    private ConcurrentQueue<int> triggerEvents = new ConcurrentQueue<int>();
    private Vector3 collisionImpulseVector;
    
    [Range(1, 100)]
    public int MaxThrottle = 10;
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
    byte[] recvPayload = new byte[8];
    byte[] sentPayload = new byte[7];
    
    // Start is called before the first frame update
    void Start()
    {
        usbManagement = gameObject.GetComponent<USBManagement>();
        transfom = gameObject.GetComponent<Transform>();
        rf24 = gameObject.GetComponent<RF24>();
        usbManagement.isDeviceConnectedCallback += new USBManagement.IsDeviceConnected(isConnectedToRP2040);
    }
    
    // Update is called once per frame
    void Update()
    {
        if(rfSetup){
            // This device is a RX node
            byte pipe = 0;
            if (rf24.available(ref pipe)) {              // is there a payload? get the pipe number that recieved it
                byte bytes = rf24.getDynamicPayloadSize(); // get the size of the payload
                rf24.read(ref recvPayload, bytes);             // fetch payload from FIFO
                String s = new String("");
                foreach(byte b in recvPayload){
                    s += String.Format("{0:X}", b);
                }
                Debug.Log("Received: " + s + " on Pipe " + pipe);
                UInt16 x = BitConverter.ToUInt16(recvPayload, 0);
                UInt16 y = BitConverter.ToUInt16(recvPayload, 2);
                UInt16 z = BitConverter.ToUInt16(recvPayload, 4);
                UInt16 w = BitConverter.ToUInt16(recvPayload, 6);
                float xf = Mathf.HalfToFloat(x);
                float yf = Mathf.HalfToFloat(y);
                float zf = Mathf.HalfToFloat(z);
                float wf = Mathf.HalfToFloat(w);
                Quaternion rotation = new Quaternion(xf,  -zf,  yf,  wf);
                // transfom.rotation = rotation;
                transfom.rotation = rotation * Quaternion.Euler(-90, 0, 0);
                // transfom.rotation = Quaternion.identity * Quaternion.AngleAxis(rotation.eulerAngles.z, Vector3.up) * Quaternion.AngleAxis(rotation.eulerAngles.x, -Vector3.forward) * Quaternion.AngleAxis(rotation.eulerAngles.y, Vector3.right);
                while(!triggerEvents.IsEmpty){
                    int value = 3;
                    if(triggerEvents.TryDequeue(out value)){
                        switch(value){
                            case 0:
                                isSolenoidDown = true;
                                break;
                            case 1:
                                isSolenoidUp = true;
                                break;
                            case 2:
                                isBoost = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
                collisionImpulseVector = collisionImpulseVector.normalized * MaxThrottle;
                Array.Copy(BitConverter.GetBytes((short)Mathf.Ceil(collisionImpulseVector.x)), 0, sentPayload, 0, 2);
                Array.Copy(BitConverter.GetBytes((short)Mathf.Ceil(collisionImpulseVector.y)), 0, sentPayload, 2, 2);
                Array.Copy(BitConverter.GetBytes((short)Mathf.Ceil(collisionImpulseVector.z)), 0, sentPayload, 4, 2);
                sentPayload[6] = (byte)((isSolenoidUp ? SOLENOID_UP : 0) | (isSolenoidDown ? SOLENOID_DOWN : 0) | (isBoost || isSolenoidDown || isSolenoidUp ? UPDATE : 0));
                Debug.Log("AckPayload" + String.Format("{0:X}", sentPayload[6]));
                if(rf24.writeAckPayload(1, sentPayload, 7)){
                    sentPayload[0] = 0;
                    sentPayload[1] = 0;
                    sentPayload[2] = 0;
                    sentPayload[3] = 0;
                    sentPayload[4] = 0;
                    sentPayload[5] = 0;
                    sentPayload[6] = 0;
                    isBoost = false;
                    isSolenoidDown = false;
                    isSolenoidUp = false;
                    collisionImpulseVector = Vector3.zero;
                }else{
                    Debug.LogError("Couldn't send AckPayload");
                }
            }
        }
    }
    
    void OnCollisionEnter(Collision collision){
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        // rb.Sleep();
        Vector3 droneCenter = DroneCenterTrans.position;
        Debug.DrawLine(droneCenter, droneCenter - collision.impulse, Color.red, 2.5f);
        // Gizmos.DrawRay(droneCenter, -collision.impulse);
        Debug.LogError("Collision: " + collision.impulse);
        collisionImpulseVector = -collision.impulse;
        if(TriggerDownOnCollision){
            triggerEvents.Enqueue(0);
        }
        if(TriggerUpOnCollision){
            triggerEvents.Enqueue(1);
        }
        if(TriggerBoostCollision){
            triggerEvents.Enqueue(2);
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
        rf24.enableDynamicPayloads();
        rf24.enableAckPayload();
        
        // set the TX address of the RX node into the TX pipe
        rf24.openWritingPipe(address[radioNumber ? 0 : 1]);  // always uses pipe 0
        
        // set the RX address of the TX node into a RX pipe
        rf24.openReadingPipe(1, BitConverter.ToUInt64(address[!radioNumber ? 0 : 1], 0));  // using pipe 1
        
        // additional setup specific to the node's role
        Debug.Log("WriteAckResult: " + rf24.writeAckPayload(1, sentPayload, 7));
        rf24.startListening();  // put radio in RX mode
        rfSetup = true;
        Debug.Log("END SETUP");
        // rf24.printDetails();
        // rf24.printPretty();
    }
    
    public void isConnectedToRP2040(bool isConnected){
        isRP2040 = isConnected;
        Debug.Log("Is NRF24Connected? " + isConnected);
        if(!isConnected){
            rfSetup = false;
        }
    }
    
    public void triggerSolenoidDown(){
        // isSolenoidDown = true;
        triggerEvents.Enqueue(0);
        Debug.Log("Triggered SolenoidDown");
    }
    
    public void triggerSolenoidUp(){
        // isSolenoidUp = true;
        triggerEvents.Enqueue(1);
        Debug.Log("Triggered SolenoidUp");
    }
    
    public void boost(){
        // isBoost = true;
        triggerEvents.Enqueue(2);
        Debug.Log("Boost");
    }
}

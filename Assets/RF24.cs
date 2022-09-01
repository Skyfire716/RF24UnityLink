using System;
using System.Text;
using System.Linq;
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
    
    protected readonly byte BYTEARRAYTRANSFER = 0x00;
    protected readonly byte BYTEARRAYTRANSFERSINLGE = 0x01;
    protected readonly byte SETCEPIN = 0x02;
    protected readonly byte SETCSNPIN = 0x03;
    
    private RF24Com rf24 = new RF24Com(0);
    
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
        if(rfSetup){
            if (role) {
                // This device is a TX node
                
                float start_timer = Time.time * 1000 * 1000;                // start the timer
                bool report = rf24.write(BitConverter.GetBytes(payload), 4);  // transmit & save the report
                float end_timer = Time.time * 1000 * 1000;                  // end the timer
                
                if (report) {
                    Debug.Log("Transmission successful! ");
                    Debug.Log("Transmission successful! ");  // payload was delivered
                    Debug.Log("Time to transmit = " + (end_timer - start_timer) + " us. Sent: " + payload);  // print payload sent
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
                }
            }  // role
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
        }
        outBuf[0] = reg;
        int bytesOut;
        int bytesIn;
        usbTransfer(outBuf, bufferLengths, out bytesOut, inBuf, bufferLengths, out bytesIn);
        Debug.Log("Wrote: " + bytesOut + " Bytes\tRead: " + bytesIn + " Bytes");
        byte[] dataOut = new byte[dataout.Length];
        for(int i = 0; i < dataout.Length; i++){
            dataout[i] = inBuf[i + 1];
        }
        return Tuple.Create(inBuf[0], dataout);
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
        Debug.Log("Wrote " + bytesOut + " bytes");
        Debug.Log("Read " + bytesIn + " bytes");
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
    
    
    byte[][] address = new byte[][]{new byte[]{0x00, 0x00, 0x00, 0x31, 0x4e, 0x6f, 0x64, 0x65}, new byte[]{0x00, 0x00, 0x00, 0x32, 0x4e, 0x6f, 0x64, 0x65}};
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
    
    public void RF24Begin(){
        rfSetup = true;
        rf24.SPIByteArrayTransfer += new RF24Com.SPITransferByteArraysCallbackHandler(transferByteArrays);
        rf24.SetCEPin += new RF24Com.SetPin(setCEPin);
        rf24.SetCSNPin += new RF24Com.SetPin(setCSNPin);
        rf24.begin();
        Debug.Log("Is Chip Connected? " + rf24.isChipConnected());
        rf24.setPALevel(0, true);  // RF24_PA_MAX is default.
        
        // save on transmission time by setting the radio to only transmit the
        // number of bytes we need to transmit a float
        rf24.setPayloadSize(4);  // float datatype occupies 4 bytes
        
        // set the TX address of the RX node into the TX pipe
        rf24.openWritingPipe(address[radioNumber ? 1 : 0]);  // always uses pipe 0
        
        // set the RX address of the TX node into a RX pipe
        rf24.openReadingPipe(1, BitConverter.ToUInt64(address[!radioNumber ? 1 : 0], 0));  // using pipe 1
        
        // additional setup specific to the node's role
        if (role) {
            rf24.stopListening();  // put radio in TX mode
        } else {
            rf24.startListening();  // put radio in RX mode
        }
    }
    //     
    public void toggleRole(){
        if(!role){
            role = true;
            Debug.Log("*** CHANGING TO TRANSMIT ROLE -- PRESS 'R' TO SWITCH BACK");
            rf24.stopListening();
        } else{
            role = false;
            Debug.Log("*** CHANGING TO RECEIVE ROLE -- PRESS 'T' TO SWITCH BACK");
            rf24.startListening();
        }
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    public class RF24Com{
        
        /*
         *  Copyright (c) 2007 Stefan Engelke <mbox@stefanengelke.de>
         *         P *ortions Copyright (C)* 201*1 Greg Copeland
         *         Permission is hereby granted, free of charge, to any person
         *         obtaining a copy of this software and associated documentation
         *         files (the "Software"), to deal in the Software without
         *         restriction, including without limitation the rights to use, copy,
         *         modify, merge, publish, distribute, sublicense, and/or sell copies
         *         of the Software, and to permit persons to whom the Software is
         *         furnished to do so, subject to the following conditions:
         *         The above copyright notice and this permission notice shall be
         *         included in all copies or substantial portions of the Software.
         *         THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
         *         EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
         *         MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
         *         NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
         *         HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
         *         WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
         *         OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
         *         DEALINGS IN THE SOFTWARE.
         */
        
        /* Memory Map */
        protected static byte NRF_CONFIG  =0x00;
        protected static byte EN_AA       =0x01;
        protected static byte EN_RXADDR   =0x02;
        protected static byte SETUP_AW    =0x03;
        protected static byte SETUP_RETR  =0x04;
        protected static byte RF_CH       =0x05;
        protected static byte RF_SETUP    =0x06;
        protected static byte NRF_STATUS  =0x07;
        protected static byte OBSERVE_TX  =0x08;
        protected static byte CD          =0x09;
        protected static byte RX_ADDR_P0  =0x0A;
        protected static byte RX_ADDR_P1  =0x0B;
        protected static byte RX_ADDR_P2  =0x0C;
        protected static byte RX_ADDR_P3  =0x0D;
        protected static byte RX_ADDR_P4  =0x0E;
        protected static byte RX_ADDR_P5  =0x0F;
        protected static byte TX_ADDR     =0x10;
        protected static byte RX_PW_P0    =0x11;
        protected static byte RX_PW_P1    =0x12;
        protected static byte RX_PW_P2    =0x13;
        protected static byte RX_PW_P3    =0x14;
        protected static byte RX_PW_P4    =0x15;
        protected static byte RX_PW_P5    =0x16;
        protected static byte FIFO_STATUS =0x17;
        protected static byte DYNPD       =0x1C;
        protected static byte FEATURE     =0x1D;
        
        /* Bit Mnemonics */
        protected static byte MASK_RX_DR  =6;
        protected static byte MASK_TX_DS  =5;
        protected static byte MASK_MAX_RT =4;
        protected static byte EN_CRC      =3;
        protected static byte CRCO        =2;
        protected static byte PWR_UP      =1;
        protected static byte PRIM_RX     =0;
        protected static byte ENAA_P5     =5;
        protected static byte ENAA_P3     =3;
        protected static byte ENAA_P2     =2;
        protected static byte ENAA_P1     =1;
        protected static byte ENAA_P0     =0;
        protected static byte ERX_P5      =5;
        protected static byte ERX_P4      =4;
        protected static byte ERX_P3      =3;
        protected static byte ERX_P2      =2;
        protected static byte ERX_P1      =1;
        protected static byte ERX_P0      =0;
        protected static byte AW          =0;
        protected static byte ARD         =4;
        protected static byte ARC         =0;
        protected static byte PLL_LOCK    =4;
        protected static byte CONT_WAVE   =7;
        protected static byte RF_DR       =3;
        protected static byte RF_PWR      =6;
        protected static byte RX_DR       =6;
        protected static byte TX_DS       =5;
        protected static byte MAX_RT      =4;
        protected static byte RX_P_NO     =1;
        protected static byte TX_FULL     =0;
        protected static byte PLOS_CNT    =4;
        protected static byte ARC_CNT     =0;
        protected static byte TX_REUSE    =6;
        protected static byte FIFO_FULL   =5;
        protected static byte TX_EMPTY    =4;
        protected static byte RX_FULL     =1;
        protected static byte RX_EMPTY    =0;
        protected static byte DPL_P5      =5;
        protected static byte DPL_P4      =4;
        protected static byte DPL_P3      =3;
        protected static byte DPL_P2      =2;
        protected static byte DPL_P1      =1;
        protected static byte DPL_P0      =0;
        protected static byte EN_DPL      =2;
        protected static byte EN_ACK_PAY  =1;
        protected static byte EN_DYN_ACK  =0;
        
        /* Instruction Mnemonics */
        protected static byte R_REGISTER    =0x00;
        protected static byte W_REGISTER    =0x20;
        protected static byte REGISTER_MASK =0x1F;
        protected static byte ACTIVATE      =0x50;
        protected static byte R_RX_PL_WID   =0x60;
        protected static byte R_RX_PAYLOAD  =0x61;
        protected static byte W_TX_PAYLOAD  =0xA0;
        protected static byte W_ACK_PAYLOAD =0xA8;
        protected static byte FLUSH_TX      =0xE1;
        protected static byte FLUSH_RX      =0xE2;
        protected static byte REUSE_TX_PL   =0xE3;
        protected static byte RF24_NOP      =0xFF;
        
        /* Non-P omissions */
        protected static byte LNA_HCURR =0;
        
        /* P model memory Map */
        protected static byte RPD                 =0x09;
        protected static byte W_TX_PAYLOAD_NO_ACK =0xB0;
        
        /* P model bit Mnemonics */
        protected static byte RF_DR_LOW   =5;
        protected static byte RF_DR_HIGH  =3;
        protected static byte RF_PWR_LOW  =1;
        protected static byte RF_PWR_HIGH =2;
        
        public enum rf24_pa_dbm_e{
            RF24_PA_MIN,
            RF24_PA_LOW,
            RF24_PA_HIGH,
            RF24_PA_MAX,
            RF24_PA_ERROR
        }
        
        enum rf24_datarate_e{
            RF24_1MBPS,
            RF24_2MBPS,
            RF24_250KBPS
        }
        
        enum rf24_crclength_e{
            RF24_CRC_DISABLED,
            RF24_CRC_8,
            RF24_CRC_16
        }
        
        protected byte[] child_pipe_enable = {ERX_P0, ERX_P1, ERX_P2, ERX_P3, ERX_P4, ERX_P5};
        protected byte[] child_pipe = {RX_ADDR_P0, RX_ADDR_P1, RX_ADDR_P2, RX_ADDR_P3, RX_ADDR_P4, RX_ADDR_P5};
        
        private byte status;
        private bool ack_payloads_enabled;
        private byte addr_width;
        private bool dynamic_payloads_enabled;
        private byte payload_size;
        private byte[] pipe0_reading_address = new byte[5];
        private byte config_reg;
        private bool _is_p_variant;
        private bool _is_p0_rx; 
        
        
        public delegate byte SPItransferCallbackHandler(byte inb);
        public event SPItransferCallbackHandler SPItransfer;
        
        public delegate void SetPin(byte level);
        public event SetPin SetCEPin;
        public event SetPin SetCSNPin;
        
        public delegate Tuple<byte, byte[]> SPITransferByteArraysCallbackHandler(byte reg, byte[] dataout);
        public event SPITransferByteArraysCallbackHandler SPIByteArrayTransfer;
        
        void read_register(byte reg, ref byte[] buf, byte len)
        {
            //beginTransaction();
            //status = _SPI.transfer(R_REGISTER | reg);
            //while (len--) {
            //    *buf++ = _SPI.transfer(0xFF);
            //}
            
            //status = SPItransfer((byte)(R_REGISTER | reg));
            //int i = 0;
            //while(len-- > 0){
            //    buf[i++] = SPItransfer(0xFF);
            //}
            
            
            Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(R_REGISTER | reg), Enumerable.Repeat((byte)0xFF, len).ToArray());
            status = t.Item1;
            buf = t.Item2;
            
            //endTransaction();
        }
        
        
        
        byte read_register(byte reg)
        {
            byte result;
            
            //beginTransaction();
            //status = _SPI.transfer(R_REGISTER | reg);
            //result = _SPI.transfer(0xff);
            //endTransaction();
            //status = SPItransfer((byte)(R_REGISTER | reg));
            //result = SPItransfer(0xFF);
            Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(R_REGISTER | reg), Enumerable.Repeat((byte)0xFF, 1).ToArray());
            status = t.Item1;
            return t.Item2[0];
        }
        
        
        
        void write_register(byte reg, byte[] buf, byte len)
        {
            //beginTransaction();
            //status = _SPI.transfer(W_REGISTER | reg);
            //while (len--) {
            //    _SPI.transfer(*buf++);
            //}
            //status = SPItransfer((byte)(W_REGISTER | reg));
            //int i = 0;
            //while (len-- > 0) {
            //    SPItransfer(buf[i++]);
            //}
            Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(W_REGISTER | reg), buf);
            status = t.Item1;
            //endTransaction();
        }
        
        
        
        void write_register(byte reg, byte value, bool is_cmd_only)
        {
            if (is_cmd_only) {
                //beginTransaction();
                //status = _SPI.transfer(W_REGISTER | reg);
                //endTransaction();
                //status = SPItransfer((byte)(W_REGISTER | reg));
                Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(W_REGISTER | reg), Array.Empty<byte>());
                status = t.Item1;
            }
            else {
                //beginTransaction();
                //status = _SPI.transfer(W_REGISTER | reg);
                //_SPI.transfer(value);
                //endTransaction();
                //status = SPItransfer((byte)(W_REGISTER | reg));
                //SPItransfer(value);
                
                Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(W_REGISTER | reg), new byte[]{value});
                status = t.Item1;
            }
        }
        
        
        
        void write_payload(byte[] buf, byte data_len, byte writeType)
        {
            //const byte* current = reinterpret_cast<const byte*>(buf);
            
            byte[] current = buf;
            
            byte blank_len = (data_len == 0) ? (byte)1 : (byte)0;
            if (!dynamic_payloads_enabled) {
                data_len = (byte)Mathf.Min(data_len, payload_size);
                blank_len = (byte)(payload_size - data_len);
            }
            else {
                data_len = (byte)Mathf.Min(data_len, 32);
            }
            
            //printf("[Writing %u bytes %u blanks]",data_len,blank_len);
            
            //beginTransaction();
            //status = _SPI.transfer(writeType);
            //while (data_len--) {
            //    _SPI.transfer(*current++);
            //}
            
            //while (blank_len--) {
            //    _SPI.transfer(0);
            //}
            //endTransaction();
            //status = SPItransfer(writeType);
            //int i = 0;
            //while (data_len-- > 0) {
            //    SPItransfer(current[i++]);
            //}
            
            //while (blank_len-- >0) {
            //    SPItransfer(0);
            //}
            byte[] outBuf = new byte[32];
            Array.Copy(current, outBuf, data_len);
            Array.Copy(Enumerable.Repeat((byte)0x00, blank_len).ToArray(), 0, outBuf, data_len, blank_len);
            Tuple<byte, byte[]> t = SPIByteArrayTransfer(writeType, outBuf);
            status = t.Item1;
        }
        
        
        
        void read_payload(ref byte[] buf, byte data_len)
        {
            //byte* current = reinterpret_cast<byte*>(buf);
            byte[] current = buf;
            
            byte blank_len = 0;
            if (!dynamic_payloads_enabled) {
                data_len = (byte)Mathf.Min(data_len, payload_size);
                blank_len = (byte)(payload_size - data_len);
            }
            else {
                data_len = (byte)Mathf.Min(data_len, 32);
            }
            
            //printf("[Reading %u bytes %u blanks]",data_len,blank_len);
            
            //beginTransaction();
            //status = _SPI.transfer(R_RX_PAYLOAD);
            //while (data_len--) {
            //    *current++ = _SPI.transfer(0xFF);
            //}
            
            //while (blank_len--) {
            //    _SPI.transfer(0xff);
            //}
            //endTransaction();
            
            /*
             *             s tatus = SPItransfe*r(R_RX*_PAYLOAD);
             *             int i = 0;
             *             while (data_len-- >0) {
             *                 current[i++] = SPItransfer(0xFF);
        }
        
        while (blank_len-- >0) {
            SPItransfer(0xFF);
        }
        */
            Tuple<byte, byte[]> t = SPIByteArrayTransfer(R_RX_PAYLOAD, Enumerable.Repeat((byte)0xFF, 32).ToArray());
            status = t.Item1;
            Array.Copy(t.Item2, current, data_len);
        }
        
        
        
        byte flush_rx()
        {
            write_register(FLUSH_RX, RF24_NOP, true);
            return status;
        }
        
        
        
        byte flush_tx()
        {
            write_register(FLUSH_TX, RF24_NOP, true);
            return status;
        }
        
        
        
        byte get_status()
        {
            write_register(RF24_NOP, RF24_NOP, true);
            return status;
        }
        
        public RF24Com(uint _spi_speed)
        {
            //: ce_pin(0xFFFF), csn_pin(0xFFFF), spi_speed(_spi_speed), payload_size(32), _is_p_variant(false), _is_p0_rx(false), addr_width(5), dynamic_payloads_enabled(true), csDelay(5)
            _init_obj();
        }
        
        public RF24Com(ushort _cepin, ushort _cspin, uint _spi_speed)
        {
            //: ce_pin(_cepin), csn_pin(_cspin), spi_speed(_spi_speed), payload_size(32), _is_p_variant(false), _is_p0_rx(false), addr_width(5), dynamic_payloads_enabled(true), csDelay(5)
            _init_obj();
        }
        
        void _init_obj()
        {
            // Use a pointer on the Arduino platform
            pipe0_reading_address[0] = 0;
        }
        
        
        
        void setChannel(byte channel)
        {
            const byte max_channel = 125;
            write_register(RF_CH, (byte)Mathf.Min(channel, max_channel), false);
        }
        
        byte getChannel()
        {
            return read_register(RF_CH);
        }
        
        
        
        public void setPayloadSize(byte size)
        {
            // payload size must be in range [1, 32]
            //payload_size = static_cast<byte>(Mathf.Max(1, Mathf.Min(32, size)));
            payload_size = (byte)Mathf.Max(1, Mathf.Min(32, size));
            
            // write static payload size setting for all pipes
            for (byte i = 0; i < 6; ++i) {
                //write_register(static_cast<byte>(RX_PW_P0 + i), payload_size);
                write_register((byte)(RX_PW_P0 + i), payload_size, false);
            }
        }
        
        
        
        public byte getPayloadSize()
        {
            return payload_size;
        }
        
        
        
        public bool begin()
        {
            return _init_pins() && _init_radio();
        }
        
        
        
        public bool begin(ushort _cepin, ushort _cspin)
        {
            //ce_pin = _cepin;
            //csn_pin = _cspin;
            return begin();
        }
        
        bool _init_pins()
        {
            if (!isValid()) {
                // didn't specify the CSN & CE pins to c'tor nor begin()
                return false;
            }
            
            
            
            // Initialize pins
            //if (ce_pin != csn_pin) {
            //pinMode(ce_pin, OUTPUT);
            //pinMode(csn_pin, OUTPUT);
            //}
            
            //ce(LOW);
            SetCEPin(0);
            //csn(HIGH);
            SetCSNPin(1);
            
            return true; // assuming pins are connected properly
        }
        
        
        
        bool _init_radio()
        {
            // Must allow the radio time to settle else configuration bits will not necessarily stick.
            // This is actually only required following power up but some settling time also appears to
            // be required after resets too. For full coverage, we'll always assume the worst.
            // Enabling 16b CRC is by far the most obvious case if the wrong timing is used - or skipped.
            // Technically we require 4.5ms + 14us as a worst case. We'll just call it 5ms for good measure.
            // WARNING: Delay is based on P-variant whereby non-P *may* require different timing.
            
            //delay(5);
            
            // Set 1500uS (minimum for 32B payload in ESB@250KBPS) timeouts, to make testing a little easier
            // WARNING: If this is ever lowered, either 250KBS mode with AA is broken or maximum packet
            // sizes must never be used. See datasheet for a more complete explanation.
            setRetries(5, 15);
            
            // Then set the data rate to the slowest (and most reliable) speed supported by all
            // hardware. Since this value occupies the same register as the PA level value, set
            // the PA level to MAX
            //setRadiation(rf24_pa_dbm_e.RF24_PA_MAX, rf24_datarate_e.RF24_1MBPS); // LNA enabled by default
            setRadiation(3, rf24_datarate_e.RF24_1MBPS); // LNA enabled by default
            
            // detect if is a plus variant & use old toggle features command accordingly
            byte before_toggle = read_register(FEATURE);
            toggle_features();
            byte after_toggle = read_register(FEATURE);
            _is_p_variant = before_toggle == after_toggle;
            if (after_toggle == 1) {
                if (_is_p_variant) {
                    // module did not experience power-on-reset (#401)
                    toggle_features();
                }
                // allow use of multicast parameter and dynamic payloads by default
                write_register(FEATURE, 0, false);
            }
            ack_payloads_enabled = false; // ack hardwarepayloads disabled by default
            write_register(DYNPD, 0, false);     // disable dynamic payloads by default (for all pipes)
            dynamic_payloads_enabled = false;
            write_register(EN_AA, 0x3F, false);  // enable auto-ack on all pipes
            write_register(EN_RXADDR, 3, false); // only open RX pipes 0 & 1
            setPayloadSize(32);           // set static payload size to 32 (max) bytes by default
            setAddressWidth(5);           // set default address length to (max) 5 bytes
            
            // Set up default configuration.  Callers can always change it later.
            // This channel should be universally safe and not bleed over into adjacent
            // spectrum.
            setChannel(76);
            
            // Reset current status
            // Notice reset and flush is the last thing we do
            write_register(NRF_STATUS, (byte)((1 << RX_DR) | (1 << TX_DS) | (1 << MAX_RT)), false);
            
            // Flush buffers
            flush_rx();
            flush_tx();
            
            // Clear CONFIG register:
            //      Reflect all IRQ events on IRQ pin
            //      Enable PTX
            //      Power Up
            //      16-bit CRC (CRC required by auto-ack)
            // Do not write CE high so radio will remain in standby I mode
            // PTX should use only 22uA of power
            write_register(NRF_CONFIG, (byte)((1 << EN_CRC) | (1 << CRCO)), false);
            config_reg = read_register(NRF_CONFIG);
            
            powerUp();
            
            // if config is not set correctly then there was a bad response from module
            return config_reg == ((1 << EN_CRC) | (1 << CRCO) | (1 << PWR_UP)) ? true : false;
        }
        
        
        
        public bool isChipConnected()
        {
            return read_register(SETUP_AW) == (addr_width - 2);
        }
        
        
        
        bool isValid()
        {
            return true;
        }
        
        
        
        public void startListening()
        {
            config_reg |= (byte)(1 << PRIM_RX);
            write_register(NRF_CONFIG, config_reg, false);
            write_register(NRF_STATUS, (byte)((1 << RX_DR) | (1 << TX_DS) | (1 << MAX_RT)), false);
            //ce(HIGH);
            SetCEPin(1);
            
            // Restore the pipe0 address, if exists
            if (_is_p0_rx) {
                write_register(RX_ADDR_P0, pipe0_reading_address, addr_width);
            }
            else {
                closeReadingPipe(0);
            }
        }
        
        public void stopListening()
        {
            //ce(LOW);
            SetCEPin(0);
            //delayMicroseconds(100);
            //delayMicroseconds(static_cast<int>(txDelay));
            float now = Time.time * 1000 * 1000;
            while(Time.time * 1000 * 1000 - now < 100){
                
            }
            if (ack_payloads_enabled) {
                flush_tx();
            }
            
            //config_reg = static_cast<byte>(config_reg & ~(1 << PRIM_RX));
            config_reg = (byte)(config_reg & ~(1 << PRIM_RX));
            write_register(NRF_CONFIG, config_reg, false);
            
            write_register(EN_RXADDR, (byte)(read_register(EN_RXADDR) | (1 << child_pipe_enable[0])), false); // Enable RX on pipe0
        }
        
        
        
        void powerDown()
        {
            //ce(LOW); // Guarantee CE is low on powerDown
            SetCEPin(0);
            config_reg = (byte)(config_reg & ~(1 << PWR_UP));
            write_register(NRF_CONFIG, config_reg, false);
        }
        
        
        
        //Power up now. Radio will not power down unless instructed by MCU for config changes etc.
        void powerUp()
        {
            // if not powered up then power up and wait for the radio to initialize
            if ((config_reg & (1 << PWR_UP)) == 0) {
                config_reg |= (byte)(1 << PWR_UP);
                write_register(NRF_CONFIG, config_reg, false);
                
                // For nRF24L01+ to go from power down mode to TX or RX mode it must first pass through stand-by mode.
                // There must be a delay of Tpd2stby (see Table 16.) after the nRF24L01+ leaves power down mode before
                // the CEis set high. - Tpd2stby can be up to 5ms per the 1.0 datasheet
                
                
                //delayMicroseconds(RF24_POWERUP_DELAY);
            }
        }
        
        //Similar to the previous write, clears the interrupt flags
        bool write(byte[] buf, byte len, bool multicast)
        {
            //Start Writing
            startFastWrite(buf, len, multicast, false);
            
            while ((get_status() & ((1 << TX_DS) | (1 << MAX_RT))) == 0) {
            }
            
            //ce(LOW);
            SetCEPin(0);
            write_register(NRF_STATUS, (byte)((1 << RX_DR) | (1 << TX_DS) | (1 << MAX_RT)), false);
            
            //Max retries exceeded
            if ((status & (1 << MAX_RT)) ==1) {
                flush_tx(); // Only going to be 1 packet in the FIFO at a time using this method, so just flush
                return false;
            }
            //TX OK 1 or 0
            return true;
        }
        
        public bool write(byte[] buf, byte len)
        {
            return write(buf, len, false);
        }
        
        
        
        //For general use, the interrupt flags are not important to clear
        bool writeBlocking(byte[] buf, byte len, uint timeout)
        {
            //Block until the FIFO is NOT full.
            //Keep track of the MAX retries and set auto-retry if seeing failures
            //This way the FIFO will fill up and allow blocking until packets go through
            //The radio will auto-clear everything in the FIFO as long as CE remains high
            
            //uint timer = millis(); // Get the time that the payload transmission started
            
            while (((get_status() & ((1 << TX_FULL)))) == 1) { // Blocking only if FIFO is full. This will loop and block until TX is successful or timeout
                
                if ((status & (1 << MAX_RT)) == 1) { // If MAX Retries have been reached
                    reUseTX();              // Set re-transmit and clear the MAX_RT interrupt flag
                    //if (millis() - timer > timeout) {
                    //    return false; // If this payload has exceeded the user-defined timeout, exit and return 0
                    //}
                }
            }
            
            //Start Writing
            startFastWrite(buf, len, false, false); // Write the payload if a buffer is clear
            
            return true; // Return 1 to indicate successful transmission
        }
        
        
        
        void reUseTX()
        {
            write_register(NRF_STATUS, (byte)(1 << MAX_RT), false); //Clear max retry flag
            write_register(REUSE_TX_PL, RF24_NOP, true);
            //ce(LOW); //Re-Transfer packet
            SetCEPin(0);
            //ce(HIGH);
            SetCEPin(1);
        }
        
        
        
        bool writeFast(byte[] buf, byte len, bool multicast)
        {
            //Block until the FIFO is NOT full.
            //Keep track of the MAX retries and set auto-retry if seeing failures
            //Return 0 so the user can control the retries and set a timer or failure counter if required
            //The radio will auto-clear everything in the FIFO as long as CE remains high
            
            //Blocking only if FIFO is full. This will loop and block until TX is successful or fail
            while (((get_status() & ((1 << TX_FULL)))) == 1) {
                if ((status & (1 << MAX_RT)) == 1) {
                    return false; //Return 0. The previous payload has not been retransmitted
                    // From the user perspective, if you get a 0, just keep trying to send the same payload
                }
            }
            startFastWrite(buf, len, multicast, false); // Start Writing
            
            return true;
        }
        
        bool writeFast(byte[] buf, byte len)
        {
            return writeFast(buf, len, false);
        }
        
        
        
        //Per the documentation, we want to set PTX Mode when not listening. Then all we do is write data and set CE high
        //In this mode, if we can keep the FIFO buffers loaded, packets will transmit immediately (no 130us delay)
        //Otherwise we enter Standby-II mode, which is still faster than standby mode
        //Also, we remove the need to keep writing the config register over and over and delaying for 150 us each time if sending a stream of data
        
        void startFastWrite(byte[] buf, byte len, bool multicast, bool startTx)
        { //TMRh20
            
            write_payload(buf, len, multicast ? W_TX_PAYLOAD_NO_ACK : W_TX_PAYLOAD);
            if (startTx) {
                //ce(HIGH);
                SetCEPin(1);
            }
        }
        
        
        
        //Added the original startWrite back in so users can still use interrupts, ack payloads, etc
        //Allows the library to pass all tests
        bool startWrite(byte[] buf, byte len, bool multicast)
        {
            
            // Send the payload
            write_payload(buf, len, multicast ? W_TX_PAYLOAD_NO_ACK : W_TX_PAYLOAD);
            //ce(HIGH);
            SetCEPin(1);
            //ce(LOW);
            SetCEPin(0);
            return (status & (1 << TX_FULL)) != 1;
        }
        
        
        
        bool rxFifoFull()
        {
            return (read_register(FIFO_STATUS) & (1 << RX_FULL)) == 1;
        }
        
        
        
        byte isFifo(bool about_tx)
        {
            return (byte)((read_register(FIFO_STATUS) >> (4 * (about_tx ? 1 : 0))) & 3);
        }
        
        
        
        bool isFifo(bool about_tx, bool check_empty)
        {
            return (isFifo(about_tx) & (1 << (!check_empty ? 1 : 0))) == 1;
        }
        
        
        
        bool txStandBy()
        {
            
            
            while (((read_register(FIFO_STATUS) & (1 << TX_EMPTY))) == 0) {
                if ((status & (1 << MAX_RT)) == 1) {
                    write_register(NRF_STATUS, (byte)(1 << MAX_RT), false);
                    //ce(LOW);
                    SetCEPin(0);
                    flush_tx(); //Non blocking, flush the data
                    return false;
                }
            }
            
            //ce(LOW); //Set STANDBY-I mode
            SetCEPin(0);
            return true;
        }
        
        
        
        bool txStandBy(uint timeout, bool startTx)
        {
            
            if (startTx) {
                stopListening();
                //ce(HIGH);
                SetCEPin(1);
            }
            //uint start = millis();
            float start = Time.time * 1000;
            while (((read_register(FIFO_STATUS) & (1 << TX_EMPTY))) == 0) {
                if ((status & (1 << MAX_RT)) == 1) {
                    write_register(NRF_STATUS, (byte)(1 << MAX_RT), false);
                    //ce(LOW); // Set re-transmit
                    SetCEPin(0);
                    //ce(HIGH);
                    SetCEPin(1);
                    if (Time.time * 1000 - start >= timeout) {
                        //ce(LOW);
                        SetCEPin(0);
                        flush_tx();
                        return false;
                    }
                }
            }
            
            //ce(LOW); //Set STANDBY-I mode
            SetCEPin(0);
            return true;
        }
        
        
        
        void maskIRQ(bool tx, bool fail, bool rx)
        {
            /* clear the interrupt flags */
            config_reg = (byte)(config_reg & ~(1 << MASK_MAX_RT | 1 << MASK_TX_DS | 1 << MASK_RX_DR));
            /* set the specified interrupt flags */
            config_reg = (byte)(config_reg | (fail ? 1 : 0) << MASK_MAX_RT | (tx ? 1 : 0) << MASK_TX_DS | (rx ? 1 : 0) << MASK_RX_DR);
            write_register(NRF_CONFIG, config_reg, false);
        }
        
        
        
        byte getDynamicPayloadSize()
        {
            byte result = read_register(R_RX_PL_WID);
            
            if (result > 32) {
                flush_rx();
                //delay(2);
                return 0;
            }
            return result;
        }
        
        
        
        public bool available()
        {
            byte pipe = 0;
            return available(ref pipe);
        }
        
        
        
        public bool available(ref byte pipe_num)
        {
            // get implied RX FIFO empty flag from status byte
            byte pipe = (byte)((get_status() >> RX_P_NO) & 0x07);
            if (pipe > 5)
                return false;
            
            // If the caller wants the pipe number, include that
            if (pipe_num != 0)
                pipe_num = pipe;
            
            return true;
        }
        
        
        
        public void read(ref byte[] buf, byte len)
        {
            
            // Fetch the payload
            read_payload(ref buf, len);
            
            //Clear the only applicable interrupt flags
            write_register(NRF_STATUS, (byte)(1 << RX_DR), false);
        }
        
        
        
        void whatHappened(bool tx_ok, bool tx_fail, bool rx_ready)
        {
            // Read the status & reset the status in one easy call
            // Or is that such a good idea?
            write_register(NRF_STATUS, (byte)((1 << RX_DR) | (1 << TX_DS) | (1 << MAX_RT)), false);
            
            // Report to the user what happened
            tx_ok = ((status & (1 << TX_DS)) == 1) ? true : false;
            tx_fail = ((status & (1 << MAX_RT)) == 1) ? true : false;
            rx_ready = ((status & (1 << RX_DR)) == 1) ? true : false;
        }
        
        
        
        void openWritingPipe(ulong value)
        {
            // Note that AVR 8-bit uC's store this LSB first, and the NRF24L01(+)
            // expects it LSB first too, so we're good.
            
            //write_register(RX_ADDR_P0, (value), addr_width);
            //write_register(TX_ADDR, (value), addr_width);
        }
        
        
        
        public void openWritingPipe(byte[] address)
        {
            // Note that AVR 8-bit uC's store this LSB first, and the NRF24L01(+)
            // expects it LSB first too, so we're good.
            write_register(RX_ADDR_P0, address, addr_width);
            write_register(TX_ADDR, address, addr_width);
        }
        
        public void openReadingPipe(byte child, ulong address)
        {
            // If this is pipe 0, cache the address.  This is needed because
            // openWritingPipe() will overwrite the pipe 0 address, so
            // startListening() will have to restore it.
            if (child == 0) {
                //memcpy(pipe0_reading_address, &address, addr_width);
                pipe0_reading_address[0] = (byte) address;
                pipe0_reading_address[1] = (byte) address;
                pipe0_reading_address[2] = (byte) address;
                pipe0_reading_address[3] = (byte) address;
                pipe0_reading_address[4] = (byte) address;
                _is_p0_rx = true;
            }
            
            if (child <= 5) {
                // For pipes 2-5, only write the LSB
                if (child < 2) {
                    write_register((child_pipe[child]), BitConverter.GetBytes(address), addr_width);
                }
                else {
                    write_register((child_pipe[child]), BitConverter.GetBytes(address), 1);
                }
                
                // Note it would be more efficient to set all of the bits for all open
                // pipes at once.  However, I thought it would make the calling code
                // more simple to do it this way.
                write_register(EN_RXADDR, (byte)((read_register(EN_RXADDR) | (1 << (child_pipe_enable[child])))), false);
            }
        }
        
        
        
        void setAddressWidth(byte a_width)
        {
            a_width = (byte)(a_width - 2);
            if (a_width == 1) {
                write_register(SETUP_AW, (byte)(a_width % 4), false);
                addr_width = (byte)((a_width % 4) + 2);
            }
            else {
                write_register(SETUP_AW, 0, false);
                addr_width = 2;
            }
        }
        
        
        
        void openReadingPipe(byte child, byte[] address)
        {
            // If this is pipe 0, cache the address.  This is needed because
            // openWritingPipe() will overwrite the pipe 0 address, so
            // startListening() will have to restore it.
            if (child == 0) {
                //memcpy(pipe0_reading_address, address, addr_width);
                _is_p0_rx = true;
            }
            if (child <= 5) {
                // For pipes 2-5, only write the LSB
                if (child < 2) {
                    write_register((child_pipe[child]), address, addr_width);
                }
                else {
                    write_register((child_pipe[child]), address, 1);
                }
                
                // Note it would be more efficient to set all of the bits for all open
                // pipes at once.  However, I thought it would make the calling code
                // more simple to do it this way.
                write_register(EN_RXADDR, (byte)((read_register(EN_RXADDR) | (1 << (child_pipe_enable[child])))), false);
            }
        }
        
        
        
        void closeReadingPipe(byte pipe)
        {
            write_register(EN_RXADDR, (byte)((read_register(EN_RXADDR) & ~(1 << (child_pipe_enable[pipe])))), false);
            if (pipe == 0) {
                // keep track of pipe 0's RX state to avoid null vs 0 in addr cache
                _is_p0_rx = false;
            }
        }
        
        
        
        void toggle_features()
        {
            //beginTransaction();
            //status = _SPI.transfer(ACTIVATE);
            //_SPI.transfer(0x73);
            //endTransaction();
            //status = SPItransfer(ACTIVATE);
            //SPItransfer(0x73);
            
            Tuple<byte, byte[]> t = SPIByteArrayTransfer(ACTIVATE, new byte[]{0x73});
            status = t.Item1;
        }
        
        
        
        void enableDynamicPayloads()
        {
            // Enable dynamic payload throughout the system
            
            //toggle_features();
            write_register(FEATURE, (byte)(read_register(FEATURE) | (1 << EN_DPL)), false);
            
            
            // Enable dynamic payload on all pipes
            //
            // Not sure the use case of only having dynamic payload on certain
            // pipes, so the library does not support it.
            write_register(DYNPD, (byte)(read_register(DYNPD) | (1 << DPL_P5) | (1 << DPL_P4) | (1 << DPL_P3) | (1 << DPL_P2) | (1 << DPL_P1) | (1 << DPL_P0)), false);
            
            dynamic_payloads_enabled = true;
        }
        
        
        
        void disableDynamicPayloads()
        {
            // Disables dynamic payload throughout the system.  Also disables Ack Payloads
            
            //toggle_features();
            write_register(FEATURE, 0, false);
            
            // Disable dynamic payload on all pipes
            //
            // Not sure the use case of only having dynamic payload on certain
            // pipes, so the library does not support it.
            write_register(DYNPD, 0, false);
            
            dynamic_payloads_enabled = false;
            ack_payloads_enabled = false;
        }
        
        
        
        void enableAckPayload()
        {
            // enable ack payloads and dynamic payload features
            
            if (!ack_payloads_enabled) {
                write_register(FEATURE, (byte)(read_register(FEATURE) | (1 << EN_ACK_PAY) | (1 << EN_DPL)), false);
                
                // Enable dynamic payload on pipes 0 & 1
                write_register(DYNPD, (byte)(read_register(DYNPD) | (1 << DPL_P1) | (1 << DPL_P0)), false);
                dynamic_payloads_enabled = true;
                ack_payloads_enabled = true;
            }
        }
        
        
        
        void disableAckPayload()
        {
            // disable ack payloads (leave dynamic payload features as is)
            if (ack_payloads_enabled) {
                write_register(FEATURE, (byte)((read_register(FEATURE) & ~(1 << EN_ACK_PAY))), false);
                
                ack_payloads_enabled = false;
            }
        }
        
        
        
        void enableDynamicAck()
        {
            //
            // enable dynamic ack features
            //
            //toggle_features();
            write_register(FEATURE, (byte)(read_register(FEATURE) | (1 << EN_DYN_ACK)), false);
            
        }
        
        
        
        bool writeAckPayload(byte pipe, byte[] buf, byte len)
        {
            if (ack_payloads_enabled) {
                byte[] current = buf;
                
                write_payload(current, len, (byte)(W_ACK_PAYLOAD | (pipe & 0x07)));
                return (status & (1 << TX_FULL)) == 1;
            }
            return false;
        }
        
        
        
        bool isAckPayloadAvailable()
        {
            byte pipe = 0;
            return available(ref pipe);
        }
        
        
        
        bool isPVariant()
        {
            return _is_p_variant;
        }
        
        
        
        void setAutoAck(bool enable)
        {
            if (enable) {
                write_register(EN_AA, 0x3F, false);
            }
            else {
                write_register(EN_AA, 0, false);
                // accommodate ACK payloads feature
                if (ack_payloads_enabled) {
                    disableAckPayload();
                }
            }
        }
        
        
        
        void setAutoAck(byte pipe, bool enable)
        {
            if (pipe < 6) {
                byte en_aa = read_register(EN_AA);
                if (enable) {
                    en_aa |= (byte)((1 << pipe));
                }
                else {
                    en_aa = (byte)(en_aa & ~(1 << pipe));
                    if (ack_payloads_enabled && pipe == 0) {
                        disableAckPayload();
                    }
                }
                write_register(EN_AA, en_aa, false);
            }
        }
        
        
        
        bool testCarrier()
        {
            return (read_register(CD) & 1) == 1;
        }
        
        
        
        bool testRPD()
        {
            return (read_register(RPD) & 1) == 1;
        }
        
        
        
        public void setPALevel(byte level, bool lnaEnable)
        {
            byte setup = (byte)(read_register(RF_SETUP) & (0xF8));
            setup |= (byte)_pa_level_reg_value(level, lnaEnable);
            write_register(RF_SETUP, setup, false);
        }
        
        
        
        byte getPALevel()
        {
            return (byte)((read_register(RF_SETUP) & ((1 << RF_PWR_LOW) | (1 << RF_PWR_HIGH))) >> 1);
        }
        
        
        
        byte getARC()
        {
            return (byte)(read_register(OBSERVE_TX) & 0x0F);
        }
        
        
        
        bool setDataRate(rf24_datarate_e speed)
        {
            bool result = false;
            byte setup = read_register(RF_SETUP);
            
            // HIGH and LOW '00' is 1Mbs - our default
            setup = (byte)(setup & ~((1 << RF_DR_LOW) | (1 << RF_DR_HIGH)));
            setup |= (byte)_data_rate_reg_value(speed);
            
            write_register(RF_SETUP, setup, false);
            
            // Verify our result
            if (read_register(RF_SETUP) == setup) {
                result = true;
            }
            return result;
        }
        
        
        
        rf24_datarate_e getDataRate()
        {
            rf24_datarate_e result;
            byte dr = (byte)(read_register(RF_SETUP) & ((1 << RF_DR_LOW) | (1 << RF_DR_HIGH)));
            
            // switch uses RAM (evil!)
            // Order matters in our case below
            if (dr == (1 << RF_DR_LOW)) {
                // '10' = 250KBPS
                result = rf24_datarate_e.RF24_250KBPS;
            }
            else if (dr == (1 << RF_DR_HIGH)) {
                // '01' = 2MBPS
                result = rf24_datarate_e.RF24_2MBPS;
            }
            else {
                // '00' = 1MBPS
                result = rf24_datarate_e.RF24_1MBPS;
            }
            return result;
        }
        
        
        
        void setCRCLength(rf24_crclength_e length)
        {
            config_reg = (byte)((config_reg & ~((1 << CRCO) | (1 << EN_CRC))));
            
            // switch uses RAM (evil!)
            if (length == rf24_crclength_e.RF24_CRC_DISABLED) {
                // Do nothing, we turned it off above.
            }
            else if (length == rf24_crclength_e.RF24_CRC_8) {
                config_reg |= (byte)(1 << EN_CRC);
            }
            else {
                config_reg |= (byte)(1 << EN_CRC);
                config_reg |= (byte)(1 << CRCO);
            }
            write_register(NRF_CONFIG, config_reg, false);
        }
        
        
        
        rf24_crclength_e getCRCLength()
        {
            rf24_crclength_e result = rf24_crclength_e.RF24_CRC_DISABLED;
            byte AA = read_register(EN_AA);
            config_reg = read_register(NRF_CONFIG);
            
            if ((config_reg & (1 << EN_CRC)) == 1 || AA == 1) {
                if ((config_reg & (1 << CRCO)) == 1) {
                    result = rf24_crclength_e.RF24_CRC_16;
                }
                else {
                    result = rf24_crclength_e.RF24_CRC_8;
                }
            }
            return result;
        }
        
        
        
        void disableCRC()
        {
            config_reg = (byte)(config_reg & ~(1 << EN_CRC));
            write_register(NRF_CONFIG, config_reg, false);
        }
        
        
        void setRetries(byte delay, byte count)
        {
            write_register(SETUP_RETR, (byte)(Mathf.Min(15, delay) << ARD | Mathf.Min(15, count)), false);
        }
        
        
        void startConstCarrier(rf24_pa_dbm_e level, byte channel)
        {
            stopListening();
            write_register(RF_SETUP, (byte)(read_register(RF_SETUP) | (1 << CONT_WAVE) | (1 << PLL_LOCK)), false);
            if (isPVariant()) {
                setAutoAck(false);
                setRetries(0, 0);
                byte[] dummy_buf = new byte[32];
                for (byte i = 0; i < 32; ++i)
                    dummy_buf[i] = 0xFF;
                
                // use write_register() instead of openWritingPipe() to bypass
                // truncation of the address with the current addr_width value
                write_register(TX_ADDR, (dummy_buf), 5);
                flush_tx(); // so we can write to top level
                
                // use write_register() instead of write_payload() to bypass
                // truncation of the payload with the current payload_size value
                write_register(W_TX_PAYLOAD, (dummy_buf), 32);
                
                disableCRC();
            }
            //setPALevel(level, true);
            switch(level){
                case rf24_pa_dbm_e.RF24_PA_MIN:
                    setPALevel(0, true);
                    break;
                case rf24_pa_dbm_e.RF24_PA_LOW:
                    setPALevel(1, true);
                    break;
                case rf24_pa_dbm_e.RF24_PA_HIGH:
                    setPALevel(2, true);
                    break;
                case rf24_pa_dbm_e.RF24_PA_MAX:
                    setPALevel(3, true);
                    break;
                case rf24_pa_dbm_e.RF24_PA_ERROR:
                    setPALevel(4, true);
                    break;
            }
            setChannel(channel);
            //ce(HIGH);
            SetCEPin(1);
            if (isPVariant()) {
                //delay(1); // datasheet says 1 ms is ok in this instance
                //ce(LOW);
                SetCEPin(0);
                reUseTX();
            }
        }
        
        
        
        void stopConstCarrier()
        {
            /*
             * A note from the datasheet:
             * Do not use REUSE_TX_PL together with CONT_WAVE=1. When both these
             * registers are set the chip does not react when setting CE low. If
             * however, both registers are set PWR_UP = 0 will turn TX mode off.
             */
            powerDown(); // per datasheet recommendation (just to be safe)
            write_register(RF_SETUP, (byte)((read_register(RF_SETUP) & ~(1 << CONT_WAVE) & ~(1 << PLL_LOCK))), false);
            //ce(LOW);
            SetCEPin(0);
        }
        
        
        
        void toggleAllPipes(bool isEnabled)
        {
            write_register(EN_RXADDR, (byte)(isEnabled ? 0x3F : 0), false);
        }
        
        
        
        byte _data_rate_reg_value(rf24_datarate_e speed)
        {
            // HIGH and LOW '00' is 1Mbs - our default
            return 0;
        }
        
        
        
        byte _pa_level_reg_value(byte level, bool lnaEnable)
        {
            // If invalid level, go to max PA
            // Else set level as requested
            // + lnaEnable (1 or 0) to support the SI24R1 chip extra bit
            return (byte)(((level > 3 ? (3) : level) << 1) + (lnaEnable ? 1 : 0));
        }
        
        
        
        void setRadiation(byte level, rf24_datarate_e speed)
        {
            byte setup = _data_rate_reg_value(speed);
            setup |= _pa_level_reg_value(level, true);
            write_register(RF_SETUP, setup, false);
        }
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
}


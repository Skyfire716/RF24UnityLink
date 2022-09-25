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
        protected static byte ENAA_P4     =4;
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
        
        protected static String rf24_model_e_str_0 = "nRF24L01";
        protected static String rf24_model_e_str_1 = "nRF24L01+";
        protected static String[] rf24_model_e_str_P = new String[]{
            rf24_model_e_str_0,
            rf24_model_e_str_1,
        };
        
        
        protected static String rf24_datarate_e_str_0 = "= 1 MBPS";
        protected static String rf24_datarate_e_str_1 = "= 2 MBPS";
        protected static String rf24_datarate_e_str_2 = "= 250 KBPS";
        protected static String[] rf24_datarate_e_str_P = new String[]{
            rf24_datarate_e_str_0,
            rf24_datarate_e_str_1,
            rf24_datarate_e_str_2,
        };
        
        protected static String rf24_crclength_e_str_0 = "= Disabled";
        protected static String rf24_crclength_e_str_1 = "= 8 bits";
        protected static String rf24_crclength_e_str_2 = "= 16 bits";
        protected static String[] rf24_crclength_e_str_P = new String[]{
            rf24_crclength_e_str_0,
            rf24_crclength_e_str_1,
            rf24_crclength_e_str_2,
        };
        
        protected static String rf24_pa_dbm_e_str_0 = "= PA_MIN";
        protected static String rf24_pa_dbm_e_str_1 = "= PA_LOW";
        protected static String rf24_pa_dbm_e_str_2 = "= PA_HIGH";
        protected static String rf24_pa_dbm_e_str_3 = "= PA_MAX";
        protected static String[] rf24_pa_dbm_e_str_P = new String[]{
            rf24_pa_dbm_e_str_0,
            rf24_pa_dbm_e_str_1,
            rf24_pa_dbm_e_str_2,
            rf24_pa_dbm_e_str_3,
        };
        
        protected static String rf24_feature_e_str_on = "= Enabled";
        protected static String rf24_feature_e_str_allowed = "= Allowed";
        protected static String rf24_feature_e_str_open = " open ";
        protected static String rf24_feature_e_str_closed = "closed";
        protected static String[] rf24_feature_e_str_P = new String[]{
            rf24_crclength_e_str_0,
            rf24_feature_e_str_on,
            rf24_feature_e_str_allowed,
            rf24_feature_e_str_closed,
            rf24_feature_e_str_open,
        };
        
        public enum rf24_pa_dbm_e{
            RF24_PA_MIN,
            RF24_PA_LOW,
            RF24_PA_HIGH,
            RF24_PA_MAX,
            RF24_PA_ERROR
        }
        
        public enum rf24_datarate_e{
            RF24_1MBPS,
            RF24_2MBPS,
            RF24_250KBPS
        }
        
        public enum rf24_crclength_e{
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
        private int csDelay;
        
        
        public delegate byte SPItransferCallbackHandler(byte inb);
        public event SPItransferCallbackHandler SPItransfer;
        
        public delegate void SetPin(byte level);
        public event SetPin SetCEPin;
        public event SetPin SetCSNPin;
        
        public delegate Tuple<byte, byte[]> SPITransferByteArraysCallbackHandler(byte reg, byte[] dataout);
        public event SPITransferByteArraysCallbackHandler SPIByteArrayTransfer;
    
        void read_register(byte reg, ref byte[] buf, byte len)
        {
            if(reg >= 0x18 && reg <= 0x1B){
                Debug.LogError("You are not allowed to read or Write to the Registers between 0x18 and 0x1B READ THE MANUAL!!!");
                return;
            }
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
            if(reg >= 0x18 && reg <= 0x1B){
                Debug.LogError("You are not allowed to read or Write to the Registers between 0x18 and 0x1B READ THE MANUAL!!!");
                return 0;
            }
            byte result;
            //beginTransaction();
            //status = _SPI.transfer(R_REGISTER | reg);
            //result = _SPI.transfer(0xff);
            
            //status = SPItransfer((byte)(R_REGISTER | reg));
            //result = SPItransfer(0xFF);
            Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(R_REGISTER | reg), Enumerable.Repeat((byte)0xFF, 1).ToArray());
            status = t.Item1;
            //endTransaction();
            return t.Item2[0];
        }
        
        
        void write_register(byte reg, byte[] buf, byte len)
        {
            if(reg >= 0x18 && reg <= 0x1B){
                Debug.LogError("You are not allowed to read or Write to the Registers between 0x18 and 0x1B READ THE MANUAL!!!");
                return;
            }
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
            if(reg >= 0x18 && reg <= 0x1B){
                Debug.LogError("You are not allowed to read or Write to the Registers between 0x18 and 0x1B READ THE MANUAL!!!");
                return;
            }
            if (is_cmd_only) {
                //beginTransaction();
                //status = _SPI.transfer(W_REGISTER | reg);
                ////endTransaction();
                //status = SPItransfer((byte)(W_REGISTER | reg));
                Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(W_REGISTER | reg), Array.Empty<byte>());
                //endTransaction();
                status = t.Item1;
            }
            else {
                //beginTransaction();
                //status = _SPI.transfer(W_REGISTER | reg);
                //_SPI.transfer(value);
                ////endTransaction();
                //status = SPItransfer((byte)(W_REGISTER | reg));
                //SPItransfer(value);
                
                Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(W_REGISTER | reg), new byte[]{value});
                //endTransaction();
                //Tuple<byte, byte[]> t = SPIByteArrayTransfer((byte)(reg), new byte[]{value});
                status = t.Item1;
            }
        }
        
        
        
        void write_payload(byte[] buf, byte data_len, byte writeType)
        {
            //const byte* current = reinterpret_cast<const byte*>(buf);
            
            byte[] current = buf;
            
            byte blank_len = ((~data_len) != 0) ? (byte)1 : (byte)0;
            if (!dynamic_payloads_enabled) {
                data_len = (byte)Mathf.Min(data_len, payload_size);
                blank_len = (byte)(payload_size - data_len);
            }else {
                data_len = (byte)Mathf.Min(data_len, 32);
            }
            int size = data_len + blank_len;
            //printf("[Writing %u bytes %u blanks]",data_len,blank_len);
            
            //beginTransaction();
            //status = _SPI.transfer(writeType);
            //while (data_len--) {
            //    _SPI.transfer(*current++);
            //}
            
            //while (blank_len--) {
            //    _SPI.transfer(0);
            //}
            ////endTransaction();
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
            byte[] dataOutBuf = new byte[size];
            Array.Copy(outBuf, 0, dataOutBuf, 0, size);
            Tuple<byte, byte[]> t = SPIByteArrayTransfer(writeType, dataOutBuf);
            //Status 0xF estimated
            //endTransaction();
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
            ////endTransaction();
            
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
            //endTransaction();
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
        
        public void setRF24(uint _spi_speed)
        {
            //: ce_pin(0xFFFF), csn_pin(0xFFFF), spi_speed(_spi_speed), payload_size(32), _is_p_variant(false), _is_p0_rx(false), addr_width(5), dynamic_payloads_enabled(true), csDelay(5)
            payload_size = 32;
            _is_p_variant =false;
            _is_p0_rx = false;
            addr_width = 5;
            dynamic_payloads_enabled = true;
            csDelay = 5;
            _init_obj();
        }
        
        public void setRF24(ushort _cepin, ushort _cspin, uint _spi_speed)
        {
            //: ce_pin(_cepin), csn_pin(_cspin), spi_speed(_spi_speed), payload_size(32), _is_p_variant(false), _is_p0_rx(false), addr_width(5), dynamic_payloads_enabled(true), csDelay(5)
            payload_size = 32;
            _is_p_variant =false;
            _is_p0_rx = false;
            addr_width = 5;
            dynamic_payloads_enabled = true;
            csDelay = 5;
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
            //payload_size = (byte)(Mathf.Max(1, Mathf.Min(32, size)));
            payload_size = (byte)Mathf.Max(1, Mathf.Min(32, size));
            
            // write static payload size setting for all pipes
            for (byte i = 0; i < 6; ++i) {
                //write_register((byte)(RX_PW_P0 + i), payload_size);
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
            if(Application.isEditor){
                float now = Time.realtimeSinceStartup * 1000;
                while(Time.realtimeSinceStartup * 1000 - now < csDelay){
                    
                }
            }else if(Application.isPlaying){
                float now = Time.time * 1000;
                while(Time.time * 1000 - now < csDelay){
                    
                }
            }
            // Set 1500uS (minimum for 32B payload in ESB@250KBPS) timeouts, to make testing a little easier
            // WARNING: If this is ever lowered, either 250KBS mode with AA is broken or maximum packet
            // sizes must never be used. See datasheet for a more complete explanation.
            setRetries(5, 15);
            
            // Then set the data rate to the slowest (and most reliable) speed supported by all
            // hardware. Since this value occupies the same register as the PA level value, set
            // the PA level to MAX
            setDataRate(rf24_datarate_e.RF24_1MBPS);
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
            }else {
                closeReadingPipe(0);
            }
        }
        
        public void stopListening()
        {
            //ce(LOW);
            SetCEPin(0);
            //delayMicroseconds(100);
            //delayMicroseconds(static_cast<int>(txDelay));
            DateTime start = DateTime.Now;
            while((new TimeSpan(DateTime.Now.Ticks - start.Ticks)).TotalMilliseconds * 1000 < 100){
                Debug.Log("" + (new TimeSpan(DateTime.Now.Ticks - start.Ticks).TotalMilliseconds * 1000));
            }
            /*
             *            i f(Ap*plication.isEditor*){
             *            float now = Time.realtimeSinceStartup * 1000 * 1000;
             *            Debug.Log("Start Waiting");
             *            while(Time.time * 1000 * 1000 - now < 100){
             *                
        }
        }else if(Application.isPlaying){
            float now = Time.time * 1000 * 1000;
            Debug.Log("Start Waiting");
            while(Time.time * 1000 * 1000 - now < 100){
                
        }
        }
        */  
            if (ack_payloads_enabled) {
                flush_tx();
            }
            
            //config_reg = (byte)(config_reg & ~(1 << PRIM_RX));
            config_reg = (byte)(config_reg & (~(1 << PRIM_RX)));
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
            startFastWrite(buf, len, multicast, true);
            DateTime start = DateTime.Now;
            
            while ((get_status() & ((1 << TX_DS) | (1 << MAX_RT))) == 0) {
                if(new TimeSpan(DateTime.Now.Ticks - start.Ticks).TotalMilliseconds > 95){
                     Debug.Log("Aborting");                    
                     return false;
                }
            }
            //ce(LOW);
            SetCEPin(0);
            write_register(NRF_STATUS, (byte)((1 << RX_DR) | (1 << TX_DS) | (1 << MAX_RT)), false);
            //Max retries exceeded
            if ((status & (1 << MAX_RT)) == (1 << MAX_RT)) {
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
            startFastWrite(buf, len, false, true); // Write the payload if a buffer is clear
            
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
            startFastWrite(buf, len, multicast, true); // Start Writing
            
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
            //Status 0xE + startTX expected
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
        
        
        
        public byte getDynamicPayloadSize()
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
            if (pipe > 5){
                return false;
            }
            // If the caller wants the pipe number, include that
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
        
        public void openWritingPipe(byte[] address)
        {
            // Debug.Log("openWritingPipe");
            // Note that AVR 8-bit uC's store this LSB first, and the NRF24L01(+)
            // expects it LSB first too, so we're good.
            byte[] addrCorrected = new byte[addr_width];
            if(address.Length != addr_width){
                Array.Copy(address, address.Length - addr_width, addrCorrected, 0, addr_width);
            }else{
                Array.Copy(address, addrCorrected, addr_width);
            }
            byte[] txAddr = new byte[addr_width];
            Array.Copy(addrCorrected, txAddr, addr_width);
            write_register(RX_ADDR_P0, addrCorrected, addr_width);
            write_register(TX_ADDR, txAddr, addr_width);
        }
        
        public void openReadingPipe(byte child, ulong address)
        {
            // If this is pipe 0, cache the address.  This is needed because
            // openWritingPipe() will overwrite the pipe 0 address, so
            // startListening() will have to restore it.
            if (child == 0) {
                //memcpy(pipe0_reading_address, &address, addr_width);
                Array.Copy(BitConverter.GetBytes(address), BitConverter.GetBytes(address).Length - addr_width, pipe0_reading_address, 0, addr_width);
                //pipe0_reading_address[0] = (byte) address;
                //pipe0_reading_address[1] = (byte) address;
                //pipe0_reading_address[2] = (byte) address;
                //pipe0_reading_address[3] = (byte) address;
                //pipe0_reading_address[4] = (byte) address;
                _is_p0_rx = true;
            }
            
            byte[] addrCorrected = new byte[addr_width];
            if(BitConverter.GetBytes(address).Length != addr_width){
                Array.Copy(BitConverter.GetBytes(address), BitConverter.GetBytes(address).Length - addr_width, addrCorrected, 0, addr_width);
            }else{
                Array.Copy(BitConverter.GetBytes(address), addrCorrected, addr_width);
            }
            if (child <= 5) {
                // For pipes 2-5, only write the LSB
                if (child < 2) {
                    write_register((child_pipe[child]), addrCorrected, addr_width);
                }else {
                    write_register((child_pipe[child]), addrCorrected, 1);
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
            if ((a_width & 0x01) == 1) {
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
                }else {
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
        
        
        
        public void enableDynamicPayloads()
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
        
        
        
        public void disableDynamicPayloads()
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
        
        
        
        public void enableAckPayload()
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
        
        
        
        public void disableAckPayload()
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
        
        
        
        public bool writeAckPayload(byte pipe, byte[] buf, byte len)
        {
            if (ack_payloads_enabled) {
                byte[] current = buf;
                
                write_payload(current, len, (byte)(W_ACK_PAYLOAD | (pipe & 0x07)));
                return (status & (1 << TX_FULL)) == 0;
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
        
        
        public void setPALevel(rf24_pa_dbm_e level, bool lnaEnable){
            switch(level){
                case rf24_pa_dbm_e.RF24_PA_MIN:
                    setPALevell(0, lnaEnable);
                    break;
                case rf24_pa_dbm_e.RF24_PA_LOW:
                    setPALevell(1, lnaEnable);
                    break;
                case rf24_pa_dbm_e.RF24_PA_HIGH:
                    setPALevell(2, lnaEnable);
                    break;
                case rf24_pa_dbm_e.RF24_PA_MAX:
                    setPALevell(3, lnaEnable);
                    break;
                case rf24_pa_dbm_e.RF24_PA_ERROR:
                    setPALevell(4, lnaEnable);
                    break;
            }
        }
        
        private void setPALevell(byte level, bool lnaEnable)
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
            
            if ((config_reg & (1 << EN_CRC)) != 0 || AA != 0) {
                if ((config_reg & (1 << CRCO)) != 0) {
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
            setPALevel(level, true);
            /*
             *           switch(level){
             *               case rf24_pa_dbm_e.RF24_PA_MIN:
             *                   setPALevel(0, true);
             *                   break;
             *               case rf24_pa_dbm_e.RF24_PA_LOW:
             *                   setPALevel(1, true);
             *                   break;
             *               case rf24_pa_dbm_e.RF24_PA_HIGH:
             *                   setPALevel(2, true);
             *                   break;
             *               case rf24_pa_dbm_e.RF24_PA_MAX:
             *                   setPALevel(3, true);
             *                   break;
             *               case rf24_pa_dbm_e.RF24_PA_ERROR:
             *                   setPALevel(4, true);
             *                   break;
        }
        */
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
            switch(speed){
                case rf24_datarate_e.RF24_1MBPS:
                    return 0;
                case rf24_datarate_e.RF24_2MBPS:
                    return 1;
                case rf24_datarate_e.RF24_250KBPS:
                    return 2;
            }
            return 0;
        }
        
        
        
        byte _pa_level_reg_value(byte level, bool lnaEnable)
        {
            // If invalid level, go to max PA
            // Else set level as requested
            // + lnaEnable (1 or 0) to support the SI24R1 chip extra bit
            return (byte)(((level > 3 ? (3) : level) << 1) + (lnaEnable ? 1 : 0));
        }
        
        
        
        void setRadiation(rf24_pa_dbm_e level, rf24_datarate_e speed)
        {
            byte setup = _data_rate_reg_value(speed);
            setup |= _pa_level_reg_value(getPALevel(level), true);
            write_register(RF_SETUP, setup, false);
        }
        
        String print_byte_register(String name, byte reg, byte qty)
        {
            String s = new String(name + "\t=");
            while (qty-- > 0) {
                s += " 0x" + String.Format("{0:X2}", read_register(reg++));
            }
            return s;
        }
        
        
        String print_address_register(String name, byte reg, byte qty)
        {
            String s = new String(name + "\t=");
            while (qty-- > 0) {
                byte[] buffer = new byte[addr_width];
                read_register((byte)(reg++ & REGISTER_MASK), ref buffer, addr_width);
                byte bufptr = addr_width;
                for(int i = addr_width - 1; i >= 0; i--){
                    s += " 0x";
                    s += String.Format("{0:X2}", buffer[i]);
                }
            }
            return s;
        }
        
        
        private int getDataRate(bool f){
            rf24_datarate_e rate = getDataRate();
            switch(rate){
                case rf24_datarate_e.RF24_1MBPS:
                    return 0;
                case rf24_datarate_e.RF24_2MBPS:
                    return 1;
                case rf24_datarate_e.RF24_250KBPS:
                    return 2;
            }
            return 0;
        }
        
        private int getCRCLength(bool f){
            rf24_crclength_e crc = getCRCLength();
            switch(crc){
                case rf24_crclength_e.RF24_CRC_DISABLED:
                    return 0;
                case rf24_crclength_e.RF24_CRC_8:
                    return 1;
                case   rf24_crclength_e.RF24_CRC_16:
                    return 2;
            }
            return 0;
        }
        
        
        private byte getPALevel(rf24_pa_dbm_e level){
            switch(level){
                case rf24_pa_dbm_e.RF24_PA_MIN:
                    return 0;
                case rf24_pa_dbm_e.RF24_PA_LOW:
                    return 1;
                case rf24_pa_dbm_e.RF24_PA_HIGH:
                    return 2;
                case rf24_pa_dbm_e.RF24_PA_MAX:
                    return 3;
                case rf24_pa_dbm_e.RF24_PA_ERROR:
                    return 4;
            }
            return 0;
        }
        
        
        private rf24_pa_dbm_e getPALevel(byte level){
            switch(level){
                case 0:
                    return rf24_pa_dbm_e.RF24_PA_MIN;
                case 1:
                    return rf24_pa_dbm_e.RF24_PA_LOW;
                case 2:
                    return rf24_pa_dbm_e.RF24_PA_HIGH;
                case 3:
                    return rf24_pa_dbm_e.RF24_PA_MAX;
                case 4:
                    return rf24_pa_dbm_e.RF24_PA_ERROR;
            }
            return rf24_pa_dbm_e.RF24_PA_ERROR;
        }

        void print_status(byte _status)
        {
            Debug.Log("STATUS\t\t= 0x" + String.Format("{0:X}", _status) + " RX_DR=" + String.Format("{0:X}", (_status & (1 << RX_DR)) > 0? 1 : 0) + " TX_DS=" + String.Format("{0:X}", (_status & (1 << TX_DS)) > 0 ? 1 : 0) + " MAX_RT=" + String.Format("{0:X}", (_status & (1 << MAX_RT)) > 0 ? 1 : 0) + " RX_P_NO=" + String.Format("{0:X}", ((_status >> RX_P_NO) & 0x07)) + " TX_FULL=" + String.Format("{0:X}", (_status & (1 << TX_FULL)) > 0 ? 1 : 0));
        }
        
        public void printDetails()
        {
            int spi_speed = 10000000;
            Debug.Log("SPI Speedz\t= " + (spi_speed / 1000000) + " Mhz"); //Print the SPI speed on non-Linux devices
            
            print_status(get_status());
            
            Debug.Log(print_address_register(("RX_ADDR_P0-1"), RX_ADDR_P0, 2));
            Debug.Log(print_byte_register(("RX_ADDR_P2-5"), RX_ADDR_P2, 4));
            Debug.Log(print_address_register(("TX_ADDR\t"), TX_ADDR, 1));
            
            Debug.Log(print_byte_register(("RX_PW_P0-6"), RX_PW_P0, 6));
            Debug.Log(print_byte_register(("EN_AA\t"), EN_AA, 1));
            Debug.Log(print_byte_register(("EN_RXADDR"), EN_RXADDR, 1));
            Debug.Log(print_byte_register(("RF_CH\t"), RF_CH, 1));
            Debug.Log(print_byte_register(("RF_SETUP"), RF_SETUP, 1));
            Debug.Log(print_byte_register(("CONFIG\t"), NRF_CONFIG, 1));
            Debug.Log(print_byte_register(("DYNPD/FEATURE"), DYNPD, 2));
            
            Debug.Log("Data Rate\t" + rf24_datarate_e_str_P[getDataRate(false)]);
            Debug.Log("Model\t\t= " + rf24_model_e_str_P[isPVariant() ? 1 : 0]);
            Debug.Log("CRC Length\t" + rf24_crclength_e_str_P[getCRCLength(false)]);
            Debug.Log("PA Power\t" + rf24_pa_dbm_e_str_P[getPALevel()]);
            Debug.Log("ARC\t\t= " + getARC());
        }
        
        
        
        
        
        public void printPretty(){
            byte channel = getChannel();
            ushort frequency = (ushort)(channel + 2400);
            Debug.Log(("Channel\t\t\t= " + channel + " (~ " + frequency + " MHz)"));
            Debug.Log(("Model\t\t\t= " + rf24_model_e_str_P[isPVariant() ? 1 : 0]));
            Debug.Log(("RF Data Rate\t\t" + rf24_datarate_e_str_P[getDataRate(false)]));
            Debug.Log(("RF Power Amplifier\t" + rf24_pa_dbm_e_str_P[getPALevel()]));
            Debug.Log(("RF Low Noise Amplifier\t" + rf24_feature_e_str_P[(read_register(RF_SETUP) & 1) * 1]));
            Debug.Log(("CRC Length\t\t" + rf24_crclength_e_str_P[getCRCLength(false)]));
            Debug.Log(("Address Length\t\t= " + ((read_register(SETUP_AW) & 3) + 2) + " bytes"));
            Debug.Log(("Static Payload Length\t= " + getPayloadSize() + " bytes"));
            
            byte setupRetry = read_register(SETUP_RETR);
            Debug.Log(("Auto Retry Delay\t= " + ((setupRetry >> ARD) * 250 + 250) + " microseconds"));
            Debug.Log(("Auto Retry Attempts\t= " + (setupRetry & 0x0F) + " maximum"));
            
            byte observeTx = read_register(OBSERVE_TX);
            Debug.Log(("Packets lost on\n    current channel\t= " + (observeTx >> 4)));
            Debug.Log(("Retry attempts made for\n    last transmission\t= " + (observeTx & 0x0F)));
            
            byte features = read_register(FEATURE);
            Debug.Log(("Multicast\t\t" + rf24_feature_e_str_P[(features & (1 << ((EN_DYN_ACK))) * 2)]));
            Debug.Log(("Custom ACK Payload\t" + rf24_feature_e_str_P[(features & (1 << ((EN_ACK_PAY))) * 1)]));
            
            byte dynPl = read_register(DYNPD);
            Debug.Log(("Dynamic Payloads\t" + rf24_feature_e_str_P[((dynPl > 0 ? true : false) && ((features & (1 << ((EN_DPL)))) > 0 ? true: false)) ? 1 : 0]));
            
            byte autoAck = read_register(EN_AA);
            if (autoAck == 0x3F || autoAck == 0) {
                // all pipes have the same configuration about auto-ack feature
                Debug.Log(("Auto Acknowledgment\t" + rf24_feature_e_str_P[autoAck != 0 ? 1 : 0]));
            }
            else {
                // representation per pipe
                Debug.Log(("Auto Acknowledgment\t= 0" + 
                ((char) (((autoAck & (1 << ((ENAA_P5)))) + 48)))+""+
                ((char) (((autoAck & (1 << ((ENAA_P4)))) + 48)))+""+
                ((char) (((autoAck & (1 << ((ENAA_P3)))) + 48)))+""+
                ((char) (((autoAck & (1 << ((ENAA_P2)))) + 48)))+""+
                ((char) (((autoAck & (1 << ((ENAA_P1)))) + 48)))+""+
                ((char) (((autoAck & (1 << ((ENAA_P0)))) + 48)))));
            }
            
            config_reg = read_register(NRF_CONFIG);
            Debug.Log(("Primary Mode\t\t= " + ((config_reg & (1 << ((PRIM_RX)))) != 0 ? 'R' : 'T') + "X"));
            Debug.Log(print_address_register(("TX address\t"), TX_ADDR, 1));
            
            byte openPipes = read_register(EN_RXADDR);
            for (byte i = 0; i < 6; ++i) {
                bool isOpen = (openPipes & (1 << ((i)))) > 0 ? true : false;
                
                if (i < 2) {
                    Debug.Log(("pipe " + i + " (" + rf24_feature_e_str_P[(isOpen ? 1 : 0) + 3] + ") bound" + print_address_register((""), (byte)(RX_ADDR_P0 + i), 1)));
                }
                else {
                    Debug.Log(("pipe " + i + " (" + rf24_feature_e_str_P[(isOpen ? 1 : 0) + 3] + ") bound" + print_byte_register((""), (byte)(RX_ADDR_P0 + i), 1)));
                }
            }
        }
    
}


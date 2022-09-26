#include <stdio.h>
#include <libusb.h>
#include <string>
#include <cstring>
#include <unistd.h>

using namespace std;

#define TIMEOUT 10*1000

bool usbinit = false;
volatile bool running = false;
struct libusb_device **devs;
struct libusb_device *dev;
struct libusb_config_descriptor *cfg;
struct libusb_device_handle *dev_handle;
const struct libusb_endpoint_descriptor* inep;
const struct libusb_endpoint_descriptor* outep;
void (*callback)(uint16_t idVendor, uint16_t idProduct, bool event) = NULL;

extern "C" void setPlugCallBack(void (*callbackk)(uint16_t idVendor, uint16_t idProduct, bool event)){
    callback = callbackk;
}

extern "C" int hotplug_callback(struct libusb_context *ctx, struct libusb_device *dev,
                     libusb_hotplug_event event, void *user_data) {
    static libusb_device_handle *dev_handle = NULL;
    struct libusb_device_descriptor desc;
    int rc;
    printf("Got Hotplugged\n");
    (void)libusb_get_device_descriptor(dev, &desc);
    
    bool eventb = (event == LIBUSB_HOTPLUG_EVENT_DEVICE_ARRIVED) ? true : false;
    if(callback){
        callback(desc.idVendor, desc.idProduct, eventb);
    }
    printf("%04x:%04x-%u\n", desc.idVendor, desc.idProduct, event);
    return 0;
}
                     
extern "C" int usbTransfer(uint8_t *outBuf, uint8_t outBufLength, int &bytesOut, uint8_t *inBuf, uint8_t inBufLength, int &bytesIn){
    if(!running || !usbinit){
        return -8;
    }
    if(!dev_handle){
        return -3;
    }
    if(!outep){
        return -4;
    }
    if(!inep){
        return -5;
    }
    if(outep->wMaxPacketSize < outBufLength){
        printf("Out Buf Length to Long must be %u<=%u\n", outBufLength, outep->wMaxPacketSize);
        return -6;
    }
    if(inep->wMaxPacketSize < inBufLength){
        printf("In Buf Length to Long must be %u<=%u\n", inBufLength, inep->wMaxPacketSize);
        return -7;
    }
    int r;
    if((r = libusb_bulk_transfer(dev_handle, outep->bEndpointAddress, outBuf, outBufLength, &bytesOut, TIMEOUT)) == 0){
        if((r = libusb_bulk_transfer(dev_handle, inep->bEndpointAddress, inBuf, inBufLength, &bytesIn, TIMEOUT)) != 0){
            printf("Bulk Read Error %s\n", libusb_error_name(r));                                    
            return -2;
        }
    }else{
        printf("Bulk Write Error %s\n", libusb_error_name(r));
        return -1;
    }
    return 0;
}

extern "C" int connectToUSBDecive(uint16_t idVendor, uint16_t idProduct){
    if(!running || !usbinit){
        return -8;
    }
    int r;
    int count = libusb_get_device_list(NULL, &devs);
    for(int i = 0; i < count; i++){
        struct libusb_device_descriptor desc;
        if ((r = libusb_get_device_descriptor(devs[i], &desc)) == 0) {
            if(desc.idVendor == idVendor && desc.idProduct == idProduct){
                dev = libusb_ref_device(devs[i]);
                if(dev){
                    printf("Dev in Main!\n");
                    if((r = libusb_get_active_config_descriptor(dev, &cfg)) == 0){
                        printf("NumInter %u\n", cfg[0].bNumInterfaces);
                        printf("Interface AltSettings %u\n", cfg[0].interface[0].num_altsetting);
                        printf("Altetting %p\n", cfg[0].interface[0].altsetting);
                        printf("Altetting %p\n", cfg[0].interface[0].altsetting->bLength);
                        printf("Number of Endpoints %u\n", cfg[0].interface[0].altsetting->bNumEndpoints);
                        for(int l = 0; l < cfg[0].interface[0].altsetting->bNumEndpoints; l++){
                            if((cfg[0].interface[0].altsetting->endpoint[l].bEndpointAddress & LIBUSB_ENDPOINT_IN) == LIBUSB_ENDPOINT_IN){
                                printf("Found IN Endpoint! %u %u\n", cfg[0].interface[0].altsetting->endpoint[l].bmAttributes, LIBUSB_ENDPOINT_TRANSFER_TYPE_BULK);
                                inep = &(cfg[0].interface[0].altsetting->endpoint[l]);
                            }else{
                                if((cfg[0].interface[0].altsetting->endpoint[l].bEndpointAddress & LIBUSB_ENDPOINT_OUT) == LIBUSB_ENDPOINT_OUT){
                                    printf("Found OUT Endpoint! %u %u\n", cfg[0].interface[0].altsetting->endpoint[l].bmAttributes, LIBUSB_ENDPOINT_TRANSFER_TYPE_BULK);   
                                    outep = &(cfg[0].interface[0].altsetting->endpoint[l]);
                                }
                            }
                        }
                        printf("Done\n");
                        if((r = libusb_open(dev, &dev_handle)) == 0){
                            if((r = libusb_claim_interface(dev_handle, 0)) == 0){
                                printf("Opened the dev handle\n");
                                return 0;
                            }else{
                                printf("Claim Interface Error %s\n", libusb_error_name(r));
                            }
                        }else{
                            printf("Device Open Error %s\n", libusb_error_name(r));
                        }
                    }else{
                        printf("Config Error %s\n", libusb_error_name(r));
                    }
                }
                i = count;
            }
        }else{
            printf("Device Descriptor Error %s\n", libusb_error_name(r));
        }
    }
    return -1;
}

extern "C" void get_usb_devices(char *out){
    if(!running || !usbinit){
        return;
    }
    struct libusb_device **devs;
    int r;
    int count = libusb_get_device_list(NULL, &devs);
    if(count > 0){
        int n = 0;
        for(int i = 0; i < count; i++){
            struct libusb_device_descriptor desc;
            if((r = libusb_get_device_descriptor(devs[i], &desc)) == 0){
                n += sprintf(&out[n], "%04x:%04x\n", desc.idVendor, desc.idProduct);
            }else{
                printf("Cant get Dev Descr %s\n", libusb_error_name(r));
            }
        }
        libusb_free_device_list(devs, 1);
    }else if(count == 0){
        printf("No Devices Found\n");
    }else{
        printf("Init Error %s\n", libusb_error_name(count));
    }    
}

extern "C" void stop(){
    if(inep){
        inep = NULL;
    }
    if(outep){
        outep = NULL;
    }
    if(cfg){
        libusb_free_config_descriptor(cfg);
        cfg = NULL;
    }
    if(dev_handle){
        libusb_close(dev_handle);
        dev_handle = NULL;
    }
    if(dev){
        libusb_free_device_list(devs, 1);
        dev = NULL;
    }
    running = false;
}

extern "C" int run(){
    int errorCode;
    int returnCode = 0;
    bool registered_HotPlug_Callback = false;
    libusb_hotplug_callback_handle callback_handle;
    running = true;
    if((errorCode = libusb_init(NULL)) == 0){
        usbinit = true;
        if((errorCode = libusb_hotplug_register_callback(NULL, LIBUSB_HOTPLUG_EVENT_DEVICE_ARRIVED |
            LIBUSB_HOTPLUG_EVENT_DEVICE_LEFT, 0, LIBUSB_HOTPLUG_MATCH_ANY, LIBUSB_HOTPLUG_MATCH_ANY,
            LIBUSB_HOTPLUG_MATCH_ANY, hotplug_callback, NULL,
            &callback_handle)) == 0){
            registered_HotPlug_Callback = true;
        while(running){
            libusb_handle_events_completed(NULL, NULL);
            usleep(1000);
        }   
            }else{
                printf("Register Hotplug Error %s\n", libusb_error_name(errorCode));
            }
    }else{
        printf("Init Error %s\n", libusb_error_name(errorCode));
        returnCode = -1;
    }
    running = false;
    if(registered_HotPlug_Callback){
        libusb_hotplug_deregister_callback(NULL, callback_handle);
    }
    usbinit = false;
    libusb_exit(NULL);
    return returnCode;
}

extern "C" int main(){
    printf("Hello World!\n");
    //run();
    //char out[2049];
    //get_usb_devices(out);
    //printf("%s\n", out);
    //printf("Connected? %u\n", connectToUSBDecive(0x0000, 0x0001));
    return 0;
}



                    

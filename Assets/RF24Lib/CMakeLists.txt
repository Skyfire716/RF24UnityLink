PROJECT(myproject)
CMAKE_MINIMUM_REQUIRED(VERSION 3.12)

project(pico_examples C CXX ASM)

SET(mylibSRCS UnityUSBInterface.cpp)

find_path(LIBUSB_INCLUDE_DIR
  NAMES libusb.h
    PATH_SUFFIXES "include" "libusb" "libusb-1.0")
find_library(LIBUSB_LIBRARY
  NAMES usb-1.0
    PATH_SUFFIXES "lib" "lib64"
    HINTS "${LIBUSB_INCLUDE_DIR}")

ADD_LIBRARY(RF24USBInterace SHARED ${mylibSRCS})
target_include_directories(RF24USBInterace PUBLIC ${LIBUSB_INCLUDE_DIR})
TARGET_LINK_LIBRARIES(RF24USBInterace PUBLIC ${LIBUSB_LIBRARY})

ADD_EXECUTABLE(RF24USBInteraceExec ${mylibSRCS})
target_include_directories(RF24USBInteraceExec PUBLIC ${LIBUSB_INCLUDE_DIR})
TARGET_LINK_LIBRARIES(RF24USBInteraceExec ${LIBUSB_LIBRARY})

add_compile_options(-Wall
    -Wno-format          # int != int32_t as far as the compiler is concerned because gcc has int32_t as long int
    -Wno-unused-function # we have some for the docs that aren't called
    -Wno-maybe-uninitialized
    )


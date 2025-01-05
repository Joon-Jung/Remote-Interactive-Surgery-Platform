# Remote-Interactive-Surgery-Platform

This repository contains code for a remote interactive surgery platform running on Micosoft HoloLens 2. For a detailed explanation of this software and evaluaation of our method, please refer to our paper [Remote Interactive Surgery Platform (RISP): Proof of Concept for an Augmented-Reality-Based Platform for Surgical Telementoring](https://www.mdpi.com/2313-433X/9/3/56).

**05/Jan/2025**: We have fixed the issues with RISP-HL2.

## Prerequisties

To use the voice communication functionality, it requires WebRTC signaler server. Please refer to [this document](https://microsoft.github.io/MixedReality-WebRTC/versions/release/1.0/manual/helloworld-unity-signaler.html#install-and-run-node-dss)'s `Install and run node-dss section` to setup the WebRTC signaler server, and make sure the server can take inbound connections from the Internet.

This software requires a Microsoft HoloLens 2 and a PC and those two devices can be reached to each other through a network.

## Dependencies

* [Unity 2019.4.28f1](https://unity.com/releases/editor/whats-new/2019.4.28) with `Universal Windows Platform Build Support` and `Windows Build Support (IL2CPP)`.
* [Visual Studio 2019](https://developer.microsoft.com/en-us/windows/downloads/) with `.NET desktop development`, `Desktop development with C++`, `Universal Windows Platform (UWP) development`, and `Game development with Unity`.
* [Anaconda](https://www.anaconda.com/products/distribution) with `python 3.7`

## How to Install

### Installing RISP-HL2

* Open `RISP-HL2` with Unity.
* Open `RISP-HL` scene and insert the WebRTC signaler server address to `Managers > WebRTCManager > NodeDssSignaler > Http Server Address` in the hierachy.
* Build the scene with targeting `Universal Windows Platform`.
* Open `package.appxmanifest` in the build folder and add the restricted capability to the manifest file.

```xml
<Package 
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest" 
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" 
  xmlns:uap2="http://schemas.microsoft.com/appx/manifest/uap/windows10/2" 
  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3" 
  xmlns:uap4="http://schemas.microsoft.com/appx/manifest/uap/windows10/4" 
  xmlns:iot="http://schemas.microsoft.com/appx/manifest/iot/windows10" 
  xmlns:mobile="http://schemas.microsoft.com/appx/manifest/mobile/windows10" 
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" 
  IgnorableNamespaces="uap uap2 uap3 uap4 mp mobile iot rescap" 
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"> 
```

```xml
  <Capabilities>
    <rescap:Capability Name="perceptionSensorsExperimental" />
    ...
  </Capabilities>
```

* Open `RISP-HL2.sln` in the build folder.
* Deploy with Release / ARM (Not ARM64)

### Installing RISP-Com main_application

* Create a virtual environment of `python 3.7` using `Anaconda`.
* Open `RISP-Com\main_application` and intall depenendices through `pip install -r requirements.txt`.

### Installing RISP-Com voice_communic

* Open `RISP-Com\voice_communicator` with Unity.
* Open `Voice_client` scene and insert the WebRTC signaler server address to `NodeDssSignaler > Http Server Address`.
* Build the scene with targeting `PC, Mav & Linux Standalone`.

## How to run

0. Get your HoloLens's IP address.
1. Launch `RISP-HL2` in your HoloLens.
2. Launch `main_application` by `python MainClient.py` in `RISP-Com\main_application`.
3. Enter HoloLens's IP address.
4. Launch built `Voice_Client` and click `CreateOffer` button.

## Work in progress

* [x] First version of remote interactive surgery platform code released.
* [ ] Add measurement tools (e.g., rulers in `RISP-HL2`).
* [ ] Detailed intrcution of `How to use`.
* etc.....

## Citation

Please cite our paper.

    @Article{jimaging9030056,
        AUTHOR = {Kalbas, Yannik and Jung, Hoijoon and Ricklin, John and Jin, Ge and Li, Mingjian and Rauer, Thomas and Dehghani, Shervin and Navab, Nassir and Kim, Jinman and Pape, Hans-Christoph and Heining, Sandro-Michael},
        TITLE = {Remote Interactive Surgery Platform (RISP): Proof of Concept for an Augmented-Reality-Based Platform for Surgical Telementoring},
        JOURNAL = {Journal of Imaging},
        VOLUME = {9},
        YEAR = {2023},
        NUMBER = {3},
        ARTICLE-NUMBER = {56},
        URL = {https://www.mdpi.com/2313-433X/9/3/56},
        ISSN = {2313-433X},
        DOI = {10.3390/jimaging9030056}
    }

## Acknowledgement

This code has been built with referencing,  modifying, and using the following libraries / repositoies:

* [Mixed Reality Toolkit](https://github.com/microsoft/MixedRealityToolkit-Unity)
* [HoloLensCameraStream](https://github.com/VulcanTechnologies/HoloLensCameraStream)
* [HoloLens2-ResearchMode-Unity](https://github.com/petergu684/HoloLens2-ResearchMode-Unity)
* [MixedReality-WebRTC](https://github.com/microsoft/MixedReality-WebRTC)
* [Open3D](http://www.open3d.org/)
* [OpenCV](https://opencv.org/)
* [HoloLens2-Unity-ResearchModeStreamer](https://github.com/cgsaxner/HoloLens2-Unity-ResearchModeStreamer)

We thank authors of those codes and opensource community for making possible to build this code.

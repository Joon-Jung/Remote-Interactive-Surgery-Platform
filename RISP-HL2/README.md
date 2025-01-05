# RISP_internal
- After building the visual studio solution from Unity, go to `App/[Project name]/Package.appxmanifest` and add the restricted capability to the manifest file. (Same as what you would do to enable research mode on HoloLens 1, reference: http://akihiro-document.azurewebsites.net/post/hololens_researchmode2/)
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
    <Capability Name="internetClient" />
    <Capability Name="internetClientServer" />
    <Capability Name="privateNetworkClientServer" />
    <uap2:Capability Name="spatialPerception" />
    <DeviceCapability Name="backgroundSpatialPerception"/>
    <DeviceCapability Name="webcam" />
  </Capabilities>
```
`<DeviceCapability Name="backgroundSpatialPerception"/>` is only necessary if you use IMU sensor. 
- Save the changes and deploy the solution to your HoloLens 2.
//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using UnityEngine;
using UnityEngine.XR.WSA;

using System;

using HoloLensCameraStream;

/// <summary>
/// This example gets the video frames at 30 fps and displays them on a Unity texture,
/// which is locked the User's gaze.
/// </summary>
public struct RGBImageFrame
{
    public RGBImageFrame(byte[] rgbImageByte, float[] projectionMat, float[] camToWorldMat, int width, int height)
    {
        RgbImageByte = rgbImageByte;
        ProjectionMat = projectionMat;
        CamToWorldMat = camToWorldMat;
        Width = width;
        Height = height;
    }
    public RGBImageFrame(RGBImageFrame rGBImage)
    {
        RgbImageByte = rGBImage.RgbImageByte;
        ProjectionMat = rGBImage.ProjectionMat;
        CamToWorldMat = rGBImage.CamToWorldMat;
        Width = rGBImage.Width;
        Height = rGBImage.Height;
    }

    public byte[] RgbImageByte;
    public float[] ProjectionMat;
    public float[] CamToWorldMat;
    public int Width;
    public int Height;
};
public class VideoPanelHelper : MonoBehaviour
{
    byte[] _latestImageBytes;
    HoloLensCameraStream.Resolution _resolution;

    //"Injected" objects.
    public VideoPanelObject _videoPanelUI;
    HoloLensCameraStream.VideoCapture _videoCapture;

    IntPtr _spatialCoordinateSystemPtr;

    VideoCaptureSample videoCaptureSample;
    private RGBImageFrame rgbImageFrame;
    private static object lockObj = new object();
    public bool hologramCapture = false;
    [Range(0.0f, 1.0f)] public float hologramOpacity = 0.8f;
    public TCPClientHelperFour clientHelperFour;
    private bool frameSkipper = false;
    void Start()
    {
        rgbImageFrame = new RGBImageFrame(null, null, null, 0, 0);
        //Fetch a pointer to Unity's spatial coordinate system if you need pixel mapping
        // //HL1
        // _spatialCoordinateSystemPtr = WorldManager.GetNativeISpatialCoordinateSystemPtr();
        // Hololens 2
        _spatialCoordinateSystemPtr = Microsoft.MixedReality.Toolkit.WindowsMixedReality.WindowsMixedRealityUtilities.UtilitiesProvider.ISpatialCoordinateSystemPtr;

        //Call this in Start() to ensure that the CameraStreamHelper is already "Awake".
        CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
        //You could also do this "shortcut":
        //CameraStreamManager.Instance.GetVideoCaptureAsync(v => videoCapture = v);

        //_videoPanelUI = GameObject.FindObjectOfType<VideoPanel>();
    }

    private void OnDestroy()
    {
        if (_videoCapture != null)
        {
            _videoCapture.FrameSampleAcquired -= OnFrameSampleAcquired;
            _videoCapture.Dispose();
        }
    }

    void OnVideoCaptureCreated(HoloLensCameraStream.VideoCapture videoCapture)
    {
        if (videoCapture == null)
        {
            Debug.LogError("Did not find a video capture object. You may not be using the HoloLens.");
            return;
        }
        
        this._videoCapture = videoCapture;

        //Request the spatial coordinate ptr if you want fetch the camera and set it if you need to 
        CameraStreamHelper.Instance.SetNativeISpatialCoordinateSystemPtr(_spatialCoordinateSystemPtr);

        _resolution = CameraStreamHelper.Instance.GetLowestResolution();
        float frameRate = CameraStreamHelper.Instance.GetHighestFrameRate(_resolution);
        videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

        //You don't need to set all of these params.
        //I'm just adding them to show you that they exist.
        HoloLensCameraStream.CameraParameters cameraParams = new HoloLensCameraStream.CameraParameters();
        cameraParams.cameraResolutionHeight = _resolution.height;
        cameraParams.cameraResolutionWidth = _resolution.width;
        cameraParams.frameRate = Mathf.RoundToInt(frameRate);
        cameraParams.pixelFormat = HoloLensCameraStream.CapturePixelFormat.BGRA32;
        //cameraParams.rotateImage180Degrees = true; //If your image is upside down, remove this line.
        cameraParams.enableHolograms = hologramCapture;
        cameraParams.hologramOpacity = hologramOpacity;

        ThreadUtils.Instance.InvokeOnMainThread(() => { _videoPanelUI.SetResolution(_resolution.width, _resolution.height); });

        rgbImageFrame.Width = _resolution.width;
        rgbImageFrame.Height = _resolution.height;

        videoCapture.StartVideoModeAsync(cameraParams, OnVideoModeStarted);
    }

    void OnVideoModeStarted(VideoCaptureResult result)
    {
        if (result.success == false)
        {
            Debug.LogWarning("Could not start video mode.");
            return;
        }

        Debug.Log("Video capture started.");
    }

    void OnFrameSampleAcquired(VideoCaptureSample sample)
    {
        //When copying the bytes out of the buffer, you must supply a byte[] that is appropriately sized.
        //You can reuse your byte[] unless you need to resize it for some reason.
        if (_latestImageBytes == null || _latestImageBytes.Length < sample.dataLength)
        {
            _latestImageBytes = new byte[sample.dataLength];
        }
        sample.CopyRawImageDataIntoBuffer(_latestImageBytes);

        //If you need to get the cameraToWorld matrix for purposes of compositing you can do it like this
        float[] cameraToWorldMatrix;
        if (sample.TryGetCameraToWorldMatrix(out cameraToWorldMatrix) == false)
        {
            return;
        }
        
        //If you need to get the projection matrix for purposes of compositing you can do it like this
        float[] projectionMatrix;
        if (sample.TryGetProjectionMatrix(out projectionMatrix) == false)
        {
            return;
        }

        sample.Dispose();

        //This is where we actually use the image data
        ThreadUtils.Instance.InvokeOnMainThread(() =>
        {
            _videoPanelUI.SetBytes(_latestImageBytes);
        });
        lock (lockObj)
        {
            rgbImageFrame.RgbImageByte = _latestImageBytes;
            rgbImageFrame.CamToWorldMat = cameraToWorldMatrix;
            rgbImageFrame.ProjectionMat = projectionMatrix;
        }
#if WINDOWS_UWP
        frameSkipper = !frameSkipper;
        if(clientHelperFour!= null&& clientHelperFour.isVideoChannelConnected() && !frameSkipper)
        {
            //clientHelperFour.TransmittingVideoFrame(rgbImageFrame);
            //ThreadUtils threadUtils = ThreadUtils.Instance;
            //byte[] encodedImage = null;
            //threadUtils.InvokeOnMainThread(() => {
            //    Texture2D texture = new Texture2D(rgbImageFrame.Width, rgbImageFrame.Height, TextureFormat.BGRA32, false);
            //    texture.LoadRawTextureData(rgbImageFrame.RgbImageByte);
            //    texture.Apply();
            //    encodedImage = texture.EncodeToJPG();
            //    UnityEngine.Object.Destroy(texture);
            //    clientHelperFour.TransmittingVideoFrame(encodedImage);
            //});
            SendVideo(rgbImageFrame);
        }
#endif
    }
#if WINDOWS_UWP
    public async void SendVideo(RGBImageFrame rGBImageFrame)
    {
        byte[] encodedImage = ImageConversion.EncodeArrayToJPG(rGBImageFrame.RgbImageByte, UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SRGB, (uint)rGBImageFrame.Width, (uint)rGBImageFrame.Height);
        clientHelperFour.TransmittingVideoFrame(encodedImage);
    }
#endif
    public RGBImageFrame GetLatestRGBImage()
    {
        RGBImageFrame returningFrame = new RGBImageFrame(null, null, null, 0, 0);
        lock (lockObj)
        {
            returningFrame = new RGBImageFrame(rgbImageFrame);
        }
        return returningFrame;
    }
}

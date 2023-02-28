using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using TMPro;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

public class ResearchModeVideoStreamHelper : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;
#endif

    enum DepthSensorMode
    {
        ShortThrow,
        LongThrow,
        None
    };
    [SerializeField] DepthSensorMode depthSensorMode = DepthSensorMode.ShortThrow;
    [SerializeField] bool enablePointCloud = true;
    [SerializeField] bool showPointCloud = false;
    [SerializeField] bool showPreview = false;

    TCPClient tcpClient;

    public GameObject depthPreviewPlane = null;
    private Material depthMediaMaterial = null;
    private Texture2D depthMediaTexture = null;
    private byte[] depthFrameData = null;

    public GameObject shortAbImagePreviewPlane = null;
    private Material shortAbImageMediaMaterial = null;
    private Texture2D shortAbImageMediaTexture = null;
    private byte[] shortAbImageFrameData = null;

    public GameObject longDepthPreviewPlane = null;
    private Material longDepthMediaMaterial = null;
    private Texture2D longDepthMediaTexture = null;
    private byte[] longDepthFrameData = null;

    public GameObject longAbImagePreviewPlane = null;
    private Material longAbImageMediaMaterial = null;
    private Texture2D longAbImageMediaTexture = null;
    private byte[] longAbImageFrameData = null;


    public TextMeshPro text;

    public GameObject pointCloudRendererGo;
    public Color pointColor = Color.white;
    private PointCloudRenderer pointCloudRenderer;
#if ENABLE_WINMD_SUPPORT
    Windows.Perception.Spatial.SpatialCoordinateSystem unityWorldOrigin;
#endif

    private void Awake()
    {
#if ENABLE_WINMD_SUPPORT
#if UNITY_2020_1_OR_NEWER // note: Unity 2021.2 and later not supported
        IntPtr WorldOriginPtr = UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
        unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
#else
        IntPtr WorldOriginPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
        unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
#endif
#endif
    }
    void Start()
    {
        if (depthSensorMode == DepthSensorMode.ShortThrow)
        {
            if (depthPreviewPlane != null)
            {
                depthMediaMaterial = depthPreviewPlane.GetComponent<MeshRenderer>().material;
                depthMediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
                depthMediaMaterial.mainTexture = depthMediaTexture;
            }

            if (shortAbImagePreviewPlane != null)
            {
                shortAbImageMediaMaterial = shortAbImagePreviewPlane.GetComponent<MeshRenderer>().material;
                shortAbImageMediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
                shortAbImageMediaMaterial.mainTexture = shortAbImageMediaTexture;
            }
            longDepthPreviewPlane.SetActive(false);
            longAbImagePreviewPlane.SetActive(false);
        }
        
        if (depthSensorMode == DepthSensorMode.LongThrow)
        {
            if (longDepthPreviewPlane != null)
            {
                longDepthMediaMaterial = longDepthPreviewPlane.GetComponent<MeshRenderer>().material;
                longDepthMediaTexture = new Texture2D(320, 288, TextureFormat.Alpha8, false);
                longDepthMediaMaterial.mainTexture = longDepthMediaTexture;
            }

            if (longAbImagePreviewPlane != null)
            {
                longAbImageMediaMaterial = longAbImagePreviewPlane.GetComponent<MeshRenderer>().material;
                longAbImageMediaTexture = new Texture2D(320, 288, TextureFormat.Alpha8, false);
                longAbImageMediaMaterial.mainTexture = longAbImageMediaTexture;
            }
            depthPreviewPlane.SetActive(false);
            shortAbImagePreviewPlane.SetActive(false);
        }

        if (pointCloudRendererGo != null)
        {
            pointCloudRenderer = pointCloudRendererGo.GetComponent<PointCloudRenderer>();
        }

        tcpClient = GetComponent<TCPClient>();

#if ENABLE_WINMD_SUPPORT
        researchMode = new HL2ResearchMode();

        // Depth sensor should be initialized in only one mode
        if (depthSensorMode == DepthSensorMode.LongThrow) researchMode.InitializeLongDepthSensor();
        else if (depthSensorMode == DepthSensorMode.ShortThrow) researchMode.InitializeDepthSensor();
        
        researchMode.InitializeSpatialCamerasFront();
        researchMode.SetReferenceCoordinateSystem(unityWorldOrigin);
        researchMode.SetPointCloudDepthOffset(0);

        // Depth sensor should be initialized in only one mode
        if (depthSensorMode == DepthSensorMode.LongThrow) researchMode.StartLongDepthSensorLoop(enablePointCloud);
        else if (depthSensorMode == DepthSensorMode.ShortThrow) researchMode.StartDepthSensorLoop(enablePointCloud);

#endif
    }

    void LateUpdate()
    {
#if ENABLE_WINMD_SUPPORT
        // update depth map texture
        if (depthSensorMode == DepthSensorMode.ShortThrow && showPreview && 
            depthPreviewPlane != null && researchMode.DepthMapTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetDepthMapTextureBuffer();
            if (frameTexture.Length > 0)
            {
                if (depthFrameData == null)
                {
                    depthFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, depthFrameData, 0, depthFrameData.Length);
                }

                depthMediaTexture.LoadRawTextureData(depthFrameData);
                depthMediaTexture.Apply();
            }
        }

        // update short-throw AbImage texture
        if (depthSensorMode == DepthSensorMode.ShortThrow && showPreview && 
            shortAbImagePreviewPlane != null && researchMode.ShortAbImageTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetShortAbImageTextureBuffer();
            if (frameTexture.Length > 0)
            {
                if (shortAbImageFrameData == null)
                {
                    shortAbImageFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, shortAbImageFrameData, 0, shortAbImageFrameData.Length);
                }

                shortAbImageMediaTexture.LoadRawTextureData(shortAbImageFrameData);
                shortAbImageMediaTexture.Apply();
            }
        }

        // update long depth map texture
        if (depthSensorMode == DepthSensorMode.LongThrow && showPreview && 
            longDepthPreviewPlane != null && researchMode.LongDepthMapTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
            if (frameTexture.Length > 0)
            {
                if (longDepthFrameData == null)
                {
                    longDepthFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, longDepthFrameData, 0, longDepthFrameData.Length);
                }

                longDepthMediaTexture.LoadRawTextureData(longDepthFrameData);
                longDepthMediaTexture.Apply();
            }
        }

        // update long-throw AbImage texture
        if (depthSensorMode == DepthSensorMode.LongThrow && showPreview &&
            longAbImagePreviewPlane != null && researchMode.LongAbImageTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetLongAbImageTextureBuffer();
            if (frameTexture.Length > 0)
            {
                if (longAbImageFrameData == null)
                {
                    longAbImageFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, longAbImageFrameData, 0, longAbImageFrameData.Length);
                }

                longAbImageMediaTexture.LoadRawTextureData(longAbImageFrameData);
                longAbImageMediaTexture.Apply();
            }
        }

        // Update point cloud
        UpdatePointCloud();
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private void UpdatePointCloud()
    {
        if (enablePointCloud && showPointCloud && pointCloudRendererGo != null)
        {
            if ((depthSensorMode == DepthSensorMode.LongThrow && !researchMode.LongThrowPointCloudUpdated()) ||
                (depthSensorMode == DepthSensorMode.ShortThrow && !researchMode.PointCloudUpdated())) return;

            float[] pointCloud = new float[] { };
            if (depthSensorMode == DepthSensorMode.LongThrow) pointCloud = researchMode.GetLongThrowPointCloudBuffer();
            else if (depthSensorMode == DepthSensorMode.ShortThrow) pointCloud = researchMode.GetPointCloudBuffer();
            
            if (pointCloud.Length > 0)
            {
                int pointCloudLength = pointCloud.Length / 3;
                Vector3[] pointCloudVector3 = new Vector3[pointCloudLength];
                for (int i = 0; i < pointCloudLength; i++)
                {
                    pointCloudVector3[i] = new Vector3(pointCloud[3 * i], pointCloud[3 * i + 1], pointCloud[3 * i + 2]);
                }
                text.text = "Point Cloud Length: " + pointCloudVector3.Length.ToString();
                pointCloudRenderer.Render(pointCloudVector3, pointColor);
            }
        }
    }

    public float[] GetPointCloud()
    {
        if (enablePointCloud)
        {
            if ((depthSensorMode == DepthSensorMode.LongThrow && !researchMode.LongThrowPointCloudUpdated()) ||
                (depthSensorMode == DepthSensorMode.ShortThrow && !researchMode.PointCloudUpdated())) return null;

            float[] pointCloud = new float[] { };
            if (depthSensorMode == DepthSensorMode.LongThrow) pointCloud = researchMode.GetLongThrowPointCloudBuffer();
            else if (depthSensorMode == DepthSensorMode.ShortThrow) pointCloud = researchMode.GetPointCloudBuffer();

            if (pointCloud.Length > 0)
            {
                //int pointCloudLength = pointCloud.Length / 3;
                //Vector3[] pointCloudVector3 = new Vector3[pointCloudLength];
                //for (int i = 0; i < pointCloudLength; i++)
                //{
                //    pointCloudVector3[i] = new Vector3(pointCloud[3 * i], pointCloud[3 * i + 1], pointCloud[3 * i + 2]);
                //}
                Debug.Log("Point Cloud Length: " + pointCloud.Length.ToString());
                return pointCloud;
            }
        }
        return null;
    }
#endif


    #region Button Event Functions
    public void TogglePreviewEvent()
    {
        showPreview = !showPreview;
    }

    public void TogglePointCloudEvent()
    {
        showPointCloud = !showPointCloud;
        if (showPointCloud)
        {
            pointCloudRendererGo.SetActive(true);
        }
        else
        {
            pointCloudRendererGo.SetActive(false);
        }
    }

    public void StopSensorsEvent()
    {
#if ENABLE_WINMD_SUPPORT
        researchMode.StopAllSensorDevice();
#endif
        showPreview = false;
    }

    public void SaveAHATSensorDataEvent()
    {
#if ENABLE_WINMD_SUPPORT
        var depthMap = researchMode.GetDepthMapBuffer();
        var AbImage = researchMode.GetShortAbImageBuffer();
#if WINDOWS_UWP
        if (tcpClient != null)
        {
            tcpClient.SendUINT16Async(depthMap, AbImage);
        }
#endif
#endif
    }

    public void SaveSpatialImageEvent()
    {
#if ENABLE_WINMD_SUPPORT
#if WINDOWS_UWP
        long ts_ft_left, ts_ft_right;
        var LRFImage = researchMode.GetLRFCameraBuffer(out ts_ft_left, out ts_ft_right);
        Windows.Perception.PerceptionTimestamp ts_left = Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(DateTime.FromFileTime(ts_ft_left));
        Windows.Perception.PerceptionTimestamp ts_right = Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(DateTime.FromFileTime(ts_ft_right));

        long ts_unix_left = ts_left.TargetTime.ToUnixTimeMilliseconds();
        long ts_unix_right = ts_right.TargetTime.ToUnixTimeMilliseconds();
        long ts_unix_current = GetCurrentTimestampUnix();

        text.text = "Left: " + ts_unix_left.ToString() + "\n" +
            "Right: " + ts_unix_right.ToString() + "\n" +
            "Current: " + ts_unix_current.ToString();

        if (tcpClient != null)
        {
            tcpClient.SendSpatialImageAsync(LRFImage, ts_unix_left, ts_unix_right);
        }
#endif
#endif
    }

    #endregion
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }

#if WINDOWS_UWP
    private long GetCurrentTimestampUnix()
    {
        // Get the current time, in order to create a PerceptionTimestamp. 
        Windows.Globalization.Calendar c = new Windows.Globalization.Calendar();
        Windows.Perception.PerceptionTimestamp ts = Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(c.GetDateTime());
        return ts.TargetTime.ToUnixTimeMilliseconds();
    }
    private Windows.Perception.PerceptionTimestamp GetCurrentTimestamp()
    {
        // Get the current time, in order to create a PerceptionTimestamp. 
        Windows.Globalization.Calendar c = new Windows.Globalization.Calendar();
        return Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(c.GetDateTime());
    }
#endif
}
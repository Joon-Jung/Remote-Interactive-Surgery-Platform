using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

#if WINDOWS_UWP
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

public class TCPClientHelperFour : MonoBehaviour
{
    //[SerializeField]
    //public string hostIPAddress;
    public string port_depth = "9090", port_rgb = "9091", port_command = "9092", port_video = "9093", port_image = "9094";
    //public TextMeshPro connectionStatTMP;
    //public TextMeshPro debuggingTMP;
    private bool connected_rgb = false, connected_depth = false, connected_command = false, connected_video = false, connected_image = false;
    private bool m_writeInProgress_rgb = false, m_writeInProgress_depth = false, m_writeInProgress_command = false, m_writeInProgress_video = false;
    public DataHelper dataHelper;
    public RemoteImageReceiver remoteImage;
    void Start()
    {
#if WINDOWS_UWP
        StartDepthServer();
        StartRGBServer();
        StartCommandServer();
        StartVideoServer();
        if(remoteImage != null)
        {
            StartImageServer();
        }
#endif
    }


#if WINDOWS_UWP
    StreamSocketListener m_streamSocketListener_rgb = new StreamSocketListener();
    StreamSocket m_streamSocket_rgb = null;
    DataWriter m_writer_rgb;
    DataReader m_reader_rgb;

    StreamSocketListener m_streamSocketListener_depth = new StreamSocketListener();
    StreamSocket m_streamSocket_depth = null;
    DataWriter m_writer_depth;
    DataReader m_reader_depth;

    StreamSocketListener m_streamSocketListner_command = new StreamSocketListener();
    StreamSocket m_streamSocket_command = null;
    DataWriter m_writer_command;
    DataReader m_reader_command;

    StreamSocketListener m_streamSocketListener_video = new StreamSocketListener();
    StreamSocket m_streamSocket_video = null;
    DataWriter m_writer_video;
    DataReader m_reader_video;

    StreamSocketListener m_streamSocketListener_image = new StreamSocketListener();
    StreamSocket m_streamSocket_image = null;
    DataWriter m_writer_image;
    DataReader m_reader_image;


    private async void StartDepthServer()
    {
        try
        {
            m_streamSocketListener_depth.ConnectionReceived += OnConnectionReceived_depth;
            await m_streamSocketListener_depth.BindServiceNameAsync(port_depth);
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        }
    }
    private async void StartRGBServer()
    {
        try
        {
            m_streamSocketListener_rgb.ConnectionReceived += OnConnectionReceived_rgb;
            await m_streamSocketListener_rgb.BindServiceNameAsync(port_rgb);
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        }
    }
    private async void StartCommandServer()
    {
        try
        {
            m_streamSocketListner_command.ConnectionReceived += OnConnectionReceived_command;
            await m_streamSocketListner_command.BindServiceNameAsync(port_command);
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        }
    }
    private async void StartVideoServer()
    {
        try
        {
            m_streamSocketListener_video.ConnectionReceived += OnConnectionReceived_video;
            await m_streamSocketListener_video.BindServiceNameAsync(port_video);
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connected_video = false;
        }
    }

    private async void StartImageServer()
    {
        try
        {
            m_streamSocketListener_image.ConnectionReceived += OnConnectionReceived_image;
            await m_streamSocketListener_image.BindServiceNameAsync(port_image);
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connected_image = false;
        }
    }

    private void OnConnectionReceived_video(StreamSocketListener streamSocketListener, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        if (m_streamSocket_video != null) m_streamSocket_video.Dispose();
        m_streamSocket_video = args.Socket;
        m_writer_video = new DataWriter(m_streamSocket_video.OutputStream);
        m_writer_video.UnicodeEncoding = UnicodeEncoding.Utf8;
        m_writer_video.ByteOrder = ByteOrder.LittleEndian;
        m_reader_video = new DataReader(m_streamSocket_video.InputStream);

        connected_video = true;
        Debug.Log("Video connected");
        //dataHelper.StartVideoTransmission();
    }

    public async void TransmittingVideoFrame(byte[] encodedImage)
    {
        if (m_writeInProgress_video) return;
        m_writeInProgress_video = true;
        try
        {
            m_writer_video.WriteInt32(encodedImage.Length);
            m_writer_video.WriteBytes(encodedImage);
            await m_writer_video.StoreAsync();
        }
        catch (Exception ex)
        {
            connected_video = false;
            m_writeInProgress_video = false;
            m_writer_video.Dispose();
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        }
        m_writeInProgress_video = false;
    }


    public async void TransmittingVideoFrame(RGBImageFrame rGBImageFrame)
    {
        if (m_writeInProgress_video) return;
        m_writeInProgress_video = true;
        ThreadUtils threadUtils = ThreadUtils.Instance;
        byte[] encodedImage = null;
        threadUtils.InvokeOnMainThread(() => {
            Texture2D texture = new Texture2D(rGBImageFrame.Width, rGBImageFrame.Height, TextureFormat.BGRA32, false);
            texture.LoadRawTextureData(rGBImageFrame.RgbImageByte);
            texture.Apply();
            encodedImage = texture.EncodeToJPG();
            UnityEngine.Object.Destroy(texture);
        });
        try
        {
            m_writer_video.WriteInt32(encodedImage.Length);
            m_writer_video.WriteBytes(encodedImage);
            await m_writer_video.StoreAsync();
        }
        catch (Exception ex)
        {
            connected_video = false;
            m_writeInProgress_video = false;
            m_writer_video.Dispose();
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        }
        m_writeInProgress_video = false;


        //int imageHeight = rGBImageFrame.Height;
        //int imageWidth = rGBImageFrame.Width;
        //byte[] imageData = rGBImageFrame.RgbImageByte;
        //int pixelStride = 4;
        //int rowStride = imageWidth * pixelStride;
        //try
        //{
        //    m_writer_video.WriteInt32(imageWidth);
        //    m_writer_video.WriteInt32(imageHeight);
        //    m_writer_video.WriteInt32(pixelStride);
        //    m_writer_video.WriteInt32(rowStride);
        //    m_writer_video.WriteBytes(imageData);
        //    await m_writer_video.StoreAsync();
        //}
        //catch (Exception ex)
        //{
        //    connected_video = false;
        //    m_writeInProgress_video = false;
        //    m_writer_video.Dispose();
        //    SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
        //    Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        //}
        //m_writeInProgress_video = false;
    }

    private void OnConnectionReceived_depth(StreamSocketListener streamSocketListener, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        if (m_streamSocket_depth != null) m_streamSocket_depth.Dispose();
        m_streamSocket_depth = args.Socket;
        m_writer_depth = new DataWriter(m_streamSocket_depth.OutputStream);
        m_writer_depth.UnicodeEncoding = UnicodeEncoding.Utf8;
        m_writer_depth.ByteOrder = ByteOrder.LittleEndian;
        m_reader_depth = new DataReader(m_streamSocket_depth.InputStream);

        connected_depth = true;
        Debug.Log("Depth connected");

    }
    private void OnConnectionReceived_rgb(StreamSocketListener streamSocketListener, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        if (m_streamSocket_rgb != null) m_streamSocket_rgb.Dispose();
        m_streamSocket_rgb = args.Socket;
        m_writer_rgb = new DataWriter(m_streamSocket_rgb.OutputStream);
        m_writer_rgb.UnicodeEncoding = UnicodeEncoding.Utf8;
        m_writer_rgb.ByteOrder = ByteOrder.LittleEndian;
        m_reader_rgb = new DataReader(m_streamSocket_rgb.InputStream);

        connected_rgb = true;
        Debug.Log("RGB connected");
    }
    private async void OnConnectionReceived_command(StreamSocketListener streamSocketListener, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        if (m_streamSocket_command != null) m_streamSocket_command.Dispose();
        m_streamSocket_command = args.Socket;
        m_writer_command = new DataWriter(m_streamSocket_command.OutputStream);
        m_writer_command.UnicodeEncoding = UnicodeEncoding.Utf8;
        m_writer_command.ByteOrder = ByteOrder.LittleEndian;
        m_reader_command = new DataReader(m_streamSocket_command.InputStream);
        //m_main_dataReader.InputStreamOptions = Windows.Storage.Streams.InputStreamOptions.
        connected_rgb = true;
        Debug.Log("Command connected");
        try
        {
            while (connected_rgb)
            {
                uint sizeOfInt = sizeof(int);
                var byteLoaded = await m_reader_command.LoadAsync(sizeOfInt);
                int numOfLines = m_reader_command.ReadInt32();
                if (numOfLines > 0)
                {
                    for (int i = 0; i < numOfLines; i++)
                    {
                        byteLoaded = await m_reader_command.LoadAsync(sizeOfInt);
                        int colorInInt = m_reader_command.ReadInt32();
                        var r = (colorInInt >> 16) & 255;
                        var g = (colorInInt >> 8) & 255;
                        var b = colorInInt & 255;
                        Color lineColor = new Color(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
                        byteLoaded = await m_reader_command.LoadAsync(sizeOfInt);
                        int floatArraySize = m_reader_command.ReadInt32();
                        uint floatArrayByteSize = (uint)(sizeof(float) * floatArraySize);
                        uint floatSize = (uint)sizeof(float);
                        List<float> floatList = new List<float>();
                        for (uint i2 = 0; i2 < floatArrayByteSize; i2 += floatSize)
                        {
                            byteLoaded = await m_reader_command.LoadAsync(floatSize);
                            float element = m_reader_command.ReadSingle();
                            floatList.Add(element);
                        }
                        //dataHelper.SetLineInScene(floatList.ToArray());
                        dataHelper.SetLineInScene(floatList.ToArray(), lineColor);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connected_rgb = false;
        }

    }
    private async void OnConnectionReceived_image(StreamSocketListener streamSocketListener, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        if (m_streamSocket_image != null) m_streamSocket_image.Dispose();
        m_streamSocket_image = args.Socket;
        m_writer_image = new DataWriter(m_streamSocket_image.OutputStream);
        m_writer_image.UnicodeEncoding = UnicodeEncoding.Utf8;
        m_writer_image.ByteOrder = ByteOrder.LittleEndian;
        m_reader_image = new DataReader(m_streamSocket_image.InputStream);
        connected_image = true;
        Debug.Log("image connected");
        ThreadUtils threadUtils = ThreadUtils.Instance;
        try
        {
            while (connected_image)
            {
                uint sizeOfInt = sizeof(int);
                var byteLoaded = await m_reader_image.LoadAsync(sizeOfInt);
                if (byteLoaded > 0)
                {
                    int payloadedByte = m_reader_image.ReadInt32();
                    byteLoaded = await m_reader_image.LoadAsync((uint)payloadedByte);
                    byte[] imageBuffer = new byte[payloadedByte];

                    m_reader_image.ReadBytes(imageBuffer);
                    threadUtils.InvokeOnMainThread(() =>
                    {
                        remoteImage.AddRemoteImage(imageBuffer);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connected_image = false;
        }

    }
    //private async void StartConnection()
    //{
    //    if (m_streamSocket_rgb != null) m_streamSocket_rgb.Dispose();
    //    if (m_streamSocket_depth != null) m_streamSocket_depth.Dispose();

    //    connectionStatTMP.text = "Connecting to " + hostIPAddress;

    //    try
    //    {
    //        var hostName = new Windows.Networking.HostName(hostIPAddress);
    //        m_streamSocket_depth = new StreamSocket();
    //        m_streamSocket_rgb = new StreamSocket();

    //        await m_streamSocket_depth.ConnectAsync(hostName, port_depth);
    //        await m_streamSocket_rgb.ConnectAsync(hostName, port_rgb);

    //        m_writer_depth = new DataWriter(m_streamSocket_depth.OutputStream);
    //        m_writer_depth.UnicodeEncoding = UnicodeEncoding.Utf8;
    //        m_writer_depth.ByteOrder = ByteOrder.LittleEndian;
    //        m_reader_depth = new DataReader(m_streamSocket_depth.InputStream);

    //        m_writer_rgb = new DataWriter(m_streamSocket_rgb.OutputStream);
    //        m_writer_rgb.UnicodeEncoding = UnicodeEncoding.Utf8;
    //        m_writer_rgb.ByteOrder = ByteOrder.LittleEndian;
    //        m_reader_rgb = new DataReader(m_streamSocket_rgb.InputStream);
    //        connected_depth = true;
    //        connected_rgb = true;
    //        connectionStatTMP.text = "Connected!";
    //    }
    //    catch (Exception ex)
    //    {
    //        SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
    //        Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
    //        connectionStatTMP.text = "Error!";
    //        debuggingTMP.text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
    //        StopConnection();
    //    }
    //}
    //private void StopConnection()
    //{
    //    m_writer_depth?.DetachStream();
    //    m_writer_depth?.Dispose();
    //    m_writer_depth = null;

    //    m_reader_depth?.DetachStream();
    //    m_reader_depth?.Dispose();
    //    m_reader_depth = null;

    //    m_streamSocket_depth?.Dispose();
    //    connected_depth = false;

    //    m_writer_rgb?.DetachStream();
    //    m_writer_rgb?.Dispose();
    //    m_writer_rgb = null;

    //    m_reader_rgb?.DetachStream();
    //    m_reader_rgb?.Dispose();
    //    m_reader_rgb = null;

    //    m_streamSocket_rgb?.Dispose();
    //    connected_rgb = false;
    //}

    private async void m_SendRGBImage(RGBImageFrame rgbImageFrame)
    {
        if (m_writeInProgress_rgb) return;
        int imageWidth = rgbImageFrame.Width,
            imageHeight = rgbImageFrame.Height,
            pixelStride = 4;
        int rowStride = imageWidth * pixelStride;

        m_writeInProgress_rgb = true;
        try
        {
            m_writer_rgb.WriteInt32(imageWidth);
            m_writer_rgb.WriteInt32(imageHeight);
            m_writer_rgb.WriteInt32(pixelStride);
            m_writer_rgb.WriteInt32(rowStride);
            foreach (float element in rgbImageFrame.ProjectionMat)
            {
                m_writer_rgb.WriteSingle(element);
            }
            foreach (float element in rgbImageFrame.CamToWorldMat)
            {
                m_writer_rgb.WriteSingle(element);
            }
            m_writer_rgb.WriteBytes(rgbImageFrame.RgbImageByte);
            m_writer_rgb.StoreAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            m_writeInProgress_rgb = false;
        }
        m_writeInProgress_rgb = false;

    }

    private async void m_SendDepthImage(ushort[] depthBuffer)
    {
        if (m_writeInProgress_depth) return;

        int imageWidth = 512,
            imageHeight = 512,
            pixelStride = 2;
        int rowStride = imageWidth * pixelStride;


        m_writeInProgress_depth = true;
        try
        {
            m_writer_depth.WriteInt32(imageWidth);
            m_writer_depth.WriteInt32(imageHeight);
            m_writer_depth.WriteInt32(pixelStride);
            m_writer_depth.WriteInt32(rowStride);
            m_writer_depth.WriteBytes(UINT16ToBytes(depthBuffer));
            m_writer_depth.StoreAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            m_writeInProgress_depth = false;
        }
        m_writeInProgress_depth = false;
    }

    private async void m_SendCloudPoint(float[] pointCloudXYZ)
    {
        if (m_writeInProgress_depth) return;
        if (pointCloudXYZ == null) return;
        m_writeInProgress_depth = true;
        int pointCloudLength = pointCloudXYZ.Length;
        try
        {
            m_writer_depth.WriteInt32(pointCloudLength);
            foreach (float pc in pointCloudXYZ)
            {
                m_writer_depth.WriteSingle(pc);
            }
            m_writer_depth.StoreAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            m_writeInProgress_depth = false;
        }
        m_writeInProgress_depth = false;
    }


#endif
    private byte[] UINT16ToBytes(ushort[] data)
    {
        byte[] ushortInBytes = new byte[data.Length * sizeof(ushort)];
        System.Buffer.BlockCopy(data, 0, ushortInBytes, 0, ushortInBytes.Length);
        return ushortInBytes;
    }

    public void SendRGBImage(RGBImageFrame rgbImageFrame)
    {
        if (connected_rgb)
        {
#if WINDOWS_UWP
            m_SendRGBImage(rgbImageFrame);
            Debug.Log("Sending RGB Image");
#endif
        }
    }
    public void SendDepthImage(ushort[] depthBuffer)
    {
        if(connected_depth)
        {
#if WINDOWS_UWP
            m_SendDepthImage(depthBuffer);
#endif
        }
    }
    public void SendPointCloud(float[] pointCloudXYZ)
    {
        if (connected_depth)
        {
#if WINDOWS_UWP
            m_SendCloudPoint(pointCloudXYZ);
            Debug.Log("Sending PC");
#endif
        }
    }
    public bool isVideoChannelConnected()
    {
        return connected_video;
    }
    public bool isVideoChannelBusy()
    {
        return m_writeInProgress_video;
    }
}

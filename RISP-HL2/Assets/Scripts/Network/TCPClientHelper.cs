﻿using System;
using UnityEngine;
//using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
#if WINDOWS_UWP
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

public class TCPClientHelper : MonoBehaviour
{
    #region Unity Functions

    private void Awake()
    {
        //ConnectionStatusLED.material.color = Color.red;
        connectionStatTMP.text = "Disconnected.";
    }
    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
#if WINDOWS_UWP
            StopCoonection();
#endif
        }
    }
    #endregion // Unity Functions

    [SerializeField]
    public string hostIPAddress;
    private string port = "9090";
    //public Renderer ConnectionStatusLED;
    public TextMeshPro connectionStatTMP;
    private TextMeshPro debuggingTMP;
    private bool connected = false;
    public bool Connected
    {
        get { return connected; }
    }
    public void SetConnectionStatTMP (TextMeshPro newConnectionTMP)
    {
        this.connectionStatTMP = newConnectionTMP;
    }
    public void SetDebuggingTMP (TextMeshPro newDebuggingTMP)
    {
        this.debuggingTMP = newDebuggingTMP;
    }

#if WINDOWS_UWP
    StreamSocket socket = null;
    public DataWriter dw;
    public DataReader dr;
    private async void StartCoonection()
    {
        if (socket != null) socket.Dispose();

        try
        {
            connectionStatTMP.text = "Connecting to "+hostIPAddress;
            socket = new StreamSocket();
            var hostName = new Windows.Networking.HostName(hostIPAddress);
            await socket.ConnectAsync(hostName, port);
            dw = new DataWriter(socket.OutputStream);
            dr = new DataReader(socket.InputStream);
            dr.InputStreamOptions = InputStreamOptions.Partial;
            connected = true;
            connectionStatTMP.text = "Connected!";
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connectionStatTMP.text = "Error!";
            debuggingTMP.text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
        }
    }

    private void StopCoonection()
    {
        dw?.DetachStream();
        dw?.Dispose();
        dw = null;

        dr?.DetachStream();
        dr?.Dispose();
        dr = null;

        socket?.Dispose();
        connected = false;
        connectionStatTMP.text = "Disconnected.";
    }

    bool lastMessageSent = true;
    public async void SendUINT16Async(ushort[] data)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("d"); // header "s" 

            // Write point cloud
            dw.WriteInt32(data.Length);
            dw.WriteBytes(UINT16ToBytes(data));

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connectionStatTMP.text = "Error on sendding message!";
            debuggingTMP.text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
        }
        lastMessageSent = true;
    }

    public async void SendUINT16Async(ushort[] data1, ushort[] data2)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("s"); // header "s" stands for it is ushort array (uint16)

            // Write Length
            dw.WriteInt32(data1.Length + data2.Length);

            // Write actual data
            dw.WriteBytes(UINT16ToBytes(data1));
            dw.WriteBytes(UINT16ToBytes(data2));

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connectionStatTMP.text = "Error on sendding message!";
            debuggingTMP.text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
        }
        lastMessageSent = true;
    }

    public async void SendSpatialImageAsync(byte[] LFImage, byte[] RFImage, long ts_left, long ts_right)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("f"); // header "f"

            // Write Length
            dw.WriteInt32(LFImage.Length + RFImage.Length);
            dw.WriteInt64(ts_left);
            dw.WriteInt64(ts_right);

            // Write actual data
            dw.WriteBytes(LFImage);
            dw.WriteBytes(RFImage);

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connectionStatTMP.text = "Error on sendding message!";
            debuggingTMP.text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
        }
        lastMessageSent = true;
    }
    public async void SendRGBImageAsync(byte[] rgbImage, int width, int height)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("r"); // header "f"

            dw.WriteInt32(width);
            dw.WriteInt32(height);
            // Write Length
            dw.WriteInt32(rgbImage.Length);

            // Write actual data
            dw.WriteBytes(rgbImage);

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connectionStatTMP.text = "Error on sendding message!";
            debuggingTMP.text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
        }
        lastMessageSent = true;
    }

    public async void SendRGBDAsync(ushort[] data, byte[] rgbImage, int width, int height)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("d"); // header "s" 

            // Write point cloud
            dw.WriteInt32(data.Length*2);
            dw.WriteBytes(UINT16ToBytes(data));

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();

            // Write header
            dw.WriteString("r"); // header "f"

            dw.WriteInt32(width);
            dw.WriteInt32(height);
            // Write Length
            dw.WriteInt32(rgbImage.Length);

            // Write actual data
            dw.WriteBytes(rgbImage);

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connectionStatTMP.text = "Error on sendding message!";
            debuggingTMP.text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
        }
        lastMessageSent = true;
    }

    public async void SendSpatialImageAsync(byte[] LRFImage, long ts_left, long ts_right)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("f"); // header "f"

            // Write Timestamp and Length
            dw.WriteInt32(LRFImage.Length);
            dw.WriteInt64(ts_left);
            dw.WriteInt64(ts_right);

            // Write actual data
            dw.WriteBytes(LRFImage);

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            connectionStatTMP.text = "Error on sendding message!";
            debuggingTMP.text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
        }
        lastMessageSent = true;
    }

#endif


    #region Helper Function
    byte[] UINT16ToBytes(ushort[] data)
    {
        byte[] ushortInBytes = new byte[data.Length * sizeof(ushort)];
        System.Buffer.BlockCopy(data, 0, ushortInBytes, 0, ushortInBytes.Length);
        return ushortInBytes;
    }
    #endregion

    #region Button Callback
    public void ConnectToServerEvent()
    {
#if WINDOWS_UWP
        if (!connected) StartCoonection();
        else StopCoonection();
#endif
    }
    #endregion
}

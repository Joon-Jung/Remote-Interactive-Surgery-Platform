using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public static class RtpPacket
{
    public static void WriteHeader(byte[] rtpPacket
        , int rtpVersion
        , int rtpPadding
        , int rtpExtension
        , int rtpSrcCount
        , int rtpMarker
        , int rtpPayload)
    {
        rtpPacket[0] = (byte)((rtpVersion << 6) | (rtpPadding << 5) | (rtpExtension << 4) | rtpSrcCount);
        rtpPacket[1] = (byte)((rtpMarker << 7) | (rtpPayload & 0x7F));
    }

    public static void WriteSequenceNumber(byte[] rtpPacket, uint emptySeqId)
    {
        rtpPacket[2] = ((byte)((emptySeqId >> 8) & 0xFF));
        rtpPacket[3] = ((byte)((emptySeqId >> 0) & 0xFF));
    }

    public static void WriteTS(byte[] rtpPacket, uint ts)
    {
        rtpPacket[4] = ((byte)((ts >> 24) & 0xFF));
        rtpPacket[5] = ((byte)((ts >> 16) & 0xFF));
        rtpPacket[6] = ((byte)((ts >> 8) & 0xFF));
        rtpPacket[7] = ((byte)((ts >> 0) & 0xFF));
    }

    public static void WriteSSRC(byte[] rtpPacket, uint ssrc)
    {
        rtpPacket[8] = ((byte)((ssrc >> 24) & 0xFF));
        rtpPacket[9] = ((byte)((ssrc >> 16) & 0xFF));
        rtpPacket[10] = ((byte)((ssrc >> 8) & 0xFF));
        rtpPacket[11] = ((byte)((ssrc >> 0) & 0xFF));
    }
}

public class AudioStreamer : MonoBehaviour
{
    // Audio control variables
    AudioClip mic;
    int lastPos, pos;

    // UDP Socket variables
    private Socket socket;
    private IPEndPoint RemoteEndPoint;
    private UInt32 sequenecId = 0;

    void SetRtpHeader(byte[] rtpPacket)
    {
        // Populate RTP Packet Header
        // 0  - Version, P, X, CC, M, PT and Sequence Number
        // 32 - Timestamp. H264 uses a 90kHz clock
        // 64 - SSRC
        // 96 - CSRCs (optional)
        // nn - Extension ID and Length
        // nn - Extension header
        RtpPacket.WriteHeader(rtpPacket
            , 2    // version
            , 0    // padding
            , 0    // extension
            , 0    // csrc_count
            , 1    // marker, set to one for last packet
            , 11); // payload_type PCM 16bits BE signed
        RtpPacket.WriteSequenceNumber(rtpPacket, sequenecId);
        RtpPacket.WriteTS(rtpPacket, Convert.ToUInt32(DateTime.Now.Millisecond * 90));
        RtpPacket.WriteSSRC(rtpPacket, 0);
        sequenecId++;
    }

    void SendToServer(float[] samples)
    {
        const int RTP_HEADER_LEN = 12;
        if (socket == null) return;
        if (samples == null || samples.Length == 0) return;

        // Convert audio from float to signed 16 bit PCM BigEndian and copy it to the byte array
        var byteArray = new byte[samples.Length * sizeof(Int16)]; // to convert each sample float to Int16
        int i = 0;
        int j = 0;
        while (i < samples.Length)
        {
            Int16 sample = Convert.ToInt16((samples[i] * Int16.MaxValue) / 100);
            byteArray[j] = (byte)(sample & 0xFF);
            byteArray[j + 1] = (byte)((sample >> 8) & 0xFF);
            i = i + 1;
            j = j + 2;
        }

        var dataToSend = byteArray.Length;
        int maxEthMTU = 1400;
        int offset = 0;
        while (dataToSend > 0)
        {
            var bodyLen = Math.Min(dataToSend, maxEthMTU);
            var rtpAudioData = new byte[RTP_HEADER_LEN + bodyLen];
            SetRtpHeader(rtpAudioData);
            System.Array.Copy(byteArray, offset, rtpAudioData, RTP_HEADER_LEN, bodyLen);
            int dataSent = socket.SendTo(rtpAudioData, 0, rtpAudioData.Length, SocketFlags.None, RemoteEndPoint);
            dataToSend = dataToSend - dataSent;
            offset = offset + dataSent;
        }
    }

    void Start()
    {
        RemoteEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 8080);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        mic = Microphone.Start(null, true, 1, 44100); // Mono
    }

    private void Update()
    {
        if ((pos = Microphone.GetPosition(null)) > 0)
        {
            if (lastPos > pos) lastPos = 0;

            if (pos - lastPos > 0)
            {
                // Allocate the space for the new sample.
                int len = (pos - lastPos) * mic.channels;
                float[] samples = new float[len];
                mic.GetData(samples, lastPos);
                SendToServer(samples);
                lastPos = pos;
            }
        }
    }

    void OnDestroy()
    {
        Microphone.End(null);
    }
}
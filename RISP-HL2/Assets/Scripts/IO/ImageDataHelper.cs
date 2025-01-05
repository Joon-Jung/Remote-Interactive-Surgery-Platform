using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading;
using System.Linq;
public class ImageDataHelper : MonoBehaviour
{
    public ResearchModeHelper researchModeHelper;
    public VideoPanelHelper videoPanelHelper;
    public TCPClientHelperFour tcpClientHelperFour;

    public async void TransmitImages()
    {
#if WINDOWS_UWP
        RGBImageFrame latestRGBImageFrame = new RGBImageFrame (null, null, null, 0, 0);
        ushort[] latestDepthImage = null;
        Thread thread0 = new Thread(() => latestRGBImageFrame = videoPanelHelper.GetLatestRGBImage());
        Thread thread1 = new Thread(() => latestDepthImage = researchModeHelper.GetLatestAHATSensorData());
        thread0.Start();
        thread1.Start();
        thread0.Join();
        thread1.Join();
        //float[] mat;
        //researchModeHelper.GetLatestDepthWithMatrix(out mat);
        //tcpClientHelper.SendUINT16Async(latestDepthImage);
        //tcpClientHelper.SendRGBImageAsync(latestRGBImageFrame.RgbImageByte, latestRGBImageFrame.Width, latestRGBImageFrame.Height);

        //For debuging perpose.
        //Texture2D texture2D = new Texture2D(latestRGBImageFrame.Width, latestRGBImageFrame.Height, TextureFormat.BGRA32, false);
        //texture2D.LoadRawTextureData(latestRGBImageFrame.RgbImageByte);
        //texture2D.Apply();
        //byte[] itemBGBytes = texture2D.EncodeToPNG();
        //File.WriteAllBytes(Application.persistentDataPath + "\\" + "rgb.png", itemBGBytes);


        //tcpClientHelper.SendRGBDAsync(latestDepthImage, latestRGBImageFrame.RgbImageByte, latestRGBImageFrame.Width, latestRGBImageFrame.Height);
        tcpClientHelperFour.SendDepthImage(latestDepthImage);
        tcpClientHelperFour.SendRGBImage(latestRGBImageFrame);
#endif
    }

}

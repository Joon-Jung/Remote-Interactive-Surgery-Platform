using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
public class DataHelper : MonoBehaviour
{
    public ResearchModeVideoStreamHelper researchModeHelper;
    public VideoPanelHelper videoPanelHelper;
    public TCPClientHelperFour tcpClientHelper;
    public GameObject lineObject;
    public LineHelper lineHelper;

    public async void TransmitImages()
    {
        Debug.Log("Transmit image triggered.");
#if WINDOWS_UWP
        RGBImageFrame latestRGBImageFrame = new RGBImageFrame (null, null, null, 0, 0);
        float[] pointCloud = null;
        Thread thread0 = new Thread(() => latestRGBImageFrame = videoPanelHelper.GetLatestRGBImage());
        Thread thread1 = new Thread(() => pointCloud = researchModeHelper.GetPointCloud());
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
        //tcpClientHelperTwo.SendDepthImage(latestDepthImage);
        //float[] projectionMatrix = latestRGBImageFrame.ProjectionMat;
        //string debug_mat = "";
        //foreach (float element in projectionMatrix)
        //{
        //    debug_mat += element.ToString() + ", ";
        //}
        //Debug.Log(debug_mat);
        //float[] camToWorldMatrix = latestRGBImageFrame.CamToWorldMat;
        //debug_mat = "";
        //foreach(float element in camToWorldMatrix)
        //{
        //    debug_mat += element.ToString() + ", ";
        //}
        //Debug.Log(debug_mat);
        tcpClientHelper.SendRGBImage(latestRGBImageFrame);
        tcpClientHelper.SendPointCloud(pointCloud);
#endif
    }
    public void SetLineInScene(Vector3[] points)
    {
        LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
        lineRenderer.SetPositions(points);
        Debug.Log("Line set");
    }
    public void SetLineInScene(float[] points)
    {
        ThreadUtils threadUtils = ThreadUtils.Instance;
        Debug.Log(points.ToString());
        threadUtils.InvokeOnMainThread(() =>
        {
            int numberOfVector = points.Length / 3;
            Debug.Log("number of received vector - " + numberOfVector.ToString());
            Vector3[] pointsArray = new Vector3[numberOfVector];

            for (int i = 0; i < numberOfVector; i++)
            {
                int index = i * 3;
                Vector3 vector3 = new Vector3(points[index], points[index + 1], points[index + 2]);
                pointsArray[i] = vector3;
                //Debug.Log(i.ToString() + " - " + vector3.ToString());
            }
            LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = pointsArray.Length;
            lineRenderer.SetPositions(pointsArray);
            lineRenderer.startWidth = 0.002f;
            lineRenderer.endWidth = lineRenderer.startWidth;
            Debug.Log("Line set - "+lineRenderer.positionCount.ToString());
        });
    }
    public void SetLineInScene(float[] points, Color lineColor)
    {
        ThreadUtils threadUtils = ThreadUtils.Instance;
        int numberOfVector = points.Length / 3;
        Debug.Log("number of received vector - " + numberOfVector.ToString());
        Vector3[] pointsArray = new Vector3[numberOfVector];
        for (int i = 0; i < numberOfVector; i++)
        {
            int index = i * 3;
            Vector3 vector3 = new Vector3(points[index], points[index + 1], points[index + 2]);
            pointsArray[i] = vector3;
            //Debug.Log(i.ToString() + " - " + vector3.ToString());
        }
        threadUtils.InvokeOnMainThread(() =>
        {
            lineHelper.AddNewLine(pointsArray, lineColor);
        });
    }
    public void StartVideoTransmission()
    {
        VideoTransmission();
    }
    private async void VideoTransmission()
    {
        //ThreadUtils threadUtils = ThreadUtils.Instance;
        int delayInLoopMS = 1000 / 30;
        bool isVideoChannelOpened = tcpClientHelper.isVideoChannelConnected();
        while (isVideoChannelOpened)
        {
            isVideoChannelOpened = tcpClientHelper.isVideoChannelConnected();
#if WINDOWS_UWP
            RGBImageFrame latestRGBImageFrame = videoPanelHelper.GetLatestRGBImage();
            if (!tcpClientHelper.isVideoChannelBusy()) tcpClientHelper.TransmittingVideoFrame(latestRGBImageFrame);
#endif
            await Task.Delay(delayInLoopMS);
        }
    }
}

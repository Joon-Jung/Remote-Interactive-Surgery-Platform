using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GameObject))]
public class RemoteImageReceiver : MonoBehaviour
{
    public GameObject remoteImageViewerPerfab;
    public Texture2D testImage;
    //// Start is called before the first frame update
    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    public void AddRemoteImage(byte[] imageBuffer)
    {
        GameObject newRemoteImageViewer = Instantiate(remoteImageViewerPerfab);
        Transform remoteImageViewerTrans = newRemoteImageViewer.transform;

        Camera mainCam = Camera.main;
        Transform mainCamTrans = mainCam.transform;
        Vector3 forward = mainCamTrans.forward.normalized;
        Vector3 location = mainCamTrans.position;
        Vector3 viewerLocation = location + forward * 0.6f;
        remoteImageViewerTrans.position = viewerLocation;
        remoteImageViewerTrans.rotation = Quaternion.LookRotation(forward);

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageBuffer);
        int imageWidth = tex.width;
        int imageHeight = tex.height;
        float widthRatio = (float)imageWidth / imageHeight;

        Transform contentQuadTrans = newRemoteImageViewer.transform.Find("ContentQuad");
        float heigthScale = contentQuadTrans.localScale.y;
        float widthScale = contentQuadTrans.localScale.x;
        float newWidthScale = heigthScale * widthRatio;
        float widthMultiplier = newWidthScale / widthScale;

        Vector3 localScale = remoteImageViewerTrans.localScale;
        localScale.x = localScale.x * widthMultiplier;
        remoteImageViewerTrans.localScale = localScale;

        GameObject imageObject = contentQuadTrans.Find("Image").gameObject;
        MeshRenderer meshRenderer = imageObject.GetComponent<MeshRenderer>();
        Material material = Instantiate(meshRenderer.sharedMaterial);
        meshRenderer.sharedMaterial = material;
        material.mainTexture = tex;
    }
    public void AddRemoteImage(Texture2D tex)
    {
        GameObject newRemoteImageViewer = Instantiate(remoteImageViewerPerfab);
        Transform remoteImageViewerTrans = newRemoteImageViewer.transform;

        Camera mainCam = Camera.main;
        Transform mainCamTrans = mainCam.transform;
        Vector3 forward = mainCamTrans.forward.normalized;
        Vector3 location = mainCamTrans.position;
        Vector3 viewerLocation = location + forward * 0.6f;
        remoteImageViewerTrans.position = viewerLocation;
        remoteImageViewerTrans.rotation = Quaternion.LookRotation(forward);

        int imageWidth = tex.width;
        int imageHeight = tex.height;
        float widthRatio = (float)imageWidth / imageHeight;

        Transform contentQuadTrans = newRemoteImageViewer.transform.Find("ContentQuad");
        float heigthScale = contentQuadTrans.localScale.y;
        float widthScale = contentQuadTrans.localScale.x;
        float newWidthScale = heigthScale * widthRatio;
        float widthMultiplier = newWidthScale / widthScale;


        Vector3 localScale = remoteImageViewerTrans.localScale;
        localScale.x = localScale.x * widthMultiplier;
        remoteImageViewerTrans.localScale = localScale;

        GameObject imageObject = contentQuadTrans.Find("Image").gameObject;
        MeshRenderer meshRenderer = imageObject.GetComponent<MeshRenderer>();
        Material material = Instantiate(meshRenderer.sharedMaterial);
        meshRenderer.sharedMaterial = material;
        material.mainTexture = tex;
    }
}

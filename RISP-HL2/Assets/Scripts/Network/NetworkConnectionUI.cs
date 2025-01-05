using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NetworkConnectionUI : MonoBehaviour
{
    public TextMeshPro ipAddrTMP;
    public TextMeshPro connectionStatTMP;
    public TextMeshPro errorTMP;
    public TCPClientHelper tCPClient;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ClickOne()
    {
        ipAddrTMP.text += 1;
    }
    public void ClickTwo()
    {
        ipAddrTMP.text += 2;
    }
    public void ClickThree()
    {
        ipAddrTMP.text += 3;
    }
    public void ClickFour()
    {
        ipAddrTMP.text += 4;
    }
    public void ClickFive()
    {
        ipAddrTMP.text += 5;
    }
    public void ClickSix()
    {
        ipAddrTMP.text += 6;
    }
    public void ClickSeven()
    {
        ipAddrTMP.text += 7;
    }
    public void ClickEigth()
    {
        ipAddrTMP.text += 8;
    }
    public void ClickNine()
    {
        ipAddrTMP.text += 9;
    }
    public void ClickTen()
    {
        ipAddrTMP.text += 0;
    }
    public void ClickDot()
    {
        ipAddrTMP.text += ".";
    }
    public void ClickBackspace()
    {
        string text = ipAddrTMP.text;
        if (text.Length > 0)
        {
            text = text.Remove(text.Length - 1);
            ipAddrTMP.text = text;
        }
    }
    public void ClickReset()
    {
        ipAddrTMP.text ="";
    }
    public void ClickConnect()
    {
        tCPClient.hostIPAddress = ipAddrTMP.text;
        tCPClient.ConnectToServerEvent();
        connectionStatTMP.text = "Connecting...";
    }
}

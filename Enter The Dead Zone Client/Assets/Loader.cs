using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

using DZNetwork;
using System;

public class Loader
{
    public static DZClient Socket = new DZClient();

    public static string ServerIP = "192.168.2.26"; //"172.16.6.165";//"192.168.2.51"; //"192.168.2.26"; //"172.16.6.165";
    public static int ServerPort = 26950;

    public static InputField IPAddressField;
    public static Text StatusText;

    [RuntimeInitializeOnLoadMethod]
    private static void Start()
    {
        Physics2D.queriesStartInColliders = false;
        Application.quitting += Dispose;
        Time.fixedDeltaTime = 1f / Game.ClientTickRate;
        Physics2D.simulationMode = SimulationMode2D.Script;

        IPAddressField = GameObject.FindGameObjectWithTag("IPInput").GetComponent<InputField>();
        IPAddressField.text = ServerIP + ":" + ServerPort;
        IPAddressField.onValueChanged.AddListener(delegate { Connect(); });
        StatusText = GameObject.FindGameObjectWithTag("StatusBox").GetComponent<Text>();
        StatusText.text = "";

        //Remove later
        Socket.ConnectHandle += Game.Connected;
        Socket.DisconnectHandle += Game.Disconnected;
        Socket.PacketHandle += ServerHandle.ProcessPacket;
        Socket.PacketLostHandle += ServerHandle.HandleLostPacket;
        Socket.Connect(ServerIP, ServerPort);
    }

    private static void Connect()
    {
        string[] IPPort = IPAddressField.text.Split(':');
        if (IPPort.Length != 2)
        {
            StatusText.text = "Please enter IP in the correct format";
        }
        try
        {
            Socket.Connect(IPPort[0], int.Parse(IPPort[1]));
        }
        catch (Exception E)
        {
            StatusText.text = "Failed to parse IP";
        }
    }

    private static void Dispose()
    {
        Socket.Dispose();
    }
}

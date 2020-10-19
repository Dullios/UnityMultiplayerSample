using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using System.Collections;
using NetworkObjects;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_Connections;

    public List<NetworkObjects.NetworkPlayer> clientList = new List<NetworkObjects.NetworkPlayer>();
    public List<CubeDetails> cubeDetList = new List<CubeDetails>();

    public GameObject cubePrefab;

    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        cubePrefab = (GameObject)Resources.Load("Prefabs/Cube");

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        Debug.Log("Server Started");

        StartCoroutine(SendHandshakeToAllClient());
    }

    IEnumerator SendHandshakeToAllClient()
    {
        while(true)
        {
            for(int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                    continue;

                HandshakeMsg m = new HandshakeMsg();
                m.player.id = m_Connections[i].InternalId.ToString();
                SendToClient(JsonUtility.ToJson(m), m_Connections[i]);
            }
            yield return new WaitForSeconds(2);
        }
    }

    void SendToClient(string message, NetworkConnection c){
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
        clientList.Clear();
    }

    void OnConnect(NetworkConnection c){
        m_Connections.Add(c);
        Debug.Log("Accepted a connection");

        NetworkObjects.NetworkPlayer p = new NetworkObjects.NetworkPlayer();
        p.id = c.InternalId.ToString();
        clientList.Add(p);

        CubeDetails cDet = new CubeDetails(c.InternalId);
        cDet.cube = GameObject.Instantiate(cubePrefab, p.cubPos, Quaternion.identity);
        cDet.cube.GetComponent<MeshRenderer>().material.color = p.cubeColor;

        InitializeConnectionMsg icMsg = new InitializeConnectionMsg();
        icMsg.connectionID = c.InternalId.ToString();
        SendToClient(JsonUtility.ToJson(icMsg), c);

        ServerUpdateMsg sMsg = new ServerUpdateMsg();
        foreach(NetworkObjects.NetworkPlayer np in clientList)
        {
            sMsg.players.Add(np);
        }

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
                continue;

            SendToClient(JsonUtility.ToJson(sMsg), m_Connections[i]);
        }
    }

    void OnData(DataStreamReader stream, int i){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.cmd)
        {
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                Debug.Log("Handshake message received!");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                Debug.Log("Player update message received!");

                int index = 0;
                foreach(NetworkObjects.NetworkPlayer p in clientList)
                {
                    if (puMsg.player.id == p.id)
                        break;
                    index++;
                }
                clientList[index] = puMsg.player;

                ServerUpdateMsg sMsg = new ServerUpdateMsg();
                sMsg.players = clientList;
                for (int j = 0; j < m_Connections.Length; j++)
                {
                    if (!m_Connections[j].IsCreated)
                        continue;

                    SendToClient(JsonUtility.ToJson(sMsg), m_Connections[j]);
                }
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Server update message received!");
                break;
            case Commands.INITIALIZE:
                InitializeConnectionMsg icMsg = JsonUtility.FromJson<InitializeConnectionMsg>(recMsg);
                Debug.Log("Initialize Connection message received!");
                break;
            default:
                Debug.Log("SERVER ERROR: Unrecognized message received!");
                break;
        }
    }

    void OnDisconnect(int i){
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {

                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c  != default(NetworkConnection))
        {            
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }


        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);
            
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}
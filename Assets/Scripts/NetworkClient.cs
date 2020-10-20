using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    public int connectedID;
    public GameObject cubePrefab;
    public List<NetworkObjects.NetworkPlayer> playerList = new List<NetworkObjects.NetworkPlayer>();
    public List<NetworkCube> cubeList = new List<NetworkCube>();

    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        m_Connection = m_Driver.Connect(endpoint);
    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("We are now connected to the server");
        StartCoroutine(SendRepeatedHandshake());
    }

    IEnumerator SendRepeatedHandshake()
    {
        while(true)
        {
            yield return new WaitForSeconds(2);
            Debug.Log("Sending handshake");
            HandshakeMsg m = new HandshakeMsg();
            m.player.id = m_Connection.InternalId.ToString();
            SendToServer(JsonUtility.ToJson(m));
        }
    }

    public void SendPlayerUpdate()
    {
        int index = GetIndex(connectedID.ToString());
        playerList[index].cubePos = cubeList[index].cube.transform.position;

        PlayerUpdateMsg m = new PlayerUpdateMsg();
        m.player = playerList[index];
        SendToServer(JsonUtility.ToJson(m));
        Debug.Log("Sent player update");
    }

    void OnData(DataStreamReader stream){
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
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Server update message received!");

                if (playerList.Count == 0)
                {
                    playerList = suMsg.players;
                    foreach(NetworkObjects.NetworkPlayer p in playerList)
                    {
                        CreateCube(p);
                    }
                }
                else
                {
                    foreach (NetworkObjects.NetworkPlayer npServer in suMsg.players)
                    {
                        int ind = GetIndex(npServer.id);

                        if(ind >= playerList.Count)
                        {
                            playerList.Add(npServer);
                            CreateCube(npServer);
                        }
                        else
                        {
                            playerList[ind] = npServer;
                            cubeList[ind].cube.transform.position = npServer.cubePos;
                        }
                    }
                }
                break;
            case Commands.INITIALIZE:
                InitializeConnectionMsg icMsg = JsonUtility.FromJson<InitializeConnectionMsg>(recMsg);
                connectedID = int.Parse(icMsg.connectionID);
                Debug.Log("Initialize Connection message received!");
                Debug.Log("Connected ID of " + connectedID);
                break;
            case Commands.PLAYER_DISCONNECT:
                PlayerDisconnectMsg pdMsg = JsonUtility.FromJson<PlayerDisconnectMsg>(recMsg);
                Debug.Log("Player disconnect message received!");
                
                int index = GetIndex(pdMsg.connectionID);
                
                playerList.RemoveAt(index);
                Destroy(cubeList[index].cube);
                cubeList.RemoveAt(index);
                break;
            default:
                Debug.Log("Unrecognized message received!");
                break;
        }
    }

    void CreateCube(NetworkObjects.NetworkPlayer npClient)
    {
        NetworkCube nCube = new NetworkCube(npClient.id);
        nCube.cube = GameObject.Instantiate(cubePrefab, npClient.cubePos, Quaternion.identity);
        nCube.cube.GetComponent<CubeController>().id = int.Parse(npClient.id);
        nCube.cube.GetComponent<MeshRenderer>().material.color = npClient.cubeColor;
        nCube.cube.GetComponent<CubeController>().netClient = this;
        cubeList.Add(nCube);
    }

    int GetIndex(string id)
    {
        int index = 0;
        foreach(NetworkObjects.NetworkPlayer p in playerList)
        {
            if (p.id == id)
                break;

            index++;
        }

        return index;
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }   
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}
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
    public List<CubeDetails> cubeDetList = new List<CubeDetails>();

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
        StartCoroutine(SendRepeatedPlayerUpdate());
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

    IEnumerator SendRepeatedPlayerUpdate()
    {
        while(true)
        {
            yield return new WaitForSeconds(1.0f);
            Debug.Log("SendingPosition");
            PlayerUpdateMsg m = new PlayerUpdateMsg();
            foreach(NetworkObjects.NetworkPlayer p in playerList)
            {
                if(int.Parse(p.id) == connectedID)
                {
                    m.player = p;
                    SendToServer(JsonUtility.ToJson(m));
                    break;
                }
            }
        }
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

                foreach (NetworkObjects.NetworkPlayer npServer in suMsg.players)
                {
                    if (playerList.Count == 0)
                        playerList.Add(npServer);
                    else
                    {
                        foreach (NetworkObjects.NetworkPlayer npClient in playerList)
                        {
                            if (npClient.id == npServer.id)
                            {
                                break;
                            }

                            playerList.Add(npServer);
                            Debug.Log("NetworkPlayer Added");
                        }
                    }
                }

                UpdateCubes();
                break;
            case Commands.INITIALIZE:
                InitializeConnectionMsg icMsg = JsonUtility.FromJson<InitializeConnectionMsg>(recMsg);
                connectedID = int.Parse(icMsg.connectionID);
                Debug.Log("Initialize Connection message received!");
                Debug.Log("Connected ID of " + connectedID);
                break;
            default:
                Debug.Log("Unrecognized message received!");
                break;
        }
    }

    void UpdateCubes()
    {
        if(cubeDetList.Count == 0)
        {
            foreach(NetworkObjects.NetworkPlayer npClient in playerList)
            {
                CreateCube(npClient);
            }
        }
        else
        {
            bool hasCube = false;

            foreach (NetworkObjects.NetworkPlayer npClient in playerList)
            {
                foreach(CubeDetails cd in cubeDetList)
                {
                    if(cd.cubeID == int.Parse(npClient.id))
                    {
                        cd.cube.transform.position = npClient.cubPos;
                        hasCube = true;
                    }
                }

                if(!hasCube)
                {
                    CreateCube(npClient);
                }
            }
        }
    }

    void CreateCube(NetworkObjects.NetworkPlayer npClient)
    {
        CubeDetails cDet = new CubeDetails(int.Parse(npClient.id));
        cDet.cube = GameObject.Instantiate(cubePrefab, npClient.cubPos, Quaternion.identity);
        cDet.cube.GetComponent<MeshRenderer>().material.color = npClient.cubeColor;
        cDet.cube.GetComponent<CubeController>().netClient = this;

        cubeDetList.Add(cDet);
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
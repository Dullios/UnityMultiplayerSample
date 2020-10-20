using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands{
        PLAYER_UPDATE,
        SERVER_UPDATE,
        HANDSHAKE,
        PLAYER_INPUT,
        INITIALIZE,
        PLAYER_DISCONNECT
    }

    [System.Serializable]
    public class NetworkHeader{
        public Commands cmd;
    }

    [System.Serializable]
    public class HandshakeMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public HandshakeMsg(){      // Constructor
            cmd = Commands.HANDSHAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }
    
    [System.Serializable]
    public class PlayerUpdateMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public PlayerUpdateMsg(){      // Constructor
            cmd = Commands.PLAYER_UPDATE;
            player = new NetworkObjects.NetworkPlayer();
        }
    };

    public class PlayerInputMsg:NetworkHeader{
        public Input myInput;
        public PlayerInputMsg(){
            cmd = Commands.PLAYER_INPUT;
            myInput = new Input();
        }
    }
    [System.Serializable]
    public class  ServerUpdateMsg:NetworkHeader{
        public List<NetworkObjects.NetworkPlayer> players;
        public ServerUpdateMsg(){      // Constructor
            cmd = Commands.SERVER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }

    [System.Serializable]
    public class InitializeConnectionMsg:NetworkHeader
    {
        public string connectionID;
        public InitializeConnectionMsg()
        {
            cmd = Commands.INITIALIZE;
        }
    }

    [System.Serializable]
    public class PlayerDisconnectMsg:NetworkHeader
    {
        public string connectionID;
        public PlayerDisconnectMsg()
        {
            cmd = Commands.PLAYER_DISCONNECT;
        }
    }
} 

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject{
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject{
        public Vector3 cubePos;
        public Color cubeColor;

        public NetworkPlayer(){
            cubeColor = new Color();
            cubePos = new Vector3();
        }
    }

    [System.Serializable]
    public class NetworkCube : NetworkObject
    {
        public GameObject cube;
        public NetworkCube(string _id)
        {
            id = _id;
        }
    }
}

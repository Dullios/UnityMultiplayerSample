using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour
{
    public int id;
    public NetworkClient netClient;

    private Vector3 currentPos;
    private bool hasUpdated = false;

    // Update is called once per frame
    void Update()
    {
        currentPos = gameObject.transform.position;
        
        if(netClient != null && id == netClient.connectedID)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentPos.x--;
                hasUpdated = true;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentPos.x++;
                hasUpdated = true;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentPos.y--;
                hasUpdated = true;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentPos.y++;
                hasUpdated = true;
            }

            gameObject.transform.position = currentPos;
            if (hasUpdated)
            {
                netClient.SendPlayerUpdate();
                hasUpdated = false;
            }
        }
    }
}

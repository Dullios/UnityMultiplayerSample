using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour
{
    public int id;
    public NetworkClient netClient;

    private Vector3 currentPos;

    // Update is called once per frame
    void Update()
    {
        currentPos = gameObject.transform.position;

        if(netClient != null && id == netClient.connectedID)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentPos.x--;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentPos.x++;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentPos.y--;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentPos.y++;
            }

            gameObject.transform.position = currentPos;
        }
    }
}

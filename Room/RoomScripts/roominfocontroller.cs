using UnityEngine;

public class roominfocontroller : MonoBehaviour
{
    public GameObject roomcode_display;
    void Start()
    {
        roomcode_display.GetComponent<StringControl>().s = "Room:  " + RoomData.roomcode;
    }

    void Update()
    {

    }
}

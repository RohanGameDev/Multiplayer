using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;
public class RoomButton : MonoBehaviour
{
    public TMP_Text buttonText;
    private RoomInfo info;

    public void SetButtonDetails(RoomInfo inputinfo)
    {
        info = inputinfo;

        buttonText.text = info.Name;
    }

    public void OpenRoom()
    {
        Launcher.instance.JoinRoom(info);
    }
}

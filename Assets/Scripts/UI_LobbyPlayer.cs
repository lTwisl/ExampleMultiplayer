using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_LobbyPlayer : MonoBehaviour
{
    public TMP_Text TextName;

    public void SetName(string name)
    {
        TextName.text = name;
    }
}

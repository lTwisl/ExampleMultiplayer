using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private MyLobby _myLobby;

    [Space(20)]
    [SerializeField] private GameObject _createOrJoinLobby;
    [SerializeField] private GameObject _joinedLobby;

    [Space(20)]
    [SerializeField] private Button _btnCreate;
    [SerializeField] private Button _btnJoin;
    [SerializeField] private TMP_InputField _fieldPlayerName;
    [SerializeField] private TMP_InputField _fieldLobbyName;
    [SerializeField] private TMP_InputField _fieldCodeLobby;

    [Space(20)]
    [SerializeField] private Button _btnStartGame;
    [SerializeField] private TMP_InputField _fieldLobbyCode;

    [Space(20)]
    [SerializeField] private RectTransform _content;
    [SerializeField] private UI_LobbyPlayer _prefabPlayer;

    private string _playerName = string.Empty;
    private string _lobbyName = string.Empty;
    private string _codeForJoining = string.Empty;

    private void Awake()
    {
        _fieldPlayerName.onEndEdit.AddListener((string newPlayerName) => _playerName = newPlayerName);
        _fieldPlayerName.onEndEdit.AddListener((string newLobbyName) => _lobbyName = newLobbyName);
        _fieldCodeLobby.onEndEdit.AddListener((string codeLobby) => _codeForJoining = codeLobby);

        _btnCreate.onClick.AddListener(CreateLobby);
        _btnJoin.onClick.AddListener(JoinLobbyByCode);

        _btnStartGame.onClick.AddListener(StartGame);

        _myLobby.onJoined += () => _joinedLobby.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        UpdatePlayers();
    }

    private void UpdatePlayers()
    {
        List<Player> players = _myLobby.GetPlayers();

        if (players == null)
            return;

        int count = _content.transform.childCount;
        for (int i = count; i > 0; --i)
        {
            Destroy(_content.GetChild(i - 1).gameObject);
        }

        foreach (Player player in players)
        {
            UI_LobbyPlayer playerUI = Instantiate(_prefabPlayer);
            playerUI.transform.SetParent(_content);
            playerUI.SetName(player.Data["PlayerName"].Value);
        }
    }

    private async void CreateLobby()
    {
        if (_playerName == string.Empty || _lobbyName == string.Empty)
            return;

        _fieldLobbyCode.text = await _myLobby.CreateLobbyAsync(_playerName, _lobbyName, 4);
        _createOrJoinLobby.SetActive(false);
        _joinedLobby.SetActive(true);
    }

    private async void JoinLobbyByCode()
    {
        if (_playerName == string.Empty || _codeForJoining == string.Empty)
            return;

        bool successful = await _myLobby.JoinlobbyByCode(_playerName, _codeForJoining);
        if (successful == false)
            return;

        _createOrJoinLobby.SetActive(false);
        _joinedLobby.SetActive(true);
        _btnStartGame.gameObject.SetActive(false);
        _fieldLobbyCode.text = _codeForJoining;
    }

    private void StartGame()
    {
        _myLobby.StartGame();

        _joinedLobby.SetActive(false);
    }
}

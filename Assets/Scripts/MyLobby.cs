using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class MyLobby : MonoBehaviour
{
    public event Action onJoined;

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    private async void Start()
    {
        await UnityServices.InitializeAsync(); // Инициализация сервисов Unity.

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Вход под анонимной учётной записью.
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15.0f;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;

                if (joinedLobby.Data["StartGame"].Value != "0")
                {
                    if (joinedLobby.HostId != AuthenticationService.Instance.PlayerId)
                    {
                        Debug.Log("StartClient");

                        RelayManager.JoinRelay(joinedLobby.Data["StartGame"].Value);

                        onJoined?.Invoke();
                    }

                    joinedLobby = null;
                }
            }
        }
    }

    public async Task<string> CreateLobbyAsync(string playerName, string lobbyName, int maxPlayers)
    {
        try
        {
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions // Настройка лобби
            {
                IsPrivate = false,
                Player = GetPlayer(playerName),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "CaptureTheFlag") },
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, "de_dust2") },
                    { "StartGame", new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions); // Создание лобби.
            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log($"Create lobby! {lobby.Name} {lobby.MaxPlayers} {lobby.Id} {lobby.LobbyCode}");
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        return joinedLobby.LobbyCode;
    }

    private Player GetPlayer(string name)
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name) }
            }
        };
    }

    public void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }

    public static void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in lobby " + lobby.Name + " " + lobby.Data["GameMode"].Value + " " + lobby.Data["Map"].Value + ":");
        foreach (Player player in lobby.Players)
        {
            Debug.Log("--- " + player.Id + " " + player.Data["PlayerName"].Value);
        }
    }

    public static async void ListLobbies()
    {
        try
        {
            // Фильтры для запроса лобби.
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 5, // Получить только 5 лобби.
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT) // Выбрать только те лобби, в которых свободных мест больше 0.
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created) // Отсортировать лобби по времени создания.
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions); // Запрос на получения списка лобби.

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async Task<bool> JoinlobbyByCode(string playerName, string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer(playerName)
            };
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions); // Подключение к лобби по коду.
            joinedLobby = lobby;

            Debug.Log("Joined lobby with code " + lobby.LobbyCode + " " + lobby.Name);
            PrintPlayers(lobby);

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);

            return false;
        }
    }

    public async void QuickJoinLobby(string playerName)
    {
        try
        {
            QuickJoinLobbyOptions quickJoinLobbyOptions = new QuickJoinLobbyOptions
            {
                Player = GetPlayer(playerName)
            };
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinLobbyOptions); // Быстрое подулючение к подходящему лобби
            joinedLobby = lobby;

            Debug.Log("Joined lobby with name " + joinedLobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void UpdateLobbyGameMode(string newGameMode)
    {
        try
        {
            // Обновление параметров лобби.
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, newGameMode) }
                }
            });
            joinedLobby = hostLobby;

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            // Обновление параметров игрока.
            await Lobbies.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newPlayerName) }
                }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId); // Выйти из лобби
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void KickPlayer(int indexPlayer)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[indexPlayer].Id); // Кикнуть игрока из лобби.
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void MigrateLobbyHost(int indexPlayer)
    {
        try
        {
            // Обновление параметров лобби.
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = hostLobby.Players[indexPlayer].Id
            });
            joinedLobby = hostLobby;

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id); // Удалить лобби.
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void StartGame()
    {
        try
        {
            string joinRelayCode = await RelayManager.CreateRelay();

            // Обновление параметров лобби.
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "StartGame", new DataObject(DataObject.VisibilityOptions.Member, joinRelayCode) }
                }
            });
            joinedLobby = hostLobby;

            Debug.Log("StartHost");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public List<Player> GetPlayers()
    {
        if (joinedLobby == null)
            return null;

        return joinedLobby.Players;
    }
}

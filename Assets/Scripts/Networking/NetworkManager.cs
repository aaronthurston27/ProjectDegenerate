﻿using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;

using Debug = UnityEngine.Debug;

/// <summary>
/// Class for managing network interaction between clients
/// Should monitor network connection, Average Latency, and game status
/// </summary>
public class NetworkManager : MonoBehaviour, IConnectionCallbacks, IMatchmakingCallbacks, ILobbyCallbacks, IOnEventCallback, IInRoomCallbacks
{

    #region Custom Photon Event Codes

    public const byte StartGame = 0x00;

    public const byte StartGameAck = 0x01;

    public const byte SynchronizationNeeded = 0x10;

    public const byte SynchronizeClientGameState = 0x11;

    public const byte HeartbeatPacket = 0x12;

    public const byte HeartbeatPacketAck = 0x13;

    public const byte PlayerInputUpdate = 0x20;

    public const byte PlayerInputAck = 0x21;

    public const byte PingRequest = 0x30;

    public const byte PingAck = 0x31;

    #endregion

    #region Player Custom Property Keys

    public const string PlayerPingKey = "Ping";

    public const string PlayerReadyKey = "Ready";

    public const string MasterClientKey = "Master";

    #endregion

    #region Unity Events

    #endregion

    #region Const Variables

    private const string ActivePlayerKey = "ActivePlayers";

    private const uint MillisecondsPerFrame = 16;

    private const uint MillisecondsPerSecond = 1000;

    private const short PingTimeoutInMilliseconds = 60;

    private const int PingSamples = 3;

    #endregion

    #region Main Variables

    [HideInInspector]
    public string CurrentRoomId;

    public int TotalDelayFrames
    {
        get;  private set;
    }

    public int NetworkDelayFrames
    {
        get
        {
            return Math.Max(TotalDelayFrames - GameStateManager.Instance.LocalFrameDelay, 0);
        }
    }

    public long CurrentDelayInMilliSeconds { get; private set; }

    /// <summary>
    /// Set to true if currently synchronizing game state (rolling back).
    /// </summary>
    [HideInInspector]
    public bool IsSynchronizing { get; private set; }

    private bool IsNetworkedGameReady;

    private bool CurrentlyPingingPlayers;

    // Hash set of players we need to ping, sorted by actor number.
    private HashSet<int> PlayersToPing = new HashSet<int>();

    #endregion

    #region Singleton Instance

    private static NetworkManager instance;

    public static NetworkManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NetworkManager>();
            }
            if (instance == null)
            {
                instance = new NetworkManager();
            }

            return instance;
        }
    }

    #endregion

    #region Monobehaviour Methods

    void Awake()
    {
        if (Overseer.Instance.IsNetworkedMode)
        {
            ConnectToNetwork();
        }
    }

    void OnDestroy()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    #endregion

    #region Public Interface

    public void ConnectToNetwork()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.AddCallbackTarget(this);
            RegisterEventTypes();
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void DisconnectFromNetwork()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public void DisconnectFromLobby()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentLobby != null)
        {
            PhotonNetwork.LeaveLobby();
        }
    }

    public void DisconnectFromRoom()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void SendEventData(byte eventCode, object eventData, ReceiverGroup receivers = 0, bool sendImmediate = false)
    {
        RaiseEventOptions receiveOptions = new RaiseEventOptions();
        receiveOptions.Receivers = receivers;
        PhotonNetwork.SendAsap(sendImmediate);
        PhotonNetwork.RaiseEvent(eventCode, eventData, receiveOptions, SendOptions.SendUnreliable);
    }

    public void SetPlayerReady(bool isReady)
    {
        Hashtable properties = PhotonNetwork.LocalPlayer.CustomProperties;
        properties[PlayerReadyKey] = isReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
    }

    #endregion

    #region Private Interface

    private void SetPlayerPhotonSettings()
    {
        ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
    }

    private void RegisterEventTypes()
    {
        PhotonPeer.RegisterType(typeof(PlayerInputPacket), PlayerInputUpdate, PlayerInputPacket.Serialize, PlayerInputPacket.Deserialize);
    }

    private Hashtable GetActivePlayers()
    {
        return PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ActivePlayerKey) ? (Hashtable)PhotonNetwork.CurrentRoom.CustomProperties[ActivePlayerKey] : null;
    }

    #endregion

    #region Lobby Callbacks

    public void OnJoinedLobby()
    {
        Debug.LogWarning("Joined lobby");
    }

    public void OnLeftLobby()
    {
        Debug.Log("Photon: Left Lobby");
    }

    // Room list. Automatically called after joining lobby.
    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            Debug.Log("Room Name = " + room.Name + ", Player Count:" + room.PlayerCount);
        }

        if (roomList.Count > 0)
        {
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.CreateRoom("Bitch Niggas Only", null);
        }
    }

    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        throw new System.NotImplementedException();
    }

    #endregion

    #region Matchmaking Callbacks

    public void OnCreatedRoom()
    {
        Debug.Log("Sucessfully created room: RoomID = " + PhotonNetwork.CurrentRoom.Name);
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to create room: Code=[" + returnCode + "], Error = " + message);
    }

    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        // To Do Print Friends list
    }

    public void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        CurrentRoomId = PhotonNetwork.CurrentRoom.Name;
        Overseer.Instance.HandleJoinedRoom();
        if (PhotonNetwork.IsMasterClient)
        {
            AddPlayerToActivePlayerTable(PhotonNetwork.LocalPlayer);
        }
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to join random room: Code=[" + returnCode + "], Error = " + message);
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to join room: Code=[" + returnCode + "], Error = " + message);
    }

    public void OnLeftRoom()
    {
        Debug.Log("Sucessfully Left Photon Room");
        CurrentRoomId = null;
        Overseer.Instance.HandleLeftRoom();
    }

    #endregion

    #region Connection Callback Interface
    public void OnConnected()
    {

    }

    public void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {

    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {

    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected: " + cause.ToString());
        CurrentRoomId = null;
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {

    }

    #endregion

    #region EventCallbacks

    // On photon event received callback
    public void OnEvent(ExitGames.Client.Photon.EventData photonEvent)
    {
        if (photonEvent.Code == PingRequest)
        {
            SendEventData(PingAck, PhotonNetwork.LocalPlayer.ActorNumber, default, true);
        }
        else if (photonEvent.Code == PingAck && CurrentlyPingingPlayers)
        {
            int actorNumber = (int)photonEvent.CustomData;
            if (actorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                OnPingAckReceived(actorNumber);
            }
        }
        else if (photonEvent.Code == StartGame)
        {
            IsNetworkedGameReady = (bool)photonEvent.CustomData;
        }
        else if (photonEvent.Code == StartGameAck)
        {
            IsNetworkedGameReady = (bool)photonEvent.CustomData;
        }
        else if (photonEvent.Code == SynchronizationNeeded)
        {
            int frameCount = (int)photonEvent.CustomData;
            HandleSynchronizationRequest((uint)frameCount);
        }
        else if (photonEvent.Code == SynchronizeClientGameState && !IsSynchronizing)
        {
            int frameCount = (int)photonEvent.CustomData;
            StartCoroutine(StartGameStateSynchronization((uint)frameCount));
        }
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddPlayerToActivePlayerTable(newPlayer);
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ActivePlayerKey))
        {
            ExitGames.Client.Photon.Hashtable activePlayers = (ExitGames.Client.Photon.Hashtable)PhotonNetwork.CurrentRoom.CustomProperties[ActivePlayerKey];
            if (activePlayers.ContainsKey(otherPlayer.ActorNumber))
            {
                RemovePlayerFromActiveTable(otherPlayer);
            }
        }
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {

    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ActivePlayerKey) && targetPlayer.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            ExitGames.Client.Photon.Hashtable activePlayers = (ExitGames.Client.Photon.Hashtable)PhotonNetwork.CurrentRoom.CustomProperties[ActivePlayerKey];
            if (activePlayers.ContainsKey(targetPlayer.ActorNumber))
            {
                UpdatePing();
            }
        }
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        //
    }

    #endregion

    #region Synchronization methods

    public void SynchronizeGame()
    {
        if (IsSynchronizing)
            return;

        IsSynchronizing = true;
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SynchronizePlayersMaster());
        }
        else
        {
            StartCoroutine(WaitForMasterClientSync());
        }
    }

    #endregion

    #region Master Client Methods
    // Methods that should only be executed if the current client is designated by the server as the master client
    private void AddPlayerToActivePlayerTable(Player player)
    {

        // Add player to set found in room custom properties
        // set should contain players that are currently player (not observing).
        ExitGames.Client.Photon.Hashtable hashtable = PhotonNetwork.CurrentRoom.CustomProperties;

        ExitGames.Client.Photon.Hashtable activePlayers = (ExitGames.Client.Photon.Hashtable)PhotonNetwork.CurrentRoom.CustomProperties[ActivePlayerKey] ?? new ExitGames.Client.Photon.Hashtable();

        if (!activePlayers.ContainsKey(player.ActorNumber))
        {
            activePlayers.Add(player.ActorNumber, player.ActorNumber);
        }
        hashtable[ActivePlayerKey] = activePlayers;

        PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);

        UpdatePing();
    }

    private void RemovePlayerFromActiveTable(Player player)
    {
        // Remove player from dictionary after leaving

        Hashtable hashtable = PhotonNetwork.CurrentRoom.CustomProperties;
        Hashtable activePlayers = (Hashtable)PhotonNetwork.CurrentRoom.CustomProperties[ActivePlayerKey] ?? new Hashtable();

        if (activePlayers.ContainsKey(player.ActorNumber))
        {
            activePlayers.Remove(player.ActorNumber);
        }
        hashtable[ActivePlayerKey] = activePlayers;

        PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);
    }

    private IEnumerator SynchronizePlayersMaster()
    {
        while(!CheckIfPlayersReady())
        {
            yield return null;
        }

        SetMasterClientReady(true);
        yield return new WaitForEndOfFrame();

        PingActivePlayers();
        // Current delay frames should only be set to > 0 if the number of players with set ping values is >= number of players needed to start the game.
        while (TotalDelayFrames <= 0)
        {
            yield return null;
            UpdatePing();
        }
        yield return null;

        IsNetworkedGameReady = true;
        SendEventData(StartGame, true, ReceiverGroup.Others);

        long frameDelay = NetworkDelayFrames;
        while (frameDelay > 0)
        {
            yield return null;
            --frameDelay;
        }

        IsSynchronizing = false;
    }

    private bool CheckIfPlayersReady()
    {
        Hashtable activePlayers = GetActivePlayers();
        if (activePlayers == null || activePlayers.Count < Overseer.NumberOfPlayers)
        {
            return false;
        }

        bool ready = true;
        foreach (int actorNumber in activePlayers.Values)
        {
            Player player = PhotonNetwork.CurrentRoom.Players[actorNumber];
            ready &= player != null && player.CustomProperties.ContainsKey(PlayerReadyKey) ? (bool)player.CustomProperties[PlayerReadyKey] : false;
            //Debug.LogWarning("PlayerId: " + actorNumber + ", Ready = " + ready);
        }
        return ready;
    }

    private void SetMasterClientReady(bool isReady)
    {
        Hashtable prop = PhotonNetwork.LocalPlayer.CustomProperties;
        prop[MasterClientKey + PlayerReadyKey] = isReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(prop);
    }

    #endregion

    #region Slave Client Methods

    private IEnumerator WaitForMasterClientSync()
    {
        while(!CheckIfMasterClientReady())
        {
            yield return new WaitForEndOfFrame();
        }

        PingActivePlayers();

        while(!IsNetworkedGameReady)
        {
            yield return null;
        }
        IsSynchronizing = false;
    }

    private bool CheckIfMasterClientReady()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null)
        {
            return false;
        }
        return PhotonNetwork.MasterClient.CustomProperties.ContainsKey(MasterClientKey + PlayerReadyKey) ? (bool)PhotonNetwork.MasterClient.CustomProperties[MasterClientKey + PlayerReadyKey] : false;
    }

    #endregion

    #region Ping Methods

    public void PingActivePlayers()
    {
        if (!CurrentlyPingingPlayers)
        {
            StartCoroutine(CheckPlayerPing());
        }
    }

    private IEnumerator CheckPlayerPing()
    {
        CurrentlyPingingPlayers = true;

        while (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ActivePlayerKey))
        {
            yield return null;
        }

        long averagePing = 0;

        for (int i = 0; i < PingSamples; ++i)
        {
            PingActivePlayersInternal();
            Stopwatch rtt = new Stopwatch();
            rtt.Start();

            while (PlayersToPing.Count > 0)
            {
                yield return null;
            }

            rtt.Stop();
            averagePing += rtt.ElapsedMilliseconds;
        }
        averagePing /= PingSamples;
        SetLocalPlayerPing(averagePing);
        UpdatePing();
        CurrentlyPingingPlayers = false;
        
    }

    private void PingActivePlayersInternal()
    {
        Hashtable activePlayers = GetActivePlayers();
        PlayersToPing.Clear();
        foreach (int actorNumber in activePlayers.Keys)
        {
            if (PhotonNetwork.CurrentRoom.Players.ContainsKey(actorNumber) && actorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                PlayersToPing.Add(actorNumber);
            }
        }
        SendEventData(PingRequest, PhotonNetwork.LocalPlayer.ActorNumber, default, true);
    }

    private void OnPingAckReceived(int actorNumber)
    {
       if (PlayersToPing.Contains(actorNumber))
        {
            PlayersToPing.Remove(actorNumber);
        }
    }

    public void SetLocalPlayerPing(long pingInMilliseconds)
    {
        Hashtable playerProperties = PhotonNetwork.LocalPlayer.CustomProperties;
        playerProperties[PlayerPingKey] = pingInMilliseconds;
        //Debug.LogWarning("Setting local ping: " + pingInMilliseconds);
        PhotonNetwork.SetPlayerCustomProperties(playerProperties);
    }

    private void UpdatePing()
    {

        Hashtable activePlayers = GetActivePlayers();
        if (activePlayers != null && activePlayers.Count >= Overseer.NumberOfPlayers)
        {
            long highestPing = 0;

            foreach (int actorNumber in activePlayers.Keys)
            {
                Player player = PhotonNetwork.CurrentRoom.Players.ContainsKey(actorNumber) ? PhotonNetwork.CurrentRoom.Players[actorNumber] : null;
                if (player != null)
                {
                    long playerPing = player.CustomProperties.ContainsKey(PlayerPingKey) ? (long)player.CustomProperties[PlayerPingKey] : 0;
                    // If a player has not set their ping yet, return from this method and wait.
                    if (playerPing == 0)
                    {
                        return;
                    }
                    highestPing = Math.Max(playerPing, highestPing);
                }
            }

            CurrentDelayInMilliSeconds = highestPing;

            float localDelayInMilliseconds = GameStateManager.Instance.LocalFrameDelay * MillisecondsPerFrame;
            float frameTimeInMilliseconds = 16.66f;
            float calculatedDelay = Mathf.Ceil((highestPing + (localDelayInMilliseconds * 2)) / (2.0f * frameTimeInMilliseconds));
            TotalDelayFrames = (int)calculatedDelay;
        }
    }

    #endregion

    #region Game Sync Methods

    public void RequestGameStateSynchronization(uint FrameToSync)
    {
        Debug.LogWarning("Requesting synchronization for frame: " + FrameToSync);
        if (PhotonNetwork.IsMasterClient)
        {
            SendEventData(SynchronizeClientGameState, (int)FrameToSync, ReceiverGroup.Others);
            StartCoroutine(StartGameStateSynchronization(FrameToSync));
        }
        else
        {
            SendEventData(SynchronizationNeeded, (int)FrameToSync, ReceiverGroup.Others);
        }
    }

    public void HandleSynchronizationRequest(uint FrameToSync)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SendEventData(SynchronizeClientGameState, (int)FrameToSync, ReceiverGroup.Others);
            StartCoroutine(StartGameStateSynchronization(FrameToSync));
        }
    }

    private IEnumerator StartGameStateSynchronization(uint FrameToSync)
    {
        if (IsSynchronizing)
        {
            yield break;
        }
        Debug.LogWarning("Start game state synchronization. Frame to sync: " + FrameToSync);
        IsSynchronizing = true;

        SetPlayerReady(false);
        Overseer.Instance.HandleRollbackRequest(FrameToSync);

        while(!(bool)PhotonNetwork.LocalPlayer.CustomProperties[PlayerReadyKey])
        {
            yield return null;
        }

        if(PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SynchronizePlayersMaster());
        }
        else
        {
            StartCoroutine(WaitForMasterClientSync());
        }

    }

    #endregion
}

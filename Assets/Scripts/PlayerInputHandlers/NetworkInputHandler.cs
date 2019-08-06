﻿using System;
using System.Collections;
using System.Collections.Generic;

using ExitGames.Client.Photon;

using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkInputHandler : MonoBehaviour, IMatchmakingCallbacks
{

    #region const variables
    
    // If the difference in ping we read from the custom player properties is more than this value, then we should
    // update the custom properties with the new value.
    private const int PingThreshold = 5;

    private const float SecondsToCheckForPing = 6f;

    #endregion

    #region main variables

    private PlayerController PlayerController;

    private CommandInterpreter CommandInterpreter;

    #endregion

    #region Callbacks
    public void OnCreatedRoom()
    {
        //
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        //
    }

    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        //
    }

    public void OnJoinedRoom()
    {

    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        //
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        //
    }

    public void OnLeftRoom()
    {
        //
    }

    #endregion

    #region monobehaviour methods

    void Update()
    {

    }

    void Awake()
    {
        PlayerController = GetComponent<PlayerController>();
        CommandInterpreter = PlayerController.CommandInterpreter;
        Overseer.Instance.OnGameReady += OnGameReady;
        PhotonNetwork.AddCallbackTarget(this);
        UpdatePlayerPing();
    }

    #endregion

    #region private interface

    private void OnGameReady(bool isGameReady)
    {
        if (isGameReady)
        {
            UpdatePlayerPing();

            StartCoroutine(CheckForPingUpdate());
            StartCoroutine(SendInputIfNeccessary());

            enabled = true;
        }
    }

    private void UpdatePlayerPing()
    {
        if (PhotonNetwork.IsConnected)
        {
            ExitGames.Client.Photon.Hashtable playerProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            if (playerProperties.ContainsKey(NetworkManager.PlayerPingKey))
            {
                int ping = (int)playerProperties[NetworkManager.PlayerPingKey];
                int currentPing = PhotonNetwork.GetPing();

                if (Math.Abs(currentPing - ping) >= PingThreshold)
                {
                    playerProperties[NetworkManager.PlayerPingKey] = currentPing;
                    Debug.LogWarning("Set");
                    PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
                }
            }
            else
            {
                playerProperties[NetworkManager.PlayerPingKey] = PhotonNetwork.GetPing();
            }
        }
    }

    private IEnumerator CheckForPingUpdate()
    {
        while (Overseer.Instance.IsGameReady)
        {
            yield return new WaitForSeconds(SecondsToCheckForPing);

            UpdatePlayerPing();
        }
    }

    private IEnumerator SendInputIfNeccessary()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (Overseer.Instance.IsGameReady && CommandInterpreter != null)
            {
                PlayerInputData inputData = CommandInterpreter.GetPlayerInputDataIfUpdated();
                if (inputData != null)
                {
                    Debug.LogWarning("Sending");
                    inputData.PlayerIndex = PlayerController.PlayerIndex;

                    NetworkManager.Instance.SendEventData(NetworkManager.PlayerInputUpdate, inputData);
                }
            }
        }
    }

    #endregion
}

﻿using System;
using System.Collections;
using System.Collections.Generic;

using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RemotePlayerController : PlayerController, IOnEventCallback
{

    #region player controller override methods

    // Pattern: 
    // Bit 0: LP
    // Bit 1: MP
    // Bit 2: HP
    // Bit 3: LK
    // Bit 4: MK
    // Bit 5: HK
    // Bit 6: Left Directional Input
    // Bit 7: Right Directional Input
    // Bit 8: Up Directional Input
    // Bit 9: Down Directional Input

    protected override void UpdateButtonInput(ref ushort inputPattern)
    {

    }

    protected override void UpdateJoystickInput(ref ushort inputPattern)
    {
        
    }

    #endregion

    #region main variables

    #endregion

    #region monobehaviour methods

    public void Update()
    {
    }

    public void Awake()
    {
        PhotonNetwork.AddCallbackTarget(this);
        Overseer.Instance.OnGameReady += OnGameReady;
        enabled = false;
    }

    #endregion

    #region photon event callback

    public void OnEvent(ExitGames.Client.Photon.EventData eventData)
    {
        if (eventData.Code == NetworkManager.PlayerInputUpdate)
        {
            try
            {
                PlayerInputData data = eventData.CustomData as PlayerInputData;
                Debug.LogWarning("Received Frame: " + GameStateManager.Instance.FrameCount);
                Debug.LogWarning("Received time: " + DateTime.Now.ToString("hh.mm.ss.ffffff"));
                if (data != null && data.PlayerIndex == PlayerIndex && Overseer.Instance.IsGameReady)
                {
                    CommandInterpreter.QueuePlayerInput(data);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    #endregion

    #region private interface

    #endregion
}

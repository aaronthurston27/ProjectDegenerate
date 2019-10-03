﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// A reference to all important variables in our character. Such as the 
/// </summary>
public class CharacterStats : MonoBehaviour
{
    #region enum

    /// <summary>
    /// 
    /// </summary>
    public enum CharacterTeam
    {
        PLAYER_1,
        PLAYER_2,
        NEUTRAL,
    }

    // Shouldn't this be in movement mechanics or the animator?
    public enum CharacterState
    {
        FreeMovement, //Player has full control of their movement
        NoPlayerControlledMovement,//If the player is in hit stun or some other even, this movement type may be set
        Dashing,//Player is dashing
    }
    #endregion

    #region event functions
    public UnityEvent OnCharacterHealthChanged;
    #endregion event functions

    #region const variables

    private const float ChipDamageRecoveryDelay = 2f;

    // Health to recover per second
    private const float ChipDamageRecoveryRate = 1.5f;

    #endregion

    #region fields

    /// <summary>
    /// Movement Mechanics Reference
    /// </summary>
    public MovementMechanics MovementMechanics { get; private set; }

    /// <summary>
    /// Character State
    /// </summary>
    public CharacterState currentCharacterState { get; set; }

    public Animator Anim;

    [HideInInspector]
    public int PlayerIndex;

    #endregion

    #region main variables

    private IEnumerator ChipDamageCoroutine;

    #endregion

    #region Health Variables

    public float TotalHealth {
        get
        {
            return CurrentHealth - CurrentChipDamage - ComboDamage;
        }
    }

    public float CurrentHealth { get; private set; }

    public float MaxHealth = 100;

    public float CurrentChipDamage { get; private set; }

    public float ComboDamage { get; private set; }

    #endregion

    #region Special Meter Variables

    public float SpecialMeter { get; private set; }

    #endregion

    #region monobehaviour methods

    private void Awake()
    {
        MovementMechanics = GetComponent<MovementMechanics>();
        Anim = GetComponent<Animator>();

        CurrentHealth = 100f;
        SpecialMeter = 0f;
        OnCharacterHealthChanged.Invoke();
    }

    #endregion

    #region public interface

    public void OnMoveExecuted(InteractionHandler.MoveData move)
    {
        SpecialMeter -= move.SpecialMeterRequired;
    }

    // DidMoveHit == False: Move was blocked
    public void OnPlayerHitByEnemy(InteractionHandler.MoveData move, bool didMoveHit)
    {
        if (Overseer.Instance.IsGameReady)
        {
            if (ChipDamageCoroutine != null)
            {
                StopCoroutine(ChipDamageCoroutine);
            }
            
            if (didMoveHit)
            {
                // "Cash In" built up chip damage that has not been recovered yet.
                CurrentHealth -= CurrentChipDamage;

                ComboDamage += move.HitDamage;
                CurrentHealth -= move.HitDamage;

                CurrentChipDamage = 0;
            }
            else
            {
                CurrentChipDamage += move.ChipDamage;
            }
        }

        OnCharacterHealthChanged.Invoke();
    }

    public void OnPlayerHitEnemy(Hitbox myHitbox, InteractionHandler.MoveData move, bool didMoveHit)
    {
        if (Overseer.Instance.IsGameReady)
        {
            float meterToAdd = didMoveHit ? move.HitMeterGain : move.ChipMeterGain;
            SpecialMeter += meterToAdd;
        }
    }

    public void OnClash(Hitbox myHitbox, Hitbox enemyHitbox,InteractionHandler.MoveData move)
    {
        print("Clash");
    }

    public void OnHitstunFinished()
    {
        ComboDamage = 0;

        if (ChipDamageCoroutine != null)
        {
            StopCoroutine(ChipDamageCoroutine);
        }

        if (CurrentChipDamage > 0)
        {
            ChipDamageCoroutine = RecoverChipDamage();
            StartCoroutine(ChipDamageCoroutine);
        }
        OnCharacterHealthChanged.Invoke();
    }

    #endregion

    #region private interface

    private IEnumerator RecoverChipDamage()
    {

        float secondsToWait = ChipDamageRecoveryDelay;
        
        while (secondsToWait > 0.0f)
        {
            yield return new WaitForEndOfFrame();
            secondsToWait -= Overseer.DELTA_TIME;
        }

        while (CurrentChipDamage > 0.0f)
        {
            float newChipDamage = CurrentChipDamage - (Overseer.DELTA_TIME * ChipDamageRecoveryRate);
            CurrentChipDamage = Mathf.Max(newChipDamage, 0.0f);
            OnCharacterHealthChanged.Invoke();
            yield return new WaitForEndOfFrame();
        }
    }

    #endregion

}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YukariArrow : BaseProjectile
{
    /// <summary>
    /// 
    /// </summary>
    public int bounceCount = 2;


    #region monobehaviour methods
    private void OnValidate()
    {
        if (rigid == null)
        {
            rigid = GetComponent<CustomPhysics2D>();
        }
        rigid.useGravity = false;
    }
    #endregion monobehaviour methods



    /// <summary>
    /// This method should be called upon spawning our projectile
    /// </summary>
    public override void SetupProjectile(CharacterStats characterStats)
    {
        base.SetupProjectile(characterStats);

        
    }

    /// <summary>
    /// 
    /// </summary>
    public override void LaunchProjectile()
    {
        
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnCharacterPressedButton()
    {

    }
}

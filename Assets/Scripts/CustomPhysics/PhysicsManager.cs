﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// The manager that handles how we control all of our physics objects in the game. This updates objects that contain a custom physics object and any collider object
/// </summary>
public class PhysicsManager : MonoBehaviour
{
    private static PhysicsManager instance;

    public static PhysicsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = GameObject.FindObjectOfType<PhysicsManager>();
            }
            return instance;
        }
    }

    /// <summary>
    /// A list of all the custom colliders in the scene
    /// </summary>
    private List<CustomCollider2D> nonStaticColliderList = new List<CustomCollider2D>();

    /// <summary>
    /// 
    /// </summary>
    private List<CustomCollider2D> staticColliderList = new List<CustomCollider2D>();
    /// <summary>
    /// A list of all the physics objects in the scene
    /// </summary>
    private List<CustomPhysics2D> allCustomPhysicsObjectsList = new List<CustomPhysics2D>();
    #region monobehaviour methods
    private void Awake()
    {
        instance = this;
    }
    private void LateUpdate()
    {
        if (!Overseer.Instance.IsGameReady)
        {
            return;
        }

        Overseer.Instance.HitboxManager.UpdateHitboxManager();

        foreach (CustomCollider2D collider in nonStaticColliderList)
        {
            if (collider.enabled)
            {
                collider.UpdateBoundsOfCollider();
            }
        }

        
        //Updates the velocity based on gravity
        foreach (CustomPhysics2D rigid in allCustomPhysicsObjectsList)
        {
            if (rigid.enabled)
            {
                rigid.UpdateVelocityFromGravity();
            }
        }

        foreach (CustomCollider2D collider in nonStaticColliderList)
        {
            collider.UpdateBoundsOfCollider();
            collider.originalVelocity = collider.rigid.velocity;
        }

        float xi;
        float xj;
        CustomCollider2D slowerCollider;
        CustomCollider2D fasterCollider ;
        float combinedVelocity;
        for (int i = 0; i < nonStaticColliderList.Count - 1; i++)
        {
            for (int j = i + 1; j < nonStaticColliderList.Count; j++)
            {
                if (!nonStaticColliderList[i].isActiveAndEnabled || !nonStaticColliderList[j].isActiveAndEnabled)
                {
                    continue;
                }
                    
                xi = nonStaticColliderList[i].rigid.velocity.x;
                xj = nonStaticColliderList[j].rigid.velocity.x;
                if (!(nonStaticColliderList[i].rigid.isInAir ^ nonStaticColliderList[j].rigid.isInAir))
                {
                    if (Mathf.Abs(xj) > Mathf.Abs(xi))
                    {
                        fasterCollider = nonStaticColliderList[j];
                        slowerCollider = nonStaticColliderList[i];
                    }
                    else
                    {
                        fasterCollider = nonStaticColliderList[i];
                        slowerCollider = nonStaticColliderList[j];
                    }
                    if (!fasterCollider.ColliderIntersectHorizontally(slowerCollider))
                    {
                        continue;
                    }

                }
                else
                {
                    if (nonStaticColliderList[i].rigid.isInAir)
                    {
                        slowerCollider = nonStaticColliderList[j];
                        fasterCollider = nonStaticColliderList[i];
                    }
                    else
                    {
                        slowerCollider = nonStaticColliderList[i];
                        fasterCollider = nonStaticColliderList[j];
                    }
                    if (!slowerCollider.ColliderIntersectHorizontally(fasterCollider))
                    {
                        continue;
                    }
                }

                
                


                if ((xi > 0 && xj > 0) || (xi < 0 && xj < 0))
                {
                    combinedVelocity = Mathf.Sign(xi) * Mathf.Max(xi, xj);
                }
                else
                {
                    combinedVelocity = xi + xj;
                }
                fasterCollider.rigid.velocity.x = combinedVelocity;
                slowerCollider.rigid.velocity.x = combinedVelocity;
                fasterCollider.UpdateBoundsOfCollider();
                slowerCollider.UpdateBoundsOfCollider();
                if (CheckForHorizontalCollisions(slowerCollider))
                {

                    slowerCollider.UpdateBoundsOfCollider();
                    fasterCollider.rigid.velocity.x = 0;
                    slowerCollider.rigid.velocity.x = 0;
                    if (fasterCollider.ColliderIntersectHorizontally(slowerCollider))
                    {
                        fasterCollider.UpdateBoundsOfCollider();
                        slowerCollider.UpdateBoundsOfCollider();
                    }

                }

                //else
                //{
                //    print("I did not collide with anything");
                //    Debug.Break();
                //}
            }
        }


        foreach (CustomCollider2D nonStaticCollider in nonStaticColliderList)
        {
            foreach (CustomCollider2D staticCollider in staticColliderList)
            {
                if (!staticCollider.isActiveAndEnabled || !nonStaticCollider.isActiveAndEnabled)
                {
                    continue;//Skip if either collider is inactive
                }
                bool collidedVertically = nonStaticCollider.ColliderIntersectVertically(staticCollider);
                bool collidedHorizontally = nonStaticCollider.ColliderIntersectHorizontally(staticCollider);

                if (collidedVertically)
                {
                    if (nonStaticCollider.rigid.isInAir && nonStaticCollider.rigid.velocity.y <= 0)
                    {
                        nonStaticCollider.rigid.isInAir = false;
                        nonStaticCollider.rigid.OnPhysicsObjectGrounded();
                    }
                    nonStaticCollider.rigid.velocity.y = 0;
                    nonStaticCollider.originalVelocity = nonStaticCollider.rigid.velocity;
                }
                if (collidedHorizontally)
                {
                    nonStaticCollider.rigid.velocity.x = 0;
                    nonStaticCollider.originalVelocity = nonStaticCollider.rigid.velocity;
                }


                
                if (collidedVertically || collidedHorizontally)
                {
                    nonStaticCollider.UpdateBoundsOfCollider();
                }

                
            }
        }


        //Updates our physics object based on its physics state
        foreach (CustomPhysics2D rigid in allCustomPhysicsObjectsList)
        {
            if (rigid.enabled)
            {
                rigid.UpdatePhysics();
            }
        }


        foreach (CustomCollider2D collider in nonStaticColliderList)
        {
            collider.rigid.velocity = collider.originalVelocity;
        }

        //for (int i = 0; i < colliderList.Count - 1; i++)
        //{
        //    for (int j = i + 1; j < colliderList.Count; j++)
        //    {
        //        if (colliderList[i].ColliderIntersect(colliderList[j]))
        //        {
        //            //print("I made it here");
        //            if (colliderList[i].isStatic)
        //            {
        //                colliderList[i].PushObjectOutsideOfCollider(colliderList[j]);
        //            }
        //            if (colliderList[j].isStatic)
        //            {
        //                colliderList[j].PushObjectOutsideOfCollider(colliderList[i]);
        //            }
        //        }
        //    }
        //}
    }

    public bool CheckForHorizontalCollisions(CustomCollider2D colliderToCheck)
    {
        foreach (CustomCollider2D staticCollider in staticColliderList)
        {
            if (colliderToCheck.ColliderIntersectHorizontally(staticCollider))
            {
                return true;
            }
        }
        return false;
    }
    #endregion monobehaviour methods

    #region collider interaction methods


    /// <summary>
    /// Add a physics object to the manager.
    /// </summary>
    /// <param name="rigid"></param>
    public void AddCustomPhysics(CustomPhysics2D rigid) 
    {
        if (allCustomPhysicsObjectsList.Contains(rigid))
        {
            return;
        }
        allCustomPhysicsObjectsList.Add(rigid);
    }

    /// <summary>
    /// Remove a physics object from the manager if is present in the manager
    /// </summary>
    /// <param name="rigid"></param>
    public void RemoveCustomPhysics(CustomPhysics2D rigid)
    {
        if (allCustomPhysicsObjectsList.Contains(rigid))
        {
            allCustomPhysicsObjectsList.Remove(rigid);
        }
    }

    /// <summary>
    /// This will add a collider to the appropriate list. The list that it is assigned to will be determined by whether or not
    /// it uses a rigid body to move. (i.e. whether or not it is static
    /// </summary>
    /// <param name="collider"></param>
    public void AddColliderToManager(CustomCollider2D collider)
    {
        if (collider.isStatic)
        {
            if (staticColliderList.Contains(collider))
            {
                Debug.LogWarning("We are trying to add a collider to our static collider list multiplie times");
            }
            else
            {
                staticColliderList.Add(collider);
            }
        }
        else
        {
            if (nonStaticColliderList.Contains(collider))
            {
                Debug.LogWarning("We are trying to add a collider to our non static collider list multiplie times");
            }
            else
            {
                nonStaticColliderList.Add(collider);
            }
        }
    }

    /// <summary>
    /// Removes a collider object from our physics manager. This should typically only be called upon the collider
    /// object being destroyed
    /// </summary>
    /// <param name="collider"></param>
    public void RemoveColliderFromManager(CustomCollider2D collider)
    {
        if (collider.isStatic)
        {
            if (staticColliderList.Contains(collider))
            {
                staticColliderList.Remove(collider);
            }
        }
        else
        {
            if (nonStaticColliderList.Contains(collider))
            {
                nonStaticColliderList.Remove(collider);
            }
        }
    }
    #endregion collider interaction methods

    /// <summary>
    /// This will compile a list of all the colliders that intersect with the line that is passed into our method
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public bool CheckLineIntersectWithCollider(Vector2 origin, Vector2 direction, float distance)
    {
        List<CustomCollider2D> list = new List<CustomCollider2D>();
        return CheckLineIntersectWithCollider(origin, direction, distance, out list);
    }

    /// <summary>
    /// Gets a list of all colliders that intersect the line that passes through
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <param name="collidersHit"></param>
    /// <returns></returns>
    public bool CheckLineIntersectWithCollider(Vector2 origin, Vector2 direction, float distance, out List<CustomCollider2D> collidersHit)
    {
        collidersHit = new List<CustomCollider2D>();
        foreach (CustomCollider2D coll in nonStaticColliderList)
        {
            if (coll.enabled)
            {
                DebugSettings.DrawLineDirection(origin, direction, distance, Color.red);
                if (coll.LineIntersectWithCollider(origin, direction, distance))
                {
                    collidersHit.Add(coll);
                }
            }
        }
        return collidersHit.Count >= 1;
    }
}

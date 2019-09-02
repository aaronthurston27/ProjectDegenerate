﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Base class of our custom collider. This will check to see if there are any points where our collider intersects
/// with other colliders.
/// </summary>
public abstract class CustomCollider2D : MonoBehaviour {

    #region const variables
    protected readonly Color GIZMO_COLOR = Color.green;
    #endregion const variables
    [Tooltip("Mark this value true if you would like to treat this value as a trigger")]
    public bool isTrigger;
    
    /// <summary>
    /// The attached Custom physics component that is attached to our custom collider
    /// This is not required for components that are static.
    /// </summary>
    public CustomPhysics2D rigid { get; set; }

    /// <summary>
    /// IMPORTANT: If there is a Custom Physics object attached to the gameobject, this collider will be registered as a nonstatic collider
    /// </summary>
    public bool isStatic
    {
        get
        {
            return rigid == null;
        }
    }

    

    protected virtual void Awake()
    {
        UpdateBoundsOfCollider();
        rigid = GetComponent<CustomPhysics2D>();
        
        Overseer.Instance.ColliderManager.AddColliderToManager(this);
    }


    protected virtual void OnDestroy()
    {
        if (Overseer.Instance && Overseer.Instance.ColliderManager)
        {
            Overseer.Instance.ColliderManager.RemoveColliderFromManager(this);
        }
    }

    protected virtual void OnValidate()
    {
        
    }

    /// <summary>
    /// Be sure to call this method
    /// </summary>
    public abstract void UpdateBoundsOfCollider();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public abstract bool LineIntersectWithCollider(Vector2 origin, Vector2 direction, float length);

    

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public abstract Vector2 GetLowerBoundsAtXValue(float x);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public abstract Vector2 GetUpperBoundsAtXValue(float x);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public abstract Vector2 GetRighBoundAtYValue(float y);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public abstract Vector2 GetLeftBoundAtYValue(float y);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public abstract Vector2 GetCenter();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="colliderToCheck"></param>
    /// <returns></returns>
    public abstract bool ColliderIntersect(CustomCollider2D colliderToCheck);

    public abstract bool ColliderIntersectBasedOnVelocity(CustomCollider2D colliderToCheck);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="collider"></param>
    public abstract void PushObjectOutsideOfCollider(CustomCollider2D collider);

    public virtual CustomCollider2D[] GetAllTilesHitFromRayCasts(Vector2 v1, Vector2 v2, Vector2 direction, float distance, int rayCount)
    {
        Vector2 offset = (v2 - v1) / (rayCount - 1);
        List<CustomCollider2D> lineColliders;
        HashSet<CustomCollider2D> allLines = new HashSet<CustomCollider2D>();
        for (int i = 0; i < rayCount; i++)
        {
            Overseer.Instance.ColliderManager.CheckLineIntersectWithCollider(v1 + offset * i, direction, distance, out lineColliders);
            foreach (CustomCollider2D c in lineColliders)
            {
                if (c != this)
                {
                    allLines.Add(c);
                }
            }
        }

        CustomCollider2D[] allValidColliderList = new CustomCollider2D[allLines.Count];
        allLines.CopyTo(allValidColliderList);
        return allValidColliderList;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct BoundsRect
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomLeft;
        public Vector2 bottomRight;
        public Vector2 center;

        public Vector3[] GetVertices()
        {
            return new Vector3[]
            {
                topLeft,
                topRight,
                bottomRight,
                bottomLeft,
            };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct BoundsCircle
    {
        public Vector2 center;
        public float radius;
    }

    /// <summary>
    /// Bounds collider for our capsule collider simply contains two circles and a rect collider
    /// </summary>
    public struct BoundsCapsule
    {
        public BoundsRect rectBounds;
        public BoundsCircle topCircleBounds;
        public BoundsCircle bottomCircleBounds;
    }

    #region static methods
    /// <summary>
    /// Use this method to check if a rect bounds intersects another rect bound
    /// </summary>
    /// <returns></returns>
    public static bool RectIntersectRect(BoundsRect r1, BoundsRect r2)
    {
        Vector2 tl1 = r1.topLeft;
        Vector2 br1 = r1.bottomRight;
        Vector2 tl2 = r2.topLeft;
        Vector2 br2 = r2.bottomRight;

        if (tl1.x > br2.x || tl2.x > br1.x)
        {
            return false;
        }
        if (tl1.y < br2.y || tl2.y < br1.y)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Use this method to check if a rect bounds intersects a circle bounds
    /// </summary>
    /// <param name="r"></param>
    /// <param name="c"></param>
    /// <param name="intersectionPoint"></param>
    /// <returns></returns>
    public static bool RectIntersectCircle(BoundsRect r, BoundsCircle c)
    {

        Vector2 point = c.center;

        Vector2 A = r.topLeft;
        Vector2 B = r.topRight;
        Vector2 D = r.bottomLeft;
        float height = r.topLeft.y - r.bottomLeft.y;
        float width = r.topRight.x - r.topRight.x;
        float APdotAB = Vector2.Dot(point - A, B - A);
        float ABdotAB = Vector2.Dot(B - A, B - A);
        float APdotAD = Vector2.Dot(point - A, D - A);
        float ADdotAD = Vector2.Dot(D - A, D - A);
        if (0 <= APdotAB && APdotAB <= ABdotAB && 0 <= APdotAD && APdotAD < ADdotAD)
        {
            return true;

        }
        
        return LineIntersectCircle(c, r.bottomLeft, r.topRight);
        //float rectX = r.bottomLeft.x;
        //float recty = r.bottomLeft.y;

        //float nearestX = Mathf.Max(rectX, Mathf.Min(point.x, rectX + width));
        //float nearestY = Mathf.Max(recty, Mathf.Min(point.y, recty + height));

        //float dX = point.x - nearestX;
        //float dY = point.y - nearestY;

        //return (dX * dX + dY * dY) < c.radius * c.radius;
    }

    /// <summary>
    /// Use this method to check if two circle bounds are intersecting with each other
    /// </summary>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    /// <param name="intersectionPoint"></param>
    /// <returns></returns>
    public static bool CircleIntersectCircle(BoundsCircle c1, BoundsCircle c2)
    {
        float distanceMax = c1.radius + c2.radius;
        float distance = Vector2.Distance(c1.center, c2.center);

        return distance <= distanceMax;
    }

    public static bool CapsuleIntersectCapsule(BoundsRect c1, BoundsRect c2)
    {
        return false;
    }

    public static bool CapsuleIntersectCircle(BoundsRect cap, BoundsCircle cir)
    {
        return false;
    }

    public static bool CapsuleIntersectRect(BoundsRect cap, BoundsRect cir)
    {
        return false;
    }

    public static bool LineIntersectCircle(BoundsCircle c, Vector2 pointA, Vector2 pointB)
    {
        Vector2 point = c.center;

        float rectX = pointA.x;
        float recty = pointA.y;

        float nearestX = Mathf.Max(rectX, Mathf.Min(point.x, pointB.x));
        float nearestY = Mathf.Max(recty, Mathf.Min(point.y, pointB.y));

        float dX = point.x - nearestX;
        float dY = point.y - nearestY;

        return (dX * dX + dY * dY) < c.radius * c.radius;
    }
    #endregion static methods
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastVehicleVisor : MonoBehaviour
{
    [SerializeField] private Vector3 boxSize;
    [SerializeField] private float distace;
    [SerializeField] private Color color = Color.red;
    private int layerMask = 14;
    private int wallLayer = 0;
    private int additionalWallLayer = 0;
    private int playerLayer = 0;
        
    private bool m_HitDetect = false;
    RaycastHit hit;

    public float hitValue = 0;

    public bool isSideCast = false;

    private void OnEnable()
    {
        layerMask = LayerMask.GetMask("Traffic");
        wallLayer = LayerMask.GetMask("Wall");
        additionalWallLayer = LayerMask.GetMask("AdditionalWall");
        playerLayer = LayerMask.GetMask("RCC");
    }

    public void HitBoxRay()
    {
        int combinedLayerMask = layerMask | wallLayer | additionalWallLayer | playerLayer;
        if(isSideCast)
            combinedLayerMask = layerMask | wallLayer | additionalWallLayer;
        m_HitDetect = Physics.BoxCast(transform.position, boxSize, transform.forward, out hit, transform.rotation, distace);

            if (m_HitDetect)
            {
                hitValue = 1 - (hit.distance / distace);
            }
            else
                hitValue = 0;
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = color;

        HitBoxRay();
        //Check if there has been a hit yet
        if (m_HitDetect)
        {
            //Draw a Ray forward from GameObject toward the hit
            Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
            //Draw a cube that extends to where the hit exists
            Gizmos.DrawWireCube(transform.position + transform.forward * hit.distance, boxSize);
        }
        //If there hasn't been a hit yet, draw the ray at the maximum distance
        else
        {
            //Draw a Ray forward from GameObject toward the maximum distance
            Gizmos.DrawRay(transform.position, transform.forward * distace);
            //Draw a cube at the maximum distance
            Gizmos.DrawWireCube(transform.position + transform.forward * distace, boxSize);
        }
    }
#endif

}

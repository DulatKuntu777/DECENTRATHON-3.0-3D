using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastVehicleVisor : MonoBehaviour
{
    [SerializeField] private Vector3 boxSize;
    [SerializeField] private float distace;
        
    private bool m_HitDetect = false;
    RaycastHit hit;

    public float hitValue = 0;


    public void HitBoxRay()
    {
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
        Gizmos.color = Color.blue;

        HitBoxRay();
        if (m_HitDetect)
        {
            Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
            Gizmos.DrawWireCube(transform.position + transform.forward * hit.distance, boxSize);
        }
        else
        {
            Gizmos.DrawRay(transform.position, transform.forward * distace);
            Gizmos.DrawWireCube(transform.position + transform.forward * distace, boxSize);
        }
    }
#endif

}

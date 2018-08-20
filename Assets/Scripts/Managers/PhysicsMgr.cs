using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsMgr : MonoBehaviour
{
    // --------------------------------- PUBLIC ATTRIBUTES ------------------------------- //
    [Range(0, 0.01f)]
    public float m_collisionDetectionPrecision = 0.001f;

    public static float CollisionDetectionPrecision { get { return m_manager.m_collisionDetectionPrecision; } }

    // -------------------------------- PRIVATE ATTRIBUTES ------------------------------- //
    private static PhysicsMgr m_manager;

    // ======================================================================================
    // PUBLIC MEMBERS
    // ======================================================================================
    public void Start()
    {
        Debug.Assert(m_manager == null, this.gameObject.name + " - PhyscsMgr : physics manager must be unique!");
        m_manager = this;
    }
}

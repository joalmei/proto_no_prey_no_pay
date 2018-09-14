using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    // ======================================================================================
    // PUBLIC MEMBERS
    // ======================================================================================
    
    // TODO
    //public enum eType
    //{
    //    ONE_WAY,
    //    TWO_WAYS
    //}

    public float SurfaceY()
    {
        return this.transform.position.y;
    }
}

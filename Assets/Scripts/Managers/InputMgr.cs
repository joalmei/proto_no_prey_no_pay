using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputMgr : MonoBehaviour
{
    // --------------------------------------- ENUMS ------------------------------------- //
    public enum eAxis
    {
        HORIZONTAL,
        VERTICAL
    }

    public enum eButton
    {
        DASH,
        JUMP,
        TOSS,
        ATTACK,
        GRAB
    }

    // --------------------------------------- STRUCT ------------------------------------ //
    [System.Serializable]
    public struct sPlayerInput
    {
        public string m_horizontalAxis;
        public string m_verticalAxis;

        public string m_dashButton;

        public string m_jumpButton;

        public string m_tossButton;
        public string m_attackButton;
        public string m_grabButton;
    }

    // --------------------------------- PUBLIC ATTRIBUTES ------------------------------- //
    public sPlayerInput[]   m_playerInputs;
    
    // --------------------------------- PRIVATE ATTRIBUTES ------------------------------ //
    private static InputMgr m_manager;

    // ======================================================================================
    // PUBLIC MEMBERS
    // ======================================================================================
    public void Start()
    {
        Debug.Assert(m_manager == null, this.gameObject.name + " - InputMgr : input manager must be unique!");
        m_manager = this;
    }

    // ======================================================================================
    public static bool GetButton(int _player, eButton _button)
    {
        if (_player > m_manager.m_playerInputs.Length || _player <= 0)
            return false;

        int i = _player - 1;

        switch (_button)
        {
            case eButton.ATTACK:
                return Input.GetButtonDown(m_manager.m_playerInputs[i].m_attackButton);
            case eButton.DASH:
                return Input.GetButtonDown(m_manager.m_playerInputs[i].m_dashButton);
            case eButton.GRAB:
                return Input.GetButtonDown(m_manager.m_playerInputs[i].m_grabButton);
            case eButton.TOSS:
                return Input.GetButtonDown(m_manager.m_playerInputs[i].m_tossButton);
            case eButton.JUMP:
                return Input.GetButtonDown(m_manager.m_playerInputs[i].m_jumpButton);
        }

        return false;
    }

    // ======================================================================================
    public static float GetAxis(int _player, eAxis _axis)
    {
        if (_player > m_manager.m_playerInputs.Length || _player <= 0)
            return 0f;

        int i = _player - 1;

        switch (_axis)
        {
            case eAxis.HORIZONTAL:
                return Input.GetAxis(m_manager.m_playerInputs[i].m_horizontalAxis);
            case eAxis.VERTICAL:
                return Input.GetAxis(m_manager.m_playerInputs[i].m_verticalAxis);
        }

        return 0f;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum eStates
    {
        Idle,
        Walking,
        Jumping,
        Falling,
        Dashing
    }


    // -------------------------------- PUBLIC ATTRIBUTES -------------------------------- //
    // BASIC
    [Header("Player")]
    [Range(1, 4)]
    public int              m_nbPlayer                  = 1;

    [Header("Dimensions")]
    public float            m_width                     = .1f;
    public float            m_height                    = .1f;

    // ANIMATION
    [Header("Animation")]
    public Animator         m_animator;
    [ConditionalHide("m_useAnimation", true)]
    public string           m_isRunningBoolParam        = "IsRunning";
    [ConditionalHide("m_useAnimation", true)]
    public string           m_isDashingBoolParam        = "IsDashing";
    [ConditionalHide("m_useAnimation", true)]
    public string           m_isJumpingBoolParam        = "IsJetpackUp";
    [ConditionalHide("m_useAnimation", true)]
    public string           m_isFallingBoolParam        = "IsFalling";
    [ConditionalHide("m_useAnimation", true)]
    public string           m_onDeath                   = "OnDeath";

    [ConditionalHide("m_useAnimation", true)]
    public float            m_minSpeedToStartWalkAnim   = .2f;
    [ConditionalHide("m_useAnimation", true)]
    public float            m_minSpeedToStopWalkAnim    = .2f;

    // PHYSICS
    // TODO : Move to PhysicsMgr ???????
    [Header("Physics")]
    [Range(0, 1)]
    public float            m_gravityRatio              = .4f;

    // Locomotion
    [Header("Locomotion")]
    [Header("Walk")]
    public float            m_maxWalkSpeed              = 7;
    public float            m_walkAcc                   = 1;

    [Header("Dash")]
    public AnimationCurve   m_dashSpeed                 = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
    public float            m_dashMaxSpeed              = 10;
    public float            m_dashDuration              = 0.1f;
    public float            m_dashCoolDownDuration      = 1;

    [Header("Jump")]
    public float            m_jumpInitSpeed             = 20;
    public float            m_jumpCooldownDuration      = 1;

    // SFX
    //[Header("SFX")]
    //public AudioSource      m_dashSFX;
    //public AudioSource      m_jetpackSFX;
    //public AudioSource      m_deathSFX;
    //public AudioSource      m_damageSFX;
    

    // -------------------------------- PRIVATE ATTRIBUTES ------------------------------- //
    // LOCOMOTION: WALK
    private float           m_walkSpeed                 = 0;

    // LOCOMOTION: JUMP
    private float           m_gravSpeed                 = 0;
    private float           m_jumpCooldownTimer         = 0;

    // LOCOMOTION: DASH
    private float           m_dashTimer                 = 0;
    private Vector2         m_dashDirection             = Vector2.zero;
    private float           m_dashCooldownTimer         = 0;

    private eStates         m_state;

    private int             m_nbLives                   = 3;

    private float           m_collisionEpsilon;

    // DEFINES
    private const float     MIN_SPEED_TO_MOVE           = 0.1f;
    private const float     GROUND_Y_VALUE_TO_DELETE    = -2.5f;

    // -------------------------------- EDITOR ATTRIBUTES -------------------------------- //
    [HideInInspector]
    public bool             m_useAnimation;


    // ======================================================================================
    // PUBLIC MEMBERS
    // ======================================================================================
    public void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
#endif

        m_dashTimer         = m_dashDuration;
        m_dashDirection     = Vector2.right;
        
        m_dashCooldownTimer = m_dashCoolDownDuration;

        m_collisionEpsilon = PhysicsMgr.CollisionDetectionPrecision;
    }

    // ======================================================================================
    public void Update()
    {
#if UNITY_EDITOR
        m_useAnimation = m_animator != null;

        if (!Application.isPlaying)
            return;
#endif
        if (GameMgr.IsGameOver)
        {
            return;
        }

        // get input
        float horizontal    = InputMgr.GetAxis(m_nbPlayer, InputMgr.eAxis.HORIZONTAL);   //Input.GetAxis("Horizontal");
        float vertical      = InputMgr.GetAxis(m_nbPlayer, InputMgr.eAxis.VERTICAL);     //Input.GetAxis("Vertical");
        bool  doJump        = InputMgr.GetButton(m_nbPlayer, InputMgr.eButton.JUMP);     //Input.GetButtonDown("Jump");
        bool  doDash        = InputMgr.GetButton(m_nbPlayer, InputMgr.eButton.DASH);     //Input.GetButtonDown("Dash");
        

        // update position
        UpdateTransform(horizontal, vertical, doJump, doDash);

        if (m_animator != null)
            UpdateAnimator();
    }


    // ======================================================================================
    public void TakeDamage()
    {
        if (GameMgr.IsPaused || GameMgr.IsGameOver)
        {
            return;
        }

        m_nbLives--;

        if (m_nbLives <= 0)
        {
            //m_deathSFX.Play();
            m_animator.SetTrigger(m_onDeath);
        }
        else
        {
           // m_damageSFX.Play();
        }
    }

    // ======================================================================================
    // PRIVATE MEMBERS
    // ======================================================================================
    private void UpdateTransform(float _inputHorizontal, float _inputVertical, bool _doJump, bool _doDash)
    {
        // update transform
        Vector3 initialPos  = this.transform.position;

        bool isWalking      = UpdateWalk(_inputHorizontal);
        
        UpdateJump(_doJump);

        bool isDashing      = UpdateDash(_inputHorizontal, _inputVertical, _doDash);
        
        UpdateGravity();

        Vector3 finalPos    = this.transform.position;

        Vector3 deltaPos = finalPos - initialPos;

        // update animation state
        eStates nextState;
        if (isDashing)
        {
            nextState = eStates.Dashing;
        }
        else if (deltaPos.y > 0)
        {
            nextState = eStates.Jumping;
        }
        else if (deltaPos.y < 0)
        {
            nextState = eStates.Falling;
        }
        else if (isWalking)
        {
            nextState = eStates.Walking;
        }
        else
        {
            nextState = eStates.Idle;
        }

        // sfx
        //if (nextState == eStates.Dashing && m_state != nextState)
        //{
        //    m_jetpackSFX.Stop();
        //    m_dashSFX.Play();
        //}
        //else if (nextState == eStates.JetpackUp)
        //{
        //    m_jetpackSFX.loop = true;

        //    if (!m_jetpackSFX.isPlaying)
        //    {
        //        m_jetpackSFX.Play();
        //    }
        //}
        //else
        //{
        //    m_jetpackSFX.loop = false;
        //}

        m_state = nextState;
    }

    // ======================================================================================
    private bool UpdateWalk(float _inputHorizontal)
    {
        bool animate = false;

        // walk
        float nextSpeed         = Mathf.Lerp(m_walkSpeed, m_maxWalkSpeed * _inputHorizontal, GameMgr.DeltaTime * m_walkAcc);
        this.transform.position += Vector3.right * GameMgr.DeltaTime * nextSpeed;

        // Walking Anim State

        float nextSpeedMag      = Mathf.Abs(nextSpeed);
        float prevSpeedMag      = Mathf.Abs(m_walkSpeed);

        bool isStartingWalk     = nextSpeed > prevSpeedMag;

        m_walkSpeed             = nextSpeed;

        if (isStartingWalk && nextSpeedMag > m_minSpeedToStartWalkAnim || !isStartingWalk && nextSpeedMag > m_minSpeedToStopWalkAnim)
        {
            animate = true;
        }

        return animate;
    }

    // ======================================================================================
    private bool UpdateJump(bool _doJump)
    {
        m_jumpCooldownTimer -= GameMgr.DeltaTime;

        // Init Dash
        if (_doJump && m_jumpCooldownTimer < 0)
        {
            m_jumpCooldownTimer = m_dashCoolDownDuration;
            m_gravSpeed         = -m_jumpInitSpeed;
            return true;
        }

        return false;
    }

    // ======================================================================================
    private bool UpdateDash(float _inputHorizontal, float _inputVertical, bool _doDash)
    {
        bool animate        = false;

        m_dashCooldownTimer -= GameMgr.DeltaTime;

        // Init Dash
        if (_doDash && m_dashCooldownTimer < 0)
        {
            m_dashTimer         = 0;
            m_dashCooldownTimer = m_dashCoolDownDuration;
        }
        m_dashTimer += GameMgr.DeltaTime;

        // Keep memory of dash direction
        if (_inputHorizontal != 0 || _inputVertical != 0)
        {
            m_dashDirection.x = _inputHorizontal;
            m_dashDirection.y = _inputVertical;
        }

        m_dashDirection.Normalize();

        Debug.DrawRay(this.transform.position, new Vector3(m_dashDirection.x, m_dashDirection.y), Color.magenta);

        // Dash if necessary
        if (m_dashTimer < m_dashDuration)
        {
            this.transform.position = CheckCollision(   this.transform.position,
                                                        this.transform.position + new Vector3(m_dashDirection.x, m_dashDirection.y) * GameMgr.DeltaTime * m_dashMaxSpeed * m_dashSpeed.Evaluate(m_dashTimer / m_dashDuration));

            animate = true;
        }

        return animate;
    }

    // ======================================================================================
    private bool UpdateGravity()
    {
        // TODO : MAKE THIS WORLK PROPERY!
        m_gravSpeed += Physics.gravity.magnitude * m_gravityRatio;
        Vector3 finalPos = CheckCollision(this.transform.position,
                                            this.transform.position + GameMgr.DeltaTime * m_gravSpeed * Vector3.down);

        if (this.transform.position == finalPos)
        {
            m_gravSpeed = 0;
            return false;
        }

        this.transform.position = finalPos;

        return true;
    }

    // ======================================================================================
    private Vector3 CheckCollision(Vector3 _startPos, Vector3 _endPos)
    {
        RaycastHit hitInfo;
        Vector3 direction   = _endPos - _startPos;
        Vector3 finalEndPos = _endPos;

        if (direction.y < 0)
        {
            if (Physics.Raycast(_startPos, direction + m_collisionEpsilon * direction.normalized, out hitInfo, direction.magnitude + m_collisionEpsilon, ~(1 << this.gameObject.layer)))
            {
                Ground gnd = hitInfo.collider.gameObject.GetComponent<Ground>();

                if (gnd != null)
                {
                    finalEndPos.y = gnd.SurfaceY() + m_collisionEpsilon;
                }
            }
        }

        finalEndPos.x = Mathf.Clamp(finalEndPos.x, SceneMgr.MinX + m_width / 2, SceneMgr.MaxX - m_width / 2);
        finalEndPos.y = Mathf.Clamp(finalEndPos.y, SceneMgr.MinY, SceneMgr.MaxY - m_height);
        return finalEndPos;
    }

    // ======================================================================================
    private void UpdateAnimator()
    {
        switch (m_state)
        {
            case eStates.Idle:
                m_animator.SetBool(m_isDashingBoolParam, false);
                m_animator.SetBool(m_isFallingBoolParam, false);
                m_animator.SetBool(m_isJumpingBoolParam, false);
                m_animator.SetBool(m_isRunningBoolParam, false);
                break;

            case eStates.Walking:
                m_animator.SetBool(m_isDashingBoolParam, false);
                m_animator.SetBool(m_isFallingBoolParam, false);
                m_animator.SetBool(m_isJumpingBoolParam, false);
                m_animator.SetBool(m_isRunningBoolParam, true);
                break;

            case eStates.Jumping:
                m_animator.SetBool(m_isDashingBoolParam, false);
                m_animator.SetBool(m_isFallingBoolParam, false);
                m_animator.SetBool(m_isJumpingBoolParam, true);
                m_animator.SetBool(m_isRunningBoolParam, false);
                break;

            case eStates.Falling:
                m_animator.SetBool(m_isDashingBoolParam, false);
                m_animator.SetBool(m_isFallingBoolParam, true);
                m_animator.SetBool(m_isJumpingBoolParam, false);
                m_animator.SetBool(m_isRunningBoolParam, false);
                break;

            case eStates.Dashing:
                m_animator.SetBool(m_isDashingBoolParam, true);
                m_animator.SetBool(m_isFallingBoolParam, false);
                m_animator.SetBool(m_isJumpingBoolParam, false);
                m_animator.SetBool(m_isRunningBoolParam, false);
                break;
        }
    }
}

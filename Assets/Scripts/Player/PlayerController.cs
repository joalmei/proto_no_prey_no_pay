using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum eStates
    {
        Idle,
        Walking,
        Jumping,        // Normal || Wall
        Falling,
        Dashing,
        Sliding,        // Wall
        Grabbed
    }

    public enum ePlayer
    {
        Player1 = 1,
        Player2 = 2,
        Player3 = 3,
        Player4 = 4
    }


    // -------------------------------- PUBLIC ATTRIBUTES -------------------------------- //
    // BASIC
    [Header("Player")]
    public ePlayer          m_player                    = ePlayer.Player1;

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
    [ConditionalHide("m_useMomentum", true)]
    public float            m_walkAccDown               = 1;
    public bool             m_useMomentum               = true;

    [Header("Dash")]
    public AnimationCurve   m_dashSpeed                 = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
    public float            m_dashMaxSpeed              = 10;
    public float            m_dashDuration              = 0.1f;
    public float            m_dashCoolDownDuration      = 1;

    [Header("Jump")]
    public float            m_jumpInitSpeed             = 20;
    public float            m_jumpCooldownDuration      = 1;

    [Header("Wall Jump / Slide")]
    public float            m_wallBoostRatio            = .1f;
    public float            m_maxSlideSpeed             = 1f;
    public float            m_slideAcc                  = 15f;
    public float            m_jumpEjectSpeed            = 2f;

    [Header("Ledge Grab")]
    [Range(0,1)]
    public float            m_playerGrabExtraWidth      = 1;

    // SFX
    //[Header("SFX")]
    //public AudioSource      m_dashSFX;
    //public AudioSource      m_jetpackSFX;
    //public AudioSource      m_deathSFX;
    //public AudioSource      m_damageSFX;


    // --------------------------------- DEBUG IN EDITOR --------------------------------- //
    public bool             m_useDebugMode              = false;

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

    // LOCOMOTION: WALL SNAP


    // GENERAL
    private eStates         m_state;

    private int             m_nbLives                   = 3;

    private float           m_collisionEpsilon;

    // DEFINES
    private const float     MIN_SPEED_TO_MOVE           = 0.1f;
    private const float     GROUND_Y_VALUE_TO_DELETE    = -2.5f;

    // --------------------------------- DEBUG IN EDITOR --------------------------------- //
#if UNITY_EDITOR
    private Material        m_debugBallMat;
    private static Color    m_debugColorFalse = Color.red;
    private static Color    m_debugColorTrue  = Color.green;
#endif

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

        StartDebug(m_useDebugMode);
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
        float horizontal    = InputMgr.GetAxis((int) m_player,   InputMgr.eAxis.HORIZONTAL);    //Input.GetAxis("Horizontal");
        float vertical      = InputMgr.GetAxis((int) m_player,   InputMgr.eAxis.VERTICAL);      //Input.GetAxis("Vertical");
        bool  doJump        = InputMgr.GetButton((int) m_player, InputMgr.eButton.JUMP);        //Input.GetButtonDown("Jump");
        bool  doDash        = InputMgr.GetButton((int) m_player, InputMgr.eButton.DASH);        //Input.GetButtonDown("Dash");
        

        // update position
        UpdateTransform(horizontal, vertical, doJump, doDash);


        // OBS: UptadeAnimator MAYBE is OUT OF DATE!!!!
        // TODO: Test with anims and update if necessary
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

        int normal = 0;
        bool isWallSnapped  = CheckWalls(out normal);


        // clamp input
        if (isWallSnapped)
            _inputHorizontal = ( normal > 0 ) ? Mathf.Clamp(_inputHorizontal, 0, 1) : Mathf.Clamp(_inputHorizontal, -1, 0);

        // compute character control
        bool isJumping      = UpdateJump(_doJump, isWallSnapped);

        bool isWalking      = UpdateWalk(_inputHorizontal);

        bool isDashing      = UpdateDash(_inputHorizontal, _inputVertical, _doDash);
        
        UpdateGravity(isWallSnapped);
        
        UpdateCollisions(initialPos, this.transform.position);
        CheckLedge(initialPos, this.transform.position);

        Vector3 finalPos    = this.transform.position;
        Vector3 deltaPos    = finalPos - initialPos;

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
            if (isWallSnapped)
                nextState = eStates.Sliding;
            else
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
        float nextSpeed;

        if (!m_useMomentum)
        {
            nextSpeed = Mathf.Lerp(m_walkSpeed, m_maxWalkSpeed * _inputHorizontal, GameMgr.DeltaTime * m_walkAcc);
        }
        else
        {
            nextSpeed = m_walkSpeed;
            if (_inputHorizontal != 0)
                nextSpeed += _inputHorizontal * m_walkAcc * GameMgr.DeltaTime;
            else
                nextSpeed = Mathf.Lerp(nextSpeed, 0, m_walkAccDown * GameMgr.DeltaTime);

            nextSpeed = nextSpeed >= 0 ? Mathf.Clamp(nextSpeed, 0, m_maxWalkSpeed) : Mathf.Clamp(nextSpeed, -m_maxWalkSpeed, 0);
        }

        this.transform.position += Vector3.right * GameMgr.DeltaTime * nextSpeed;

        // Walking Anim State

        float nextSpeedMag      = Mathf.Abs(nextSpeed);
        float prevSpeedMag      = Mathf.Abs(m_walkSpeed);

        bool isStartingWalk     = nextSpeedMag > prevSpeedMag;

        m_walkSpeed             = nextSpeed;

        if (isStartingWalk && nextSpeedMag > m_minSpeedToStartWalkAnim || !isStartingWalk && nextSpeedMag > m_minSpeedToStopWalkAnim)
        {
            animate = true;
        }

        return animate;
    }

    // ======================================================================================
    private bool UpdateJump(bool _doJump, bool _wallSnap)
    {
        m_jumpCooldownTimer -= GameMgr.DeltaTime;

        // Init Jump
        if (_doJump && m_jumpCooldownTimer < 0 && ( m_state == eStates.Idle || m_state == eStates.Walking || m_state == eStates.Sliding) )
        {
            m_jumpCooldownTimer = m_dashCoolDownDuration;
            m_gravSpeed         = - m_jumpInitSpeed * (_wallSnap ? 1 + m_wallBoostRatio : 1.0f);
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
            this.transform.position = this.transform.position + new Vector3(m_dashDirection.x, m_dashDirection.y) * GameMgr.DeltaTime * m_dashMaxSpeed * m_dashSpeed.Evaluate(m_dashTimer / m_dashDuration);

            animate = true;
        }

        return animate;
    }

    // ======================================================================================
    private bool UpdateGravity(bool _wallSnap)
    {
        m_gravSpeed += Physics.gravity.magnitude * m_gravityRatio;

        if (_wallSnap && m_gravSpeed > 0)
        {
            m_gravSpeed = Mathf.Lerp(m_gravSpeed, m_maxSlideSpeed, m_slideAcc * Time.deltaTime);
        }

        this.transform.position = this.transform.position + GameMgr.DeltaTime * m_gravSpeed * Vector3.down;
        
        return true;
    }

    // ======================================================================================
    private bool UpdateCollisions(Vector3 _startPos, Vector3 _endPos)
    {
        Vector3 finalPos = CheckCollision(_startPos, _endPos);

        if (_startPos.y == finalPos.y)
        {
            m_gravSpeed = 0;
        }

        this.transform.position = finalPos;

        return true;
    }

    // ======================================================================================
    private void UpdateAnimator()
    {
        // OBS: UptadeAnimator MAYBE is OUT OF DATE!!!!
        // TODO: Test with anims and update if necessary

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

    // ======================================================================================
    private Vector3 CheckCollision(Vector3 _startPos, Vector3 _endPos)
    {
        Vector3 direction   = _endPos - _startPos;
        Vector3 finalEndPos = _endPos;

        if (direction.y < 0)
        {
            RaycastHit hitInfo;
            // check ground
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
    //private bool CheckWalls ()
    //{
    //    Vector3 lWall = this.transform.position - ( m_collisionEpsilon + m_width / 2 ) * Vector3.right;
    //    Vector3 rWall = this.transform.position + ( m_collisionEpsilon + m_width / 2 ) * Vector3.right;

    //    if (lWall.x <= SceneMgr.MinX || rWall.x >= SceneMgr.MaxX)
    //        return true;

    //    return Physics.Raycast(this.transform.position, -(m_collisionEpsilon - m_width / 2) * Vector3.right) || Physics.Raycast(this.transform.position, -(m_collisionEpsilon - m_width / 2) * Vector3.right + (m_collisionEpsilon + m_width / 2) * Vector3.right);
    //}

    // TODO: Jump in the wall's normal, not its tangent!
    // !!!!!!!!!! SIMPLER IDEA: Just jump in the opposite dir of input.x
    // because, if we are "wall snipped", it is in the opposite direction...
    // PROBLEM! It can be wall snapped, but with x == 0

    // ======================================================================================
    private bool CheckWalls(out int _normal)
    {
        _normal = 0;
        Vector3 lWall = this.transform.position - (m_collisionEpsilon + m_width / 2) * Vector3.right;
        Vector3 rWall = this.transform.position + (m_collisionEpsilon + m_width / 2) * Vector3.right;

        Vector3 center = this.transform.position + Vector3.up * (m_height / 2);
        Vector3 testEpsilon = (m_collisionEpsilon + m_width / 2) * Vector3.right;
        if (lWall.x <= SceneMgr.MinX || Physics.Raycast(center, -testEpsilon, ~(1 << this.gameObject.layer)))
        {
            // |->
            _normal = 1;
            return true;
        }
        else if (rWall.x >= SceneMgr.MaxX || Physics.Raycast(center, testEpsilon, ~(1 << this.gameObject.layer)))
        {
            // <-|
            _normal = -1;
            return true;
        }

        return false;
    }

    // ======================================================================================
    // checks tree points above the player to check if can grab
    private bool CheckLedge(Vector3 _startPos, Vector3 _endPos)
    {
        Vector3 direction   = _endPos - _startPos;
        Vector3 finalEndPos = _endPos;

        if (direction.y < 0)
        {
            RaycastHit hitInfo;
            // check ledge
            Vector3 center      = _startPos + Vector3.up * m_height;
            Vector3 centerLeft  = _startPos + Vector3.up * m_height - Vector3.right * (m_width / 2) * (1 + m_playerGrabExtraWidth);
            Vector3 centerRight = _startPos + Vector3.up * m_height + Vector3.right * (m_width / 2) * (1 + m_playerGrabExtraWidth);
            float testEpsilon   = m_collisionEpsilon + m_width / 2.0f;

            if (m_useDebugMode)
            {
                Debug.DrawRay(center, direction, Physics.Raycast(center, Vector3.down, direction.magnitude, ~(1 << this.gameObject.layer)) ? Color.white : Color.red);
                Debug.DrawRay(centerRight, direction, Physics.Raycast(centerRight, Vector3.down, direction.magnitude, ~(1 << this.gameObject.layer)) ? Color.white : Color.red);
                Debug.DrawRay(centerLeft, direction, Physics.Raycast(centerLeft, Vector3.down, direction.magnitude, ~(1 << this.gameObject.layer)) ? Color.white : Color.red);
            }

            if ((Physics.Raycast(center, Vector3.down, out hitInfo, testEpsilon, ~(1 << this.gameObject.layer)) && hitInfo.collider.gameObject.GetComponent<Ground>() != null) ||
                (Physics.Raycast(centerLeft, Vector3.down, out hitInfo, testEpsilon, ~(1 << this.gameObject.layer)) && hitInfo.collider.gameObject.GetComponent<Ground>() != null) ||
                (Physics.Raycast(centerRight, Vector3.down, out hitInfo, testEpsilon, ~(1 << this.gameObject.layer)) && hitInfo.collider.gameObject.GetComponent<Ground>() != null))
            {
#if UNITY_EDITOR
                if (m_useDebugMode)
                    m_debugBallMat.color = m_debugColorTrue;
#endif

                Ground gnd = hitInfo.collider.gameObject.GetComponent<Ground>();

                if (gnd != null)
                {
                    finalEndPos.y = gnd.SurfaceY() + m_collisionEpsilon - m_height;
                }
            }
#if UNITY_EDITOR
            else if (m_useDebugMode)
                m_debugBallMat.color = m_debugColorFalse;
#endif
        }

        if (_startPos.y == finalEndPos.y)
        {
            m_gravSpeed = 0;
        }

        this.transform.position = finalEndPos;

        return true;
    }

    
    // ======================================================================================
    // DEBUG METHODS
    // ======================================================================================
    private void StartDebug(bool _useDebug)
    {
        GameObject debugBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugBall.transform.parent = this.transform;
        debugBall.transform.position = this.transform.position + (m_height + .25f) * Vector3.up;
        debugBall.transform.localScale *= .2f;

        debugBall.GetComponent<Collider>().enabled = false;

        m_debugBallMat = debugBall.GetComponent<Renderer>().material;
        m_debugBallMat.shader = Shader.Find("GUI/Text Shader");
        m_debugBallMat.color = m_debugColorFalse;

        if (!_useDebug)
            debugBall.SetActive(false);
    }
}

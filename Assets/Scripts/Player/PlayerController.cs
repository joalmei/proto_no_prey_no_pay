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
        Sliding         // Wall
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
    private float           m_slideAcc                  = 15f;

    //COMBAT
    [Header("Combat")]
    [Header("General")]
    public List<GameObject> WeaponList                  = new List<GameObject>();
    public WeaponPickup.WeaponType       EquippedWeapon              = WeaponPickup.WeaponType.FISTS; //talvez mudar pra privado, talvez usar Enum (mas WeaponPickup.WeaponType está no weaponpickup)
    public GameObject       WeaponObject                = null; //n pensei num nome melhor, é o objeto do item q foi pegado
    public float            AttackCooldown              = 1f;
    public Vector2          ThrowOffset;

    [Header("Weapons - Punch")]
    public Vector2          PunchOffset;
    public Vector2          PunchHitboxSize;

    [Header("Weapons - Saber")]
    public Vector2          SaberOffset;
    public Vector2          SaberHitboxSize;
      
    [Header("Weapons - Pistol (in progress)")]
    public GameObject       ProjectilePrefab;
    public Vector2 PistolOffset;

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

    // LOCOMOTION: WALL SNAP

    // COMBAT

    private bool            isAttacking                 = false;
    
    [SerializeField]
    private LayerMask       playerLayer;

    // GENERAL
    private eStates         m_state;

    private int             m_nbLives                   = 3;

    private float           m_collisionEpsilon;

    // DEFINES
    private const float     MIN_SPEED_TO_MOVE           = 0.1f;
    private const float     GROUND_Y_VALUE_TO_DELETE    = -2.5f;

    private bool            grabButtonPressed           = false; //meio gambiarra mas n pensei numa maneira melhor de fazer

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
        float horizontal    = InputMgr.GetAxis((int) m_player,   InputMgr.eAxis.HORIZONTAL);    //Input.GetAxis("Horizontal");
        float vertical      = InputMgr.GetAxis((int) m_player,   InputMgr.eAxis.VERTICAL);      //Input.GetAxis("Vertical");
        bool  doJump        = InputMgr.GetButton((int) m_player, InputMgr.eButton.JUMP);        //Input.GetButtonDown("Jump");
        bool  doDash        = InputMgr.GetButton((int) m_player, InputMgr.eButton.DASH);        //Input.GetButtonDown("Dash");
        

        // update position
        UpdateTransform(horizontal, vertical, doJump, doDash);

        // get pick up item input
        if(InputMgr.GetButton((int) m_player, InputMgr.eButton.GRAB) && !grabButtonPressed){
            if(WeaponList.Count > 0){
                PickupWeapon();
            }
        }
        grabButtonPressed = InputMgr.GetButton((int) m_player, InputMgr.eButton.GRAB);

        // get throw item input
        if(InputMgr.GetButton((int) m_player, InputMgr.eButton.TOSS)){
            if(EquippedWeapon != WeaponPickup.WeaponType.FISTS){
                ThrowWeapon();
            }
        }

        // get attack input
        if(InputMgr.GetButton((int) m_player, InputMgr.eButton.ATTACK) && !isAttacking){
            isAttacking = true;
            switch(EquippedWeapon){
                case WeaponPickup.WeaponType.FISTS:
                    PunchAttack();
                break;
                case WeaponPickup.WeaponType.SABER:
                    SaberAttack();
                break;
                case WeaponPickup.WeaponType.PISTOL:
                    PistolAttack();
                break;
            }
        }



        // OBS: UptadeAnimator MAYBE is OUT OF DATE!!!!
        // TODO: Test with anims and update if necessary
        if (m_animator != null)
            UpdateAnimator();
    }

    // ======================================================================================
    public void TakeDamage()
    {
        print(m_player + " is taking damage");
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

    public void GetStunned(){
        print("GET STUNNED " + m_player);
    }

    // ======================================================================================
    // PRIVATE MEMBERS
    // ======================================================================================
    private void UpdateTransform(float _inputHorizontal, float _inputVertical, bool _doJump, bool _doDash)
    {
        // update transform
        Vector3 initialPos  = this.transform.position;

        bool isWallSnapped  = CheckWalls();

        bool isWalking      = UpdateWalk(_inputHorizontal);

        bool isDashing      = UpdateDash(_inputHorizontal, _inputVertical, _doDash);

        UpdateJump(_doJump, isWallSnapped);
        
        UpdateGravity(isWallSnapped);

        UpdateCollisions(initialPos, this.transform.position);

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
    private bool UpdateJump(bool _doJump, bool _wallSnap)
    {
        m_jumpCooldownTimer -= GameMgr.DeltaTime;

        // Init Dash
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

    private bool CheckWalls ()
    {
        Vector3 lWall = this.transform.position - ( m_collisionEpsilon + m_width / 2 ) * Vector3.right;
        Vector3 rWall = this.transform.position + ( m_collisionEpsilon + m_width / 2 ) * Vector3.right;

        if (lWall.x <= SceneMgr.MinX || rWall.x >= SceneMgr.MaxX)
            return true;

        return Physics.Raycast(this.transform.position, -(m_collisionEpsilon - m_width / 2) * Vector3.right) || Physics.Raycast(this.transform.position, -(m_collisionEpsilon - m_width / 2) * Vector3.right + (m_collisionEpsilon + m_width / 2) * Vector3.right);
    }

    // TODO: Jump in the wall's normal, not its tangent!
    // !!!!!!!!!! SIMPLER IDEA: Just jump in the opposite dir of input.x
    // because, if we are "wall snipped", it is in the opposite direction...
    // PROBLEM! It can be wall snapped, but with x == 0
    /*
    private bool CheckWalls(out int _normal)
    {
        _normal = 0;
        Vector3 lWall = this.transform.position - (m_collisionEpsilon + m_width / 2) * Vector3.right;
        Vector3 rWall = this.transform.position + (m_collisionEpsilon + m_width / 2) * Vector3.right;

        if (lWall.x <= SceneMgr.MinX || rWall.x >= SceneMgr.MaxX)
            return true;

        if (Physics.Raycast(this.transform.position, -(m_collisionEpsilon - m_width / 2) * Vector3.right))
        {
            // |->
            _normal = -1;
            return true;
        }
        else if (Physics.Raycast(this.transform.position, -(m_collisionEpsilon - m_width / 2) * Vector3.right + (m_collisionEpsilon + m_width / 2) * Vector3.right))
        {
            // <-|
            return true;
        }

        return false;
    }
    */

    private void PickupWeapon(){
        print("watch start");
        var watch = System.Diagnostics.Stopwatch.StartNew();

        int currWeapon = 0;
 
        float closestDist = Vector2.Distance(this.transform.position, WeaponList[0].transform.position);
        float currentDist;

        for(int i = 1; i < WeaponList.Count; i++){
            currentDist = Vector2.Distance(this.transform.position, WeaponList[i].transform.position);
            if(currentDist < closestDist){
                closestDist = currentDist;
                currWeapon = i;
            }
        }
        
        if(EquippedWeapon != WeaponPickup.WeaponType.FISTS){
            WeaponObject.transform.position = this.transform.position + new Vector3(0,0.25f,0);
            ToggleWeaponActive(WeaponObject, true);
            WeaponObject.GetComponent<Projectile>().MoveProjectileAtAngle();
        }
        EquippedWeapon = WeaponList[currWeapon].GetComponent<WeaponPickup>().weaponType;
        WeaponObject = WeaponList[currWeapon];
        WeaponList.Remove(WeaponObject);
        ToggleWeaponActive(WeaponObject, false);

        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        print("time elapsed " + elapsedMs);
    }

    private void ThrowWeapon(){
        //inicialmente só vou fazer dropar a arma
        WeaponObject.transform.position = transform.position + (Vector3)ThrowOffset;
        ToggleWeaponActive(WeaponObject, true);
        WeaponObject.GetComponent<Projectile>().MoveProjectile(transform.right * 5);
        EquippedWeapon = WeaponPickup.WeaponType.FISTS;
        WeaponObject = null;
    }

    private void ToggleWeaponActive(GameObject WeaponObject, bool status){
        WeaponObject.GetComponent<MeshRenderer>().enabled = status;
        WeaponObject.GetComponent<BoxCollider>().enabled = status;
    }

    private void PunchAttack(){
        Collider2D[] hitTargets = Physics2D.OverlapBoxAll(PunchOffset, PunchHitboxSize, 0, playerLayer);
        print("hit targets length " + hitTargets.Length);
        for(int i = 0; i < hitTargets.Length; i++){
            print("acertou " + hitTargets[i].name);
            hitTargets[i].GetComponent<PlayerController>().TakeDamage();
        }
    }

    private void SaberAttack(){
        Collider2D[] hitTargets = Physics2D.OverlapBoxAll(SaberOffset, SaberHitboxSize, 0, playerLayer);
        print("hit targets length " + hitTargets.Length);
        for(int i = 0; i < hitTargets.Length; i++){
            print("acertou " + hitTargets[i].name);
            hitTargets[i].GetComponent<PlayerController>().TakeDamage();
        }
    }

    private void PistolAttack(){
        // spawnar um projetil e mandar ele pra frente
        GameObject obj = Instantiate(ProjectilePrefab, transform.position + (Vector3)PistolOffset, Quaternion.identity);
        obj.GetComponent<Projectile>().MoveProjectile(transform.right * 5);
    }

    void OnDrawGizmosSelected(){
        // draws gizmos for punch and saber hitboxes
        if(EquippedWeapon == WeaponPickup.WeaponType.FISTS){
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(transform.position + (Vector3)PunchOffset, (Vector3)PunchHitboxSize);
        }
        if(EquippedWeapon == WeaponPickup.WeaponType.SABER){
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position + (Vector3)SaberOffset, (Vector3)SaberHitboxSize);
        }
        if(EquippedWeapon == WeaponPickup.WeaponType.PISTOL){
            Gizmos.color = Color.green;
            Gizmos.DrawCube(transform.position + (Vector3)PistolOffset, new Vector3(0.25f, 0.25f, 0));
        }
    }
}

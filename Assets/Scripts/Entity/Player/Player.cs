using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity
{
    [HideInInspector] public BoxCollider2D boxCd;
    [HideInInspector] public CircleCollider2D circleCd;
    [HideInInspector] public PolygonCollider2D traingleCd;

    public bool isGrounded;
    public float resetCd;

    public int traingleCount;
    public int squareCount; 
    public int circleCount;

    #region State
    public PlayerMoveState moveState;
    public PlayerDeadState deadState;
    public PlayerPauseState pauseState;
    #endregion

    [Header("Collision Info")]
    public Transform resetPosition;
    public Transform groundCheckL;
    public Transform groundCheckR;
    //public Transform wallCheck;
    public LayerMask enemy;
    public LayerMask ground;

    protected override void Awake()
    {
        base.Awake();

        moveState = new PlayerMoveState(this, stateMachine, "Move");    
        deadState = new PlayerDeadState(this, stateMachine, "Dead");
        pauseState = new PlayerPauseState(this, stateMachine, "Pause");
    }

    protected override void Start()
    {
        base.Start();

        traingleCount = 0;
        squareCount = 0;
        circleCount = 0;

        boxCd = GetComponent<BoxCollider2D>();
        circleCd = GetComponent<CircleCollider2D>();
        traingleCd = GetComponent<PolygonCollider2D>();

        stateMachine.Initialize(moveState);
    }

    protected override void Update()
    {
        base.Update();

    }

    public void PausePlayer()
    {
        stateMachine.ChangeState(pauseState);
    }

    public void StartPlayer()
    {
        stateMachine.ChangeState(moveState);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Entity entity = collision.gameObject.GetComponent<Entity>();

        if (entity.tag != this.tag && entity != null) 
        {
            bool shouldEliminateOther = (this.spriteCount == 0 && entity.spriteCount == 2) || (this.spriteCount == 2 && entity.spriteCount == 1) || (this.spriteCount == 1 && entity.spriteCount == 0);

            if (shouldEliminateOther)
            {
                Player player = entity as Player;
                if (player != null)
                {
                    player.stateMachine.ChangeState(player.deadState);  
                }
                else
                {   
                    entity.gameObject.layer = LayerMask.NameToLayer("Background");

                    entity.sr.enabled = false;

                    entity.ps.gameObject.SetActive(true);

                    Destroy(entity.gameObject, 5f);
                }

            }
        }
    }

    #region Collision
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundCheckL.transform.position, new Vector2(groundCheckL.transform.position.x, groundCheckL.transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(groundCheckR.transform.position, new Vector2(groundCheckR.transform.position.x, groundCheckR.transform.position.y - groundCheckDistance));
        //Gizmos.color = Color.white;
        //Gizmos.DrawLine(wallCheck.transform.position, new Vector2(wallCheck.transform.position.x + wallCheckDistance * facingDir, wallCheck.transform.position.y));
    }

    public virtual bool IsGroundDetected()
    {
        return Physics2D.Raycast(groundCheckL.transform.position, Vector2.down, groundCheckDistance, ground) || Physics2D.Raycast(groundCheckR.transform.position, Vector2.down, groundCheckDistance, ground);
    }

    //public bool IsWallDetected()
    //{

    //    return Physics2D.Raycast(wallCheck.transform.position, Vector2.right * facingDir, wallCheckDistance, ground);
    //}
    #endregion
}

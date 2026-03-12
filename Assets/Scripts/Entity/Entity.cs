using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class Entity : MonoBehaviour
{
    public int spriteCount;
    public bool isDead; 

    [Header("Move Info")]
    public float walkSpeed;
    //public float wallCheckDistance;
    //public int facingDir = 1;
    //[HideInInspector] public bool turnBack;

    [Header("Jump Info")]
    public float jumpForce;
    //public float jumpForceHolder;
    //public float jumpDuration;
    public float groundCheckDistance;

    #region Compoents
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;
    [HideInInspector] public SpriteRenderer sr;
    public ParticleSystem ps;
    public EntityStateMachine stateMachine;
    #endregion

    protected virtual void Awake()
    {

        stateMachine = new EntityStateMachine();
    }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        
        ps.gameObject.SetActive(false); 
    }

    protected virtual void Update()
    {

        stateMachine.currentState.Update();
    }

    protected virtual void FixedUpdate()
    {
        if (stateMachine != null && stateMachine.currentState != null)
            stateMachine.currentState.FixedUpdate();
    }

    public void UsingEnumerator(IEnumerator enumerator)
    {
        StartCoroutine(enumerator);
    }

    #region Flip

    //public void Flip(float _xInput)
    //{
    //    if (_xInput * facingDir == -1)
    //    {
    //        facingDir = -facingDir;
    //        transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
    //    }
    //    turnBack = true;
    //}

    //public void FlipOfEnemy()
    //{
    //    facingDir = -facingDir;
    //    transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
    //    turnBack = true;
    //}

    #endregion


}
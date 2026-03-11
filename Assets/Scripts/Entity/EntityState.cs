using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityState 
{
    protected Entity entity;
    protected EntityStateMachine stateMachine;
    protected Rigidbody2D rb;

    public float xInput;
    public float yInput;

    protected string animBoolName;
    public float timer;
    public bool isTriggered;

    public EntityState(Entity entity ,EntityStateMachine stateMachine, string animBoolName)
    {
        this.entity = entity;
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        rb = entity.rb;
        entity.anim.SetBool(animBoolName, true);
        timer = 0;
        isTriggered = false;
    }

    public virtual void Update()
    {
        timer -= Time.deltaTime;

        xInput = Input.GetAxisRaw("Horizontal");
    }

    public virtual void Exit()
    {
        entity.anim.SetBool(animBoolName, false);
    }
    protected void SetVelocity(float x, float y)
    {
        rb.velocity = new Vector2(x, y);
    }

    protected void CollisionClear()
    {

    }
}

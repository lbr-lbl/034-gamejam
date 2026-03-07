using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(Entity entity, EntityStateMachine stateMachine, string animBoolName) : base(entity, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.spriteCount = 1;
        player.anim.SetBool("Traingle", false);
        player.anim.SetBool("Square", true);
        player.anim.SetBool("Circle", false);
        player.boxCd.enabled = true;
        player.circleCd.enabled = false;
        player.traingleCd.enabled = false;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Space) && player.IsGroundDetected()) 
        {
            SetVelocity(xInput * player.walkSpeed, player.jumpForce);
        }
        else
        {
            SetVelocity(xInput * player.walkSpeed, player.rb.velocity.y);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            player.spriteCount++;

            if (player.spriteCount > 2) player.spriteCount = 0;

            if (player.spriteCount == 0)
            {
                ShapeChange("Traingle", player.traingleCd, true);
            }
            else if (player.spriteCount == 1)
            {
                ShapeChange("Square", player.boxCd, true);
            }
            else if (player.spriteCount == 2)
            {
                ShapeChange("Circle", player.circleCd, true);
            }
        }

        if (player.isDead)
        {
            Debug.Log("Player is Dead");
            stateMachine.ChangeState(player.deadState);
        }

    }

    private void ShapeChange(string shapeName, Collider2D cd, bool setTrue)
    {
        player.anim.SetBool("Traingle", false);
        player.anim.SetBool("Square", false);
        player.anim.SetBool("Circle", false);
        player.boxCd.enabled = false;
        player.circleCd.enabled = false;
        player.traingleCd.enabled = false;

        cd.enabled = true;
        player.anim.SetBool(shapeName, setTrue);
    }
}

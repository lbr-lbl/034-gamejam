using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static UnityEngine.InputSystem.InputAction;

public class PlayerMoveState : PlayerState
{
    private InputAction moveAction;
    private InputAction jumpAction;
    public Vector2 moveInput;

    public PlayerMoveState(Entity entity, EntityStateMachine stateMachine, string animBoolName) : base(entity, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 从 PlayerInput 获取动作（动作名称需与 .inputactions 资产中定义的一致）
        moveAction = player.playerInput.actions["Move"];
        jumpAction = player.playerInput.actions["Jump"];

        // 订阅事件
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove; // 松开时清零
        jumpAction.performed += OnJump;

        // 启用动作（PlayerInput 会自动管理，但显式启用可以确保生效）
        moveAction.Enable();
        jumpAction.Enable();

        // 原有 player.control 的代码全部移除
        // player.control.PlayerController.Enable();  // 删除

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

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
        jumpAction.performed -= OnJump;

        // 可以禁用动作，但通常由 PlayerInput 自动处理
        moveAction.Disable();
        jumpAction.Disable();

        // 删除 player.control 相关代码
        // player.control.PlayerController.Disable();
    }

    public override void Update()
    {
        base.Update();

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

        if (player.gameObject.transform.position.y < PlayerManager.instance.playerDeadZoneY)
        {
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

    public override void FixedUpdate()
    {
        SetVelocity(moveInput.x * player.walkSpeed, player.rb.velocity.y);
    }

    #region InputSystem

    // 输入回调
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (player.IsGroundDetected())
        {
            // 注意：这里直接设置 velocity，但可能需要在 FixedUpdate 中处理更合适
            // 简单起见可以调用 SetVelocity
            SetVelocity(player.rb.velocity.x, player.jumpForce);
        }
    }


    #endregion
}

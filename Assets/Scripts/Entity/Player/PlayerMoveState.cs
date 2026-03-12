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

        // �� PlayerInput ��ȡ������������������ .inputactions �ʲ��ж����һ�£�
        moveAction = player.playerInput.actions["Move"];
        jumpAction = player.playerInput.actions["Jump"];

        // �����¼�
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove; // �ɿ�ʱ����
        jumpAction.performed += OnJump;

        // ���ö�����PlayerInput ���Զ�����������ʽ���ÿ���ȷ����Ч��
        moveAction.Enable();
        jumpAction.Enable();

        // ԭ�� player.control �Ĵ���ȫ���Ƴ�
        // player.control.PlayerController.Enable();  // ɾ��

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

        // ���Խ��ö�������ͨ���� PlayerInput �Զ�����
        moveAction.Disable();
        jumpAction.Disable();

        // ɾ�� player.control ��ش���
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

    // ����ص�
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (player.IsGroundDetected())
        {
            // ע�⣺����ֱ������ velocity����������Ҫ�� FixedUpdate �д���������
            // ��������Ե��� SetVelocity
            SetVelocity(player.rb.velocity.x, player.jumpForce);
        }
    }


    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(Entity entity, EntityStateMachine stateMachine, string animBoolName) : base(entity, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.UsingEnumerator(WaitForReset());

        player.gameObject.layer = LayerMask.NameToLayer("Background");

        player.sr.enabled = false;

        player.ps.gameObject.SetActive(true);
    }

    public override void Exit()
    {
        base.Exit();

        player.gameObject.layer = LayerMask.NameToLayer("Player");

        player.sr.enabled = true;

        player.ps.gameObject.SetActive(false);
    }

    public override void Update()
    {
        base.Update();
    }

    IEnumerator WaitForReset()
    {
        yield return new WaitForSeconds(player.resetCd);

        player.transform.position = player.resetPosition.position;
        player.stateMachine.ChangeState(player.moveState);
    }   
}

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

        player.DropAllItemsOnDeath();
    }

    public override void Exit()
    {
        base.Exit();

        Debug.Log(111);

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

        // 使用玩家独立的复活点，而不是全局的 PlayerManager
        if (player.respawnPoint != null)
            player.transform.position = player.respawnPoint.position;
        else
            Debug.LogError($"玩家 {player.name} 的复活点未设置！");

        SetVelocity(0, 0);

        player.stateMachine.ChangeState(player.moveState);
    }   
}

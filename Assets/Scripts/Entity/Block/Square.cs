using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square : Block
{

    protected override void Start()
    {
        base.Start();

        spriteCount = 1;
    }

    protected override void Update()
    {
        if (isDead) stateMachine.ChangeState(deadState);
    }
}

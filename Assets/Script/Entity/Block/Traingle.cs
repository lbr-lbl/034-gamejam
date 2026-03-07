using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Traingle : Block
{

    protected override void Start()
    {
        base.Start();

        spriteCount = 0;
    }

    protected override void Update()
    {
        if(isDead)stateMachine.ChangeState(deadState);
    }
}

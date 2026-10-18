using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCoyoteTimeState : PlayerState
{

    public PlayerCoyoteTimeState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        parameter.rb.gravityScale = 0;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (parameter.isJump)
        {
            manager.TransitionState(StateType.Jump);
        }

        if (StateDuration >= parameter.coyoteTime || !parameter.isMove)
        {
            manager.TransitionState(StateType.Fall);
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }

    public override void OnExit()
    {
        base.OnExit();
        parameter.rb.gravityScale = parameter.Gravity;
    }
}


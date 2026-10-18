using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLandState : PlayerState
{
    public PlayerLandState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        //parameter.LandDown.SetActive(true);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        //if (PlayComplete())
        if(true)
        {
            manager.TransitionState(StateType.Idle);
        }
        if (parameter.HasJumpInputBuffer || parameter.isJump)
        {
            manager.TransitionState(StateType.Jump);
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }

    public override void OnExit()
    {
        base.OnExit();

    }

}


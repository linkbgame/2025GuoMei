using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallJumpFallState : PlayerState
{
    public PlayerWallJumpFallState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        //parameter.TakeClickStopJump = false;
        //Debug.Log(1);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (parameter.isClickMeleeAttack)
        {
            manager.TransitionState(StateType.MeleeAttack1);
        }
        if (parameter.isDash)
        {
            manager.TransitionState(StateType.Dash);
        }
        if (parameter.check.isTouchGround)
        {
           // parameter.LandDown.SetActive(true);
            manager.TransitionState(StateType.Land);
        }
        if (parameter.isWallSlide)
        {
            manager.TransitionState(StateType.WallSlide);
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        if (parameter.inputDirection.x == -parameter.facingDirection)
            Move();
            //¿ÉÒÔ·­×ª
            manager.FlipTo();
    }

    public override void OnExit()
    {
        base.OnExit();
        //parameter.TakeClickStopJump = true;
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        parameter.isClickMeleeAttack = false;
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
        if (parameter.isMove)
        {
            manager.TransitionState(StateType.Move);
        }
        if (parameter.isWallSlide)
        {
            manager.TransitionState(StateType.WallSlide);
        }
        if (parameter.isJump)
        {
            manager.TransitionState(StateType.Jump);
        }

        parameter.currentSpeed = Mathf.MoveTowards(parameter.currentSpeed, 0f, parameter.deceleration * Time.deltaTime);
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        SetVelocity(parameter.currentSpeed * parameter.facingDirection, parameter.rb.velocity.y);//¼õËÙ
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}


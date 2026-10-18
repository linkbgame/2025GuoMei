using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

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
        if (!parameter.isMove)
        {
            manager.TransitionState(StateType.Idle);
        }
        if (parameter.isWallSlide)
        {
            manager.TransitionState(StateType.WallSlide);
        }
        if (parameter.isJump)
        {
            manager.TransitionState(StateType.Jump);
        }
        if (parameter.isFall)
        {
            manager.TransitionState(StateType.CoyoteTime);
        }
        if (parameter.check.isTouchWall && !parameter.check.isTouchGround)
        {
            manager.TransitionState(StateType.WallSlide);
        }

        parameter.currentSpeed = Mathf.MoveTowards(parameter.currentSpeed, parameter.normalSpeed, parameter.acceration * Time.deltaTime);//º”ÀŸ
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        Move();
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}


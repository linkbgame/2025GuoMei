using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeAttack2State : PlayerState
{
    public PlayerMeleeAttack2State(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        parameter.isClickMeleeAttack = false;
        // parameter.currentSpeed = parameter.meleeAttackSpeed;
        //ÒÆ¶¯²¹³¥
        SetVelocity(parameter.attackMovePostion.x * parameter.facingDirection, parameter.attackMovePostion.y);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (parameter.isDash)
        {
            manager.TransitionState(StateType.Dash);
        }

        if (PlayComplete())
        {
            manager.TransitionState(StateType.Idle);
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        // Move();
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}


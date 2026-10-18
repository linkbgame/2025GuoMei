using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeAttack1State : PlayerState
{
    public PlayerMeleeAttack1State(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        if (!parameter.needMovePostion)
            manager.FlipTo();
        else return;
        parameter.isClickMeleeAttack = false;

        //移动补偿
        SetVelocity(parameter.attackMovePostion.x * parameter.facingDirection, parameter.attackMovePostion.y);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (parameter.isDash)
        {
            manager.TransitionState(StateType.Dash);
        }
        /*
        if (PlayComplete())
        {
            if (parameter.isClickMeleeAttack)
            {
                manager.TransitionState(StateType.MeleeAttack2);
            }
            else
            {
                manager.TransitionState(StateType.Idle);
            }

        }*/
        if (PlayComplete())
        {
            manager.TransitionState(StateType.Idle);
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }

    public override void OnExit()
    {
        base.OnExit();
        //攻击执行完可以转向，比如第二段攻击往后打，手感更好
        manager.FlipTo();

    }
}


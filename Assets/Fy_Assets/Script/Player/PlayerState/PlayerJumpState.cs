using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        parameter.HasJumpInputBuffer = false;
        parameter.isClickJump = false;

        parameter.currentJumpNum++;
        //Debug.Log(parameter.currentJumpNum);
        if (parameter.check.isTouchFlyGround)
        {
            SetVelocity(parameter.rb.velocity.x, parameter.jumpFlyForce);// 设置跳跃速度  
            parameter.FlyJumpShow.SetActive(true);
            parameter.isIntoJump = true;
        }
        else
        {
            SetVelocity(parameter.rb.velocity.x, parameter.jumpForce);// 设置跳跃速度  

        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (parameter.isClickMeleeAttack)
        {
            manager.TransitionState(StateType.MeleeAttack1);
        }
       // if (parameter.isClimb)
        {
        //    manager.TransitionState(StateType.ClimbBegin);
        }
        if (parameter.isDash)
        {
            manager.TransitionState(StateType.Dash);
        }
        if (parameter.isFall)
        {
            manager.TransitionState(StateType.Fall);
        }
        if (parameter.isWallSlide)
        {
            manager.TransitionState(StateType.WallSlide);
        }
        if (parameter.rb.velocity.y < 0.1f && parameter.check.isTouchGround)
        {
            manager.TransitionState(StateType.Land);
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        Move();
    }

    public override void OnExit()
    {
        base.OnExit();
        parameter.isIntoJump = false;

    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallSlideState : PlayerState
{
    public PlayerWallSlideState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        parameter.rb.gravityScale = 0;
        SetVelocity(0, 0);//速度清0
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (parameter.inputDirection.x==0)
        {
            //Debug.Log(parameter.inputDirection.x);
            manager.TransitionState(StateType.Fall);
        }
        if (parameter.isClickMeleeAttack)
        {
            manager.TransitionState(StateType.MeleeAttack1);
        }
        //if (parameter.isClimb)
        {
         //   manager.TransitionState(StateType.ClimbBegin);
        }
        if (parameter.check.isTouchGround&&parameter.inputDirection.x==0)
        {
            //parameter.LandDown.SetActive(true);
            manager.TransitionState(StateType.Idle);
        }
        if (parameter.isClickJump)
        {
            manager.TransitionState(StateType.WallJump);
        }
        if (!parameter.check.isTouchWall)
        {
            manager.TransitionState(StateType.Fall);
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        Move();
        //应用滑墙速度     按下加速下滑
        if (parameter.inputDirection.y < 0)
        {
            SetVelocity(0, parameter.wallSlideSpeedUp);// 限制垂直速度以应用墙壁滑行速度
        }
        else if(parameter.inputDirection.y == 0)
        {
            SetVelocity(0, parameter.wallSlideSpeed);
        }
        else
        {
            SetVelocity(0, parameter.wallSlideSpeed);
            //SetVelocity(0, parameter.jumpForce);//快速爬墙大人
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        parameter.rb.gravityScale = parameter.Gravity;
    }
}

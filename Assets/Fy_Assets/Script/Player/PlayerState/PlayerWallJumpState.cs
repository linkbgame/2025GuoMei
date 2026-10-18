using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallJumpState : PlayerState
{
    public PlayerWallJumpState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        parameter.HasJumpInputBuffer = false;
        parameter.isClickJump = false;
        //parameter.isIntoJump = true;
        //parameter.currentSpeed = parameter.normalSpeed;
        SetVelocity(parameter.rb.velocity.x, parameter.jumpForce);// 设置跳跃速度  
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (parameter.isClickMeleeAttack)
        {
            manager.TransitionState(StateType.MeleeAttack1);
        }
        if (!parameter.check.isTouchWall)
        {
            manager.TransitionState(StateType.Fall);
            //Debug.Log(1);
        }
        if (parameter.isDash)
        {
            manager.TransitionState(StateType.Dash);
        }
        if (parameter.isFall)
        {
            manager.TransitionState(StateType.WallJumpFall);
            //Debug.Log(2);
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        // 输入方向一定是面朝反方向才可控制方向，禁止单墙跳，且确保不输入可以从墙上弹开
        if (parameter.inputDirection.x == -parameter.facingDirection)
        {
            //Move();
        }
        else if (!parameter.isWallSlide)
        {
            //Move(); 
        }
        
    }

    public override void OnExit()
    {
        base.OnExit();
        //parameter.isIntoJump = false;
    }
}



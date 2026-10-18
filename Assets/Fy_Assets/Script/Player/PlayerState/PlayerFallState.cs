using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class PlayerFallState : PlayerState
{
    public PlayerFallState(PlayerFSM manager, string animationName) : base(manager, animationName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        //Debug.Log(1);
        //parameter.isIntoJump = false;
        //parameter.TakeClickStopJump = false;
        parameter.currentSpeed = parameter.jumpSpeed;
        //SetVelocity(parameter.rb.velocity.x, 0);//¿ªÆôºóÎÞÖÍ¿Õ
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (parameter.isClickMeleeAttack)
        {
            manager.TransitionState(StateType.MeleeAttack1);
        }
        //if (parameter.isClimb)
        {
           // manager.TransitionState(StateType.ClimbBegin);
        }
        
        if (parameter.isDash)
        {
            manager.TransitionState(StateType.Dash);
        }
        if (parameter.check.isTouchWall&&parameter.inputDirection.x!=0)
        {
            manager.TransitionState(StateType.WallSlide);
        }
        if (parameter.check.isTouchGround)
        {
            //parameter.LandDown.SetActive(true);
            manager.TransitionState(StateType.Land);
        }
        if (parameter.isJump&&!parameter.check.isTouchWall)
        {
            manager.TransitionState(StateType.Jump);
        }
        if(parameter.isJump && parameter.check.isTouchWall && parameter.inputDirection.x == 0)
        {
            manager.TransitionState(StateType.Jump);
        }
        if (parameter.isWallSlide)
        {
            manager.TransitionState(StateType.WallSlide);
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
       // parameter.TakeClickStopJump = true;
    }
}


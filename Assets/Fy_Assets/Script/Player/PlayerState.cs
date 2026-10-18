using System;
using UnityEngine;

[Serializable]
public class PlayerState : IState
{
    protected PlayerFSM manager;
    protected PlayerParameter parameter;
    protected string animationName;
    protected AnimatorStateInfo animatorStateInfo;   // 动画状态信息

    //状态切换计时器
    float stateStartTime;
    protected float StateDuration => Time.time - stateStartTime;

    public PlayerState(PlayerFSM manager, string animationName)
    {
        this.manager = manager;
        this.parameter = manager.parameter;
        this.animationName = animationName;
    }

    public virtual void OnEnter()
    {
        stateStartTime = Time.time;

        //将当前动画平滑过渡到名为animationName的动画状态，并且过渡持续时间为 0.1 秒
        Debug.Log(animationName);
        parameter.animator.CrossFade(animationName, 0.1f);
    }

    public virtual void OnUpdate()
    {
        animatorStateInfo = parameter.animator.GetCurrentAnimatorStateInfo(0);// 获取当前动画状态信息 

        if (parameter.rb.velocity.y < 0.1f && parameter.check.isTouchGround)
        {
            parameter.currentJumpNum = 0;//接触地面重置跳跃次数
        }
    }
    
    public virtual void OnFixedUpdate() { }

    public virtual void OnExit() { }

    //动画是否播放完成
    public bool PlayComplete()
    {
        // 当前动画是否播放了95%
        if (animatorStateInfo.normalizedTime >= .95f && animatorStateInfo.IsName(animationName)) return true;
        return false;
    }

    //移动
    public void Move()
    {
        // 根据输入方向移动角色
        SetVelocity(parameter.inputDirection.x * parameter.currentSpeed, parameter.rb.velocity.y);
        //翻转
        manager.FlipTo();
    }

    //设置速度
    public void SetVelocity(float setVelocityX, float setVelocityY)
    {
        parameter.rb.velocity = new Vector2(setVelocityX, setVelocityY);
    }
}




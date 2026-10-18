

//抽象基类，定义了所有状态类的基本结构

using System;
using UnityEngine;

public interface IState
{
    void OnEnter();// 进入状态时的方法
    void OnUpdate();// 更新方法
    void OnFixedUpdate();// 固定更新方法
    void OnExit();// 退出状态时的方法
}


// 定义状态类型枚举
public enum StateType
{
    Idle, //待机
    Move, //移动
    Dash, //冲锋
    MeleeAttack, //近战攻击
    Hit, //受击
    Death,//死亡
    Jump,//跳跃
    Fall, //下落
    Land,//落地
    WallSlide,//墙壁滑行
    WallJump, //蹬墙跳
    WallJumpFall, //蹬墙跳下落状态
    MeleeAttack1,//一段近战攻击状态
    MeleeAttack2,//二段近战攻击状态
    ClimbBegin,//开始攀爬
    Climbing,//攀爬中
    ClimbEnd,//攀爬结束
    CoyoteTime,//土狼时间
}
[Serializable]
public class Parameter
{
    [Header("属性")]
    [HideInInspector] public Animator animator;

    [HideInInspector] public SpriteRenderer sr;
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public PhysicsCheck check;

    [Header("受伤与死亡")]
    [HideInInspector] public bool isHurt; // 是否受伤
    [HideInInspector] public bool isDead; // 是否死亡
}



using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class PlayerParameter : Parameter
{
    [Header("移动")]
    [LabelText("默认移动速度")]
    public float normalSpeed = 6f; // 默认移动速度
    [LabelText("跳跃时的移动速度")]
    public float jumpSpeed = 4f; // 跳跃时的移动速度
    [LabelText("加速过渡值")]
    public float acceration = 40f;//加速过渡值
    [LabelText("减速过渡值")]
    public float deceleration = 40f;//减速过渡值
    [HideInInspector] public float Gravity;//重力
    [HideInInspector] public int facingDirection = 1; // 角色面向的方向，1右 -1左
    //[HideInInspector] public int facingRush = 1; // 角色面向的方向，1右 -1左
    [HideInInspector] public Vector2 inputDirection; // 输入的移动方向
    [HideInInspector] public float currentSpeed; // 当前移动速度
    //如果不触壁有输入 或者 触墙反方向移动才可以移动
    public bool isMove => (check.isTouchWall && inputDirection.x == -facingDirection) ||
                        (!check.isTouchWall && inputDirection.x != 0);


    [Header("跳跃")]
    [LabelText("跳跃力")]
    public float jumpForce;//跳跃力
    [LabelText("踩在空中方块时的跳跃力")]
    public float jumpFlyForce;//跳跃力
    [LabelText("跳跃次数")]
    public int jumpNum = 1;//跳跃次数
    [HideInInspector] public int currentJumpNum;//当前跳跃次数
    [LabelText("是否按下跳跃")]
    public bool isClickJump;//是否按下跳跃
    public bool isIntoJump;
    public GameObject FlyJumpShow;
    public bool isJump => isClickJump && currentJumpNum < jumpNum;
    [LabelText("停止跳跃标志")]
    public bool isClickStopJump;
    //[LabelText("自动停止跳跃标志")]
    //public bool TakeClickStopJump;
    public bool isFall => (rb.velocity.y < 0.1f && !check.isTouchGround) || (isClickStopJump && !check.isTouchGround);

    [Header("墙壁滑行")]
    [LabelText("滑行速度")]
    public float wallSlideSpeed = -1f;//滑行速度
    [LabelText("加速下滑速度")]
    public float wallSlideSpeedUp = -10f;//加速下滑
    public bool isWallSlide => check.isTouchWall  && facingDirection == inputDirection.x;

    //[Header("蹬墙跳")]
    //[LabelText("蹬墙跳时施加的力")]
    //public float wallJumpForce; // 蹬墙跳时施加的力

    [Header("近战攻击")]
    [LabelText("移动补偿")] public Vector2 attackMovePostion;//移动补偿
    [HideInInspector] public bool isClickMeleeAttack;//是否点击近战攻击
    [LabelText("是否需要移动补偿")] public bool needMovePostion;

    [Header("冲刺")]
    [LabelText("冲锋速度")]
    public Vector2 dashPostion;//冲锋速度
    [LabelText("冲锋时间")]
    public float dashTime = 0.5f; //冲锋时间
    [LabelText("冲锋CD")]
    public float dashCD = 1f; //冲锋CD
    [LabelText("冲锋残影，可做特效")]
    public GameObject shadowPrefab; //冲锋残影
    [HideInInspector] public bool isClickDash;
    [HideInInspector] public bool isDashing;//是否正在冲锋
    [HideInInspector] public bool isWaitDash;//是否CD冷却
    public bool isDash => isClickDash && !isDashing && !isWaitDash;

    //[Header("攀爬")]
    //public bool isClimbing;
    //public bool isClimb => check.isCheckClimb && inputDirection.y == 0;

    [Header("土狼时间")]
    public float coyoteTime = 0.1f;

    [Header("预输入")]
    [LabelText("跳跃输入缓冲时间")]
    public float waitJumpInputBufferTime = 0.2f;//跳跃输入缓冲时间
    [LabelText("是否存在跳跃缓冲")]
    public bool HasJumpInputBuffer;//是否存在跳跃缓冲


    [LabelText("向右旋转")]
    public bool needRXuan;//旋转右缓冲
    [LabelText("向左旋转")]
    public bool needLXuan;//旋转左缓冲
    [LabelText("长按旋转间隔")]
    public float timeToXuan;
    [HideInInspector] public bool hasDoXuan;
    //public bool 

    public GameObject LandDown;
}


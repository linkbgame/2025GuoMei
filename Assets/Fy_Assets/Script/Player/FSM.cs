using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class FSM : MonoBehaviour
{
    private IState currentState;        // 当前状态接口
    protected Dictionary<StateType, IState> states = new Dictionary<StateType, IState>();  // 状态字典，存储各种状态

    protected virtual void Awake()
    {
        TransitionState(StateType.Idle);    // 初始状态为Idle
    }

    protected virtual void OnEnable()
    {
        currentState.OnEnter();
    }

    protected virtual void Update()
    {
        currentState.OnUpdate();
    }

    protected virtual void FixedUpdate()
    {
        currentState.OnFixedUpdate();
    }

    // 状态转换方法
    public void TransitionState(StateType type)
    {
        if (currentState != null)
            currentState.OnExit();// 先调用退出方法

        currentState = states[type];    // 更新当前状态为指定类型的状态
        currentState.OnEnter();         // 调用新状态的进入方法
    }

    // 翻转角色
    public virtual void FlipTo() { }
}



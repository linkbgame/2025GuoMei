using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;


public class PlayerController : PlayerFSM
{
    @InputSystem_Actions inputSystem;
    [LabelText("炸弹x秒后爆炸")]
    public float BongTime;
    [HideInInspector] public float T_BongTime;
    [LabelText("炸弹释放CD")]
    public float TakeBongTime;
    protected override void Awake()
    {
        base.Awake();
        // 初始化输入控制
        inputSystem = new @InputSystem_Actions();
        inputSystem.Player.Move.performed += Move;
        inputSystem.Player.Move.canceled += StopMove;
        inputSystem.Player.Jump.started += Jump;
        inputSystem.Player.Jump.canceled += StopJump;
        inputSystem.Player.MeleeAttack.started += MeleeAttack;
        // inputSystem.Player.MeleeAttack.canceled += StopMeleeAttack;
        inputSystem.Player.Dash.started += Dash;
        inputSystem.Player.Dash.canceled += StopDash;
        inputSystem.Player.Rxuan.started += Rxuan;
        inputSystem.Player.Lxuan.started += Lxuan;
        inputSystem.Player.Rxuan.canceled += StopXuan;
        inputSystem.Player.Lxuan.canceled += Lxuan;
        SwitchActionMap(inputSystem.Player); // 切换到游戏操作的输入映射
    }

    void OnDisable()
    {
        // 禁用所有输入
        inputSystem.Disable();
    }

    // 切换操作映射
    public void SwitchActionMap(InputActionMap actionMap)
    {
        inputSystem.Disable(); // 禁用当前的输入映射
        actionMap.Enable(); // 启用新的输入映射
    }
    public void Rxuan(InputAction.CallbackContext context)
    {
        parameter.needRXuan = true;
    }
    public void Lxuan(InputAction.CallbackContext context)
    {
        parameter.needLXuan = true;
    }
    public void StopXuan(InputAction.CallbackContext context)
    {
        parameter.needLXuan = false;
        parameter.needRXuan = false;
        parameter.hasDoXuan = false;
    }
    public IEnumerator WaitNextXuan()
    {
        yield break;
    }
    //移动
    public void Move(InputAction.CallbackContext context)
    {
        parameter.inputDirection = inputSystem.Player.Move.ReadValue<Vector2>();
    }

    //停止移动
    public void StopMove(InputAction.CallbackContext context)
    {
        parameter.inputDirection = Vector2.zero;
    }
    // 跳跃
    public void Jump(InputAction.CallbackContext context)
    {
            //if (!parameter.TakeClickStopJump && parameter.currentJumpNum >= parameter.jumpNum)
            {
             //   parameter.isClickStopJump = true;
             //   parameter.isClickJump = false;
              //  return; 
            }
        parameter.isClickJump = true;
        parameter.isClickStopJump = false;
        //预输入计时
        StopCoroutine(PreInputExitTime());
        StartCoroutine(PreInputExitTime());

    }

    IEnumerator PreInputExitTime()
    {
        Debug.Log(1);
        parameter.HasJumpInputBuffer = true;
        yield return new WaitForSeconds(parameter.waitJumpInputBufferTime); // 等待
        parameter.HasJumpInputBuffer = false;
    }

    //停止移动
    public void StopJump(InputAction.CallbackContext context)
    {
        parameter.isClickStopJump = true;
        parameter.isClickJump = false;
    }

    //近战攻击
    public void MeleeAttack(InputAction.CallbackContext context)
    {
        parameter.isClickMeleeAttack = true;
        parameter.needMovePostion = false;
    }
    private void Dash(InputAction.CallbackContext context)
    {
        parameter.isClickDash = true;
    }

    private void StopDash(InputAction.CallbackContext context)
    {
        parameter.isClickDash = false;
    }
    [Header("玩家攻击相关数据")]


    [LabelText("攻击内置CD")]
    public float hitCD = 0.2f;
    float CD = 0.2f;

    protected override void Update()
    {
        base.Update();
        CD -= Time.deltaTime;
        T_BongTime -= Time.deltaTime;

        if (parameter.isClickStopJump)
        {
            parameter.FlyJumpShow.SetActive(false);
        }
        if (Input.GetMouseButtonDown(0))
        {
            parameter.isClickMeleeAttack = true;
            parameter.needMovePostion = false;
        }
        if (Input.GetMouseButtonUp(0))
        {
            parameter.isClickMeleeAttack = false;
            parameter.needMovePostion = true;
        }

    }

}



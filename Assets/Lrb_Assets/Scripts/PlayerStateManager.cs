using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    // 跟踪玩家是否被锁定移动
    public bool IsMovementLocked { get; private set; }

    // 供外部组件调用的方法，用于请求锁定或解锁移动
    public void SetMovementLock(bool isLocked, object source)
    {
        // 可以增加更复杂的逻辑，例如追踪是谁锁定的 (source)
        IsMovementLocked = isLocked;

        // 查找并控制 PlayerMovement 组件
        // 注意：这里是唯一耦合 PlayerMovement 的地方，但其他组件不再耦合。
        GetComponent<PlayerMove>().enabled = !isLocked;

        // 如果有 PlayerShooting 等组件，也可以在这里统一控制
        // GetComponent<PlayerShooting>().enabled = !isLocked;
    }
}

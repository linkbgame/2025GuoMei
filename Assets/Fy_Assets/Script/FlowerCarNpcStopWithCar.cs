using UnityEngine;

/// <summary>
/// 花车沿路 <see cref="CarMove"/> 停止时（<c>Stop()</c>、或 <c>loop==false</c> 走到终点），
/// 让车上 NPC 的 <see cref="NpcScriptedMove"/> 同步 <see cref="NpcScriptedMove.Stop"/>，避免脚本化位移继续拖走角色。
/// 挂在花车根或与 <see cref="CarMove"/> 同层级即可；未手动指定时在子物体中自动查找 <see cref="NpcScriptedMove"/>。
/// </summary>
public sealed class FlowerCarNpcStopWithCar : MonoBehaviour
{
    [SerializeField] private CarMove carMove;
    [Tooltip("留空则自动收集子物体上所有 NpcScriptedMove；可改为只填需要同步的 NPC。")]
    [SerializeField] private NpcScriptedMove[] npcScriptedMoves;

    bool _hasPrevious;
    bool _wasPathPlaying;

    void Awake()
    {
        if (carMove == null)
        {
            carMove = GetComponent<CarMove>();
            if (carMove == null)
                carMove = GetComponentInParent<CarMove>();
            if (carMove == null)
                carMove = GetComponentInChildren<CarMove>(true);
        }

        if (npcScriptedMoves == null || npcScriptedMoves.Length == 0)
            npcScriptedMoves = GetComponentsInChildren<NpcScriptedMove>(true);

        if (carMove == null)
            Debug.LogWarning("[FlowerCarNpcStopWithCar] 未找到 CarMove，请在 Inspector 指定或挂在含 CarMove 的花车层级上。", this);
    }

    void LateUpdate()
    {
        if (carMove == null)
            return;

        bool playing = carMove.IsPathPlaying;
        if (_hasPrevious && _wasPathPlaying && !playing && npcScriptedMoves != null)
        {
            for (int i = 0; i < npcScriptedMoves.Length; i++)
            {
                if (npcScriptedMoves[i] != null)
                    npcScriptedMoves[i].Stop();
            }
        }

        _wasPathPlaying = playing;
        _hasPrevious = true;
    }
}

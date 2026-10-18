using UnityEngine;

/// <summary>
/// 与 <see cref="Animator"/> 挂在同一物体上。枯萎 Clip 的 Animation Event 只会在该物体上查找方法，
/// 由此转发到父级 <see cref="FlowerCarInteract.NotifyWitherAnimationFinished"/>。
/// </summary>
public sealed class FlowerCarWitherAnimEventRelay : MonoBehaviour
{
    FlowerCarInteract _flowerCar;

    void Awake()
    {
        _flowerCar = GetComponentInParent<FlowerCarInteract>();
    }

    public void NotifyWitherAnimationFinished()
    {
        if (_flowerCar != null)
            _flowerCar.NotifyWitherAnimationFinished();
    }
}

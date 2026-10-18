using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderBinder : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;

    private void Awake()
    {
        if (volumeSlider == null)
            volumeSlider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        TryBind();
    }

    private void OnDisable()
    {
        if (volumeSlider != null && BGMManager.Instance != null)
            volumeSlider.onValueChanged.RemoveListener(BGMManager.Instance.SetBGMSliderVolume);
    }

    private void TryBind()
    {
        var bgm = BGMManager.Instance;
        if (bgm == null || volumeSlider == null) return;

        // 防止重复绑定
        volumeSlider.onValueChanged.RemoveListener(bgm.SetBGMSliderVolume);
        volumeSlider.onValueChanged.AddListener(bgm.SetBGMSliderVolume);

        // 初始化 UI 显示到当前真实值
        volumeSlider.SetValueWithoutNotify(bgm.GetMasterBGMVolume());
    }
}

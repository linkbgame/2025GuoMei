using System;
using UnityEngine;

public class Settings
{
    public const float itemFadeDuration = 0.35f;
    public const float targetAlpha = 0.45f;

    //时间相关
    public const float secondThreshold = 0.01f;    //数值越小时间越快
    public const int secondHold = 59;
    public const int minuteHold = 59;
    public const int hourHold = 23;
    public const int dayHold = 30;

    //NPC网格移动
    public const float gridCellSize = 1;
    public const float gridCellDiagonalSize = 1.41f;
    public const float pixelSize = 0.05f;   //20*20 占 1 unit
    public const float animationBreakTime = 5f; //动画间隔时间
    public const int maxGridSize = 9999;
}
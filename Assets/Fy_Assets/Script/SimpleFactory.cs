using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
///<summary>
//简单工厂
///</summary>
public static class SimpleFactory
{
    //创建Resources/Model中的物体
    public static GameObject CreateModel(string name, Transform parent)
    {
        return Object.Instantiate(Resources.Load("" + name), parent) as GameObject;
    }
    public static GameObject CreateModelDonw(string name, Transform parent)
    {
        return Object.Instantiate(Resources.Load("" + name), parent) as GameObject;
    }
    public static TextAsset FindText(string name)
    {
        return Resources.Load("Read/" + name) as TextAsset;
    }
    public static Sprite FindImageChuPu(string name)
    {
        Texture2D it = Resources.Load("ChuPu/" + name) as Texture2D;
        return Sprite.Create(it, new Rect(0, 0, it.width, it.height), Vector2.zero);
    }
}

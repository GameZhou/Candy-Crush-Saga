using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public enum CandyType
{
    Ordinary,//普通
    Diamond,//菱形爆炸
    Vertical,//纵向
    Horizontal,//横向
    AllIn,//全消
}

public class CandyControl : MonoBehaviour
{
    public CandyType candyType;//糖果类型
    GameManager GM;
    public int color;
    private void Awake()
    {
        GM = GameManager.instance;
    }
    public void SetCandyType(CandyType candyType)
    {
        this.candyType = candyType;
        GetComponent<SpriteRenderer>().sprite = GM.GetCandyTexture(color, candyType); ;
    }
    public void Initialization(Sprite candySprite,CandyType candyType,int color)
    {
        this.candyType = candyType;
        this.color = color;
        GetComponent<SpriteRenderer>().sprite = candySprite;
    }
    public void Delete()
    {
        Destroy(gameObject);
    }
}

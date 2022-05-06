using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square : MonoBehaviour
{
    public Vector2Int index;//格子的坐标
    //初始化格子所需要记录的数据
    public void Initialization(int row, int column)
    {
        index = new Vector2Int(row, column);
    }
}

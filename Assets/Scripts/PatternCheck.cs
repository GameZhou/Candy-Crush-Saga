using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternCheck
{
    List<byte[,]> patternList = new List<byte[,]>();
    List<Vector2Int[]> positionArrList;
    /// <summary>
    /// 用二维数组表示需要检测的图案
    /// 0为任意，1表示相同
    /// 不需要添加旋转后的同样图案
    /// </summary>
    /// <param name="patterArray">需要检测的图案</param>
    public void Add(byte[,] patterArray)
    {
        patternList.Add(patterArray);
    }
    //添加图案之后统一缓存需要比较的坐标点
    //将每一个图案本身及三次旋转90°的结果存储
    public void SetPosition()
    {
        positionArrList = new List<Vector2Int[]>();
        foreach (var item in patternList)
        {
            positionArrList.Add(GetPositionArr(item));
            positionArrList.Add(GetPositionArr(RotateArray(item)));
            positionArrList.Add(GetPositionArr(RotateArray(item)));
            positionArrList.Add(GetPositionArr(RotateArray(item)));
        }
    }
    //遍历整个棋盘，以每个点为起坐标点判断以该点为起点是否存在符合图形匹配结果的组合
    public bool CheckPatternByPosition(int[,] map)
    {
        for (int y = 0; y < map.GetLength(0); y++)
        {
            for (int x = 0; x < map.GetLength(1); x++)
            {
                if (CheckPattern(map, y, x))
                {
                    return true;
                }
            }
        }
        return false;
    }
    //输入对比坐标起点
    //如果表中其中一个图形符合条件，将flag设置为T
    bool CheckPattern(int[,] map, int mY, int mX)
    {
        bool flag0 = false;
        foreach (var item in positionArrList)
        {
            if (Check(map, item, mY, mX))
            {
                flag0 = true;
                break;
            }
        }
        return flag0;
    }
    //对比图形，如果其中一个颜色不符合则返回F，否则为T
    bool Check(int[,] map, Vector2Int[] posArry, int mY, int mX)
    {
        bool flag = true;
        int indexColor, Y, X;
        Y = posArry[0].x + mY;
        X = posArry[0].y + mX;
        if (Y >= 0 && X >= 0 && Y < map.GetLength(0) && X < map.GetLength(1))
        {
            indexColor = map[Y, X];
            for (int i = 1; i < posArry.Length; i++)
            {
                Y = posArry[i].x + mY;
                X = posArry[i].y + mX;
                if (Y >= 0 && X >= 0 && Y < map.GetLength(0) && X < map.GetLength(1))
                {
                    if (map[Y, X] != indexColor)
                    {
                        flag = false;
                        break;
                    }
                }
                else
                {
                    flag = false;
                    break;
                }
            }
        }
        else
        {
            flag = false;
        }
        return flag;
    }
    //旋转二维数组并返回旋转结果
    byte[,] RotateArray(byte[,] pattern)
    {
        byte[,] temp = new byte[pattern.GetLength(1), pattern.GetLength(0)];
        for (int y = 0; y < pattern.GetLength(0); y++)
        {
            for (int x = 0; x < pattern.GetLength(1); x++)
            {
                temp[x, pattern.GetLength(0) - 1 - y] = pattern[y, x];
            }
        }
        return temp;
    }
    //提取值为1的坐标点
    Vector2Int[] GetPositionArr(byte[,] pattern)
    {
        List<Vector2Int> tempList = new List<Vector2Int>();
        for (int y = 0; y < pattern.GetLength(0); y++)
        {
            for (int x = 0; x < pattern.GetLength(1); x++)
            {
                if (pattern[y, x] == 1)
                {
                    tempList.Add(new Vector2Int(y, x));
                }
            }
        }
        return tempList.ToArray();
    }
}

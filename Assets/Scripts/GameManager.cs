using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
/* 
 * 1、所有二维数据坐标为了适应二维数组统一为（Y，X）
 * 2、棋盘数据：i<0代表不可操作/挖空，0~n代表颜色，max代表空可填充，糖果数代表Allin，因为是所有颜色。
 * 3、temp数据：（x,y）x为横向连续数，y为纵向连续数
 */
public enum GameState
{
    Play,//等待玩家操作
    Match,//无操作结算
    Clear,//删除可合成糖果
    CreateMatchCandy,//生成合成结果
    Down,//下落
    Shuffle,//洗牌
    Anima,//动画状态
}
public struct CandyInfo
{
    public int color;
    public Vector2Int index;
    public CandyType candyType;
    public CandyInfo(int color, Vector2Int index, CandyType candyType)
    {
        this.color = color;
        this.index = index;
        this.candyType = candyType;
    }
}
public class GameManager : MonoBehaviour
{
    public static GameManager instance; //单例
    public int diamondRadius = 1;

    int animaCounter;//动画计数器
    GameState gameState;//游戏当前状态
    GameState gameStateNext;//游戏下一个状态

    RaycastHit2D hit;//用于接收射线检测信息
    bool isDrag = false;//拖拽标志位
    Vector3 startPos;//鼠标按下时的位置
    Vector3 deltaPos;//鼠标偏移量
    Vector2Int swapIndex0;//被选中的糖果的坐标
    Vector2Int swapIndex1;//鼠标拖拽方向上的隔壁糖果

    PatternCheck patternCheck;//图形匹配检测，用于检查是否存在最小合成情况
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        LoadData();
        CreatePad();
        gameState = GameState.Down;
    }
    void Update()
    {
        switch (gameState)
        {
            case GameState.Play:
                Play();
                break;
            case GameState.Match:
                Match();
                break;
            case GameState.Clear:
                Clear();
                break;
            case GameState.CreateMatchCandy:
                CreateMatchCandy();
                break;
            case GameState.Down:
                Down();
                break;
            case GameState.Shuffle:
                Shuffle();
                break;
            case GameState.Anima:
                Anima();
                break;
            default:
                break;
        }
    }

    public int boardWidth = 7;              //棋盘列数
    public int boardHeight = 8;             //棋盘行数
    public Transform squareparent;//格子的父物体

    public int candySpritesCount = 6;//素材数量=糖果种类数
    string candySpritesPath = "Textures/Items/item_0";//普通贴图文件路径前缀
    string candyDiamondSuffix = "_extra";//菱形爆炸贴图文件后缀
    string candyVerticalSuffix = "_stripes_vert";//纵向爆炸贴图文件后缀
    string candyHorizontalSuffix = "_stripes_horiz";//横向爆炸贴图文件后缀
    string candyAllInPath = "Textures/Items/candy10_choco";//全消贴图文件路径
    int[,] candyColorInBoard;//记录棋盘上的糖果颜色
    CandyControl[,] candiesInBoard;//记录糖果物体
    List<Sprite> ordinaryCandySprites = new List<Sprite>();//当局普通糖果的贴图文件集
    GameObject squarePrefab;//格子的预制体
    GameObject candyPrefab;//糖果的预制体
    List<GameObject> explosionEffects;

    //获取所需要的数据
    void LoadData()
    {
        candiesInBoard = new CandyControl[boardHeight, boardWidth];
        patternCheck = new PatternCheck();
        //添加需要检测的图形并进行缓存
        patternCheck.Add(new byte[,] {
            {1,1,0 },
            {0,0,1 },
        });
        patternCheck.Add(new byte[,] {
            {0,0,1 },
            {1,1,0 },
        });
        patternCheck.Add(new byte[,] {
            {0,1,0 },
            {1,0,1 },
        });
        patternCheck.Add(new byte[,] {
            {1,1,0,1 },
        });
        patternCheck.SetPosition();
        candyColorInBoard = new int[boardHeight, boardWidth];
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                candyColorInBoard[y, x] = int.MaxValue;
            }
        }
        for (int i = 1; i <= candySpritesCount; i++)
        {
            ordinaryCandySprites.Add(Resources.Load<Sprite>(candySpritesPath + i));
        }
        explosionEffects = new List<GameObject>();
        for (int i = 1; i <= candySpritesCount; i++)
        {
            explosionEffects.Add(Resources.Load<GameObject>("Prefabs/Effect0" + i));
        }
        squareparent = GameObject.Find("GameField").transform;
        squarePrefab = Resources.Load<GameObject>("Prefabs/Square");
        candyPrefab = Resources.Load<GameObject>("Prefabs/Candy");
    }

    public float widthDistance = 1.2f;       //两个格子中心点偏移距离
    public float heightDistance = 1.2f;       //两个格子中心点偏移距离
    //生成新的棋盘
    void CreatePad()
    {
        Sprite square1 = Resources.Load<Sprite>("Textures/Blocks/square1");
        Sprite square2 = Resources.Load<Sprite>("Textures/Blocks/square2");
        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                //生成格子
                GameObject squareGO = Instantiate(squarePrefab, squareparent.position + new Vector3(j * widthDistance, -i * heightDistance), Quaternion.identity);
                squareGO.GetComponent<Square>().Initialization(i, j);
                if ((i * boardWidth + j) % 2 == 0)
                    squareGO.GetComponent<SpriteRenderer>().sprite = square1;
                else
                    squareGO.GetComponent<SpriteRenderer>().sprite = square2;
                squareGO.transform.parent = squareparent;
            }
        }
    }
    
    public void AnimaPlayEnd()
    {
        animaCounter--;
    }
    public void AddAnima()
    {
        animaCounter++;
    }
    void Anima()
    {
        if (animaCounter == 0)
        {
            gameState = gameStateNext;
        }
    }
    void Shuffle()
    {
        int H = candyColorInBoard.GetLength(0);
        int W = candyColorInBoard.GetLength(1);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int rand = Random.Range(y * W + x, W * H);

                int temp = candyColorInBoard[y, x];
                candyColorInBoard[y, x] = candyColorInBoard[rand / W, rand % W];
                candyColorInBoard[rand / W, rand % W] = temp;

                Vector3 tempVec = candiesInBoard[y, x].transform.position;
                candiesInBoard[y, x].transform.position = candiesInBoard[rand / W, rand % W].transform.position;
                candiesInBoard[rand / W, rand % W].transform.position = tempVec;

                CandyControl tempCC = candiesInBoard[y, x];
                candiesInBoard[y, x] = candiesInBoard[rand / W, rand % W];
                candiesInBoard[rand / W, rand % W] = tempCC;
            }
        }
        gameState = GameState.Match;
    }
    void Play()
    {
        if (Input.GetMouseButtonDown(0))//按下鼠标左键
        {
            //从屏幕上鼠标的位置发射一条射线，检测碰撞到的是否是所需要的物体
            hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.transform != null && hit.transform.GetComponent<Square>())
            {
                swapIndex0 = hit.transform.GetComponent<Square>().index;
                if (IsCandy(swapIndex0.x, swapIndex0.y))
                {
                    isDrag = true;
                    startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDrag = false;
        }
        if (isDrag)
        {
            //计算鼠标偏移量，根据偏移量计算拖拽方向
            deltaPos = startPos - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (Vector3.Magnitude(deltaPos) > 0.1f)
            {
                if (Mathf.Abs(deltaPos.x) > Mathf.Abs(deltaPos.y) && deltaPos.x > 0)//和左边的交换
                    swapIndex1 = new Vector2Int(swapIndex0.x, swapIndex0.y - 1);
                else if (Mathf.Abs(deltaPos.x) > Mathf.Abs(deltaPos.y) && deltaPos.x < 0)//和右边的交换
                    swapIndex1 = new Vector2Int(swapIndex0.x, swapIndex0.y + 1);
                else if (Mathf.Abs(deltaPos.x) < Mathf.Abs(deltaPos.y) && deltaPos.y > 0)//和下面的交换
                    swapIndex1 = new Vector2Int(swapIndex0.x + 1, swapIndex0.y);
                else if (Mathf.Abs(deltaPos.x) < Mathf.Abs(deltaPos.y) && deltaPos.y < 0)//和上面的交换
                    swapIndex1 = new Vector2Int(swapIndex0.x - 1, swapIndex0.y);
                //标记位更新
                isDrag = false;
                if (IsCandy(swapIndex1.x, swapIndex1.y))
                {
                    Sequence s1 = DOTween.Sequence();
                    Sequence s2 = DOTween.Sequence();
                    //数据记录更新交换
                    int tempColor;
                    tempColor = candyColorInBoard[swapIndex0.x, swapIndex0.y];
                    candyColorInBoard[swapIndex0.x, swapIndex0.y] = candyColorInBoard[swapIndex1.x, swapIndex1.y];
                    candyColorInBoard[swapIndex1.x, swapIndex1.y] = tempColor;

                    //图形引用更新
                    CandyControl tempCandy;
                    tempCandy = candiesInBoard[swapIndex0.x, swapIndex0.y];
                    candiesInBoard[swapIndex0.x, swapIndex0.y] = candiesInBoard[swapIndex1.x, swapIndex1.y];
                    candiesInBoard[swapIndex1.x, swapIndex1.y] = tempCandy;

                    Vector2 vec0 = candiesInBoard[swapIndex0.x, swapIndex0.y].transform.position;
                    Vector2 vec1 = candiesInBoard[swapIndex1.x, swapIndex1.y].transform.position;
                    //匹配场上的元素，如果有匹配结果，进入删除状态，否则复位。
                    if (Swap())
                    {
                        //调用对应图形动作
                        s1.Append(candiesInBoard[swapIndex0.x, swapIndex0.y].transform.DOMove(vec1, 0.5f));
                        s1.OnComplete(() => { AnimaPlayEnd(); });
                        s2.Append(candiesInBoard[swapIndex1.x, swapIndex1.y].transform.DOMove(vec0, 0.5f));
                        s2.OnComplete(() => { AnimaPlayEnd(); });
                        animaCounter += 2;


                        gameState = GameState.Anima;
                        gameStateNext = GameState.Clear;
                    }
                    else
                    {
                        s1.Append(candiesInBoard[swapIndex0.x, swapIndex0.y].transform.DOMove(vec1, 0.5f));
                        s1.Append(candiesInBoard[swapIndex0.x, swapIndex0.y].transform.DOMove(vec0, 0.5f));
                        s1.OnComplete(() => { AnimaPlayEnd(); });
                        s2.Append(candiesInBoard[swapIndex1.x, swapIndex1.y].transform.DOMove(vec0, 0.5f));
                        s2.Append(candiesInBoard[swapIndex1.x, swapIndex1.y].transform.DOMove(vec1, 0.5f));
                        s2.OnComplete(() => { AnimaPlayEnd(); });
                        animaCounter += 2;
                        //数据复位
                        tempColor = candyColorInBoard[swapIndex0.x, swapIndex0.y];
                        candyColorInBoard[swapIndex0.x, swapIndex0.y] = candyColorInBoard[swapIndex1.x, swapIndex1.y];
                        candyColorInBoard[swapIndex1.x, swapIndex1.y] = tempColor;

                        //图形引用更新
                        tempCandy = candiesInBoard[swapIndex0.x, swapIndex0.y];
                        candiesInBoard[swapIndex0.x, swapIndex0.y] = candiesInBoard[swapIndex1.x, swapIndex1.y];
                        candiesInBoard[swapIndex1.x, swapIndex1.y] = tempCandy;

                        gameState = GameState.Anima;
                        gameStateNext = GameState.Play;
                    }
                }
            }
        }

    }

    void Clear()
    {
        while (delPositionListTemp.Count != 0)
        {
            foreach (var item in delPositionListTemp)
            {
                delPositionList.Add(item);
            }
            delPositionListTemp.Clear();
            for (int i = 0; i < delPositionList.Count; i++)
            {
                DestroyCandyUnclassified(delPositionList[i]);
            }
            delPositionList.Clear();
        }
        gameState = GameState.Anima;
        gameStateNext = GameState.CreateMatchCandy;
    }
    void CreateMatchCandy()
    {
        foreach (var item in createInfoList)
        {
            CreateCandy(item.index.x, item.index.y, item.color, item.candyType);
        }
        createInfoList.Clear();
        gameState = GameState.Down;
    }
    void Match()
    {
        if (HaveMatch())
        {
            gameState = GameState.Clear;
        }
        else
        {
        //如果检测是否存在最小可操作情况的结果为不存在，跳转到洗牌状态，否则才进入等待操作状态
            if (!patternCheck.CheckPatternByPosition(candyColorInBoard))
            {
                gameState = GameState.Shuffle;
            }
            else
                gameState = GameState.Play;
        }
    }
    void Down()
    {
        if (Fall())
        {
            gameState = GameState.Anima;
            gameStateNext = GameState.Down;
        }
        else
        {
            gameState = GameState.Match;
        }
    }
    bool Swap()
    {
        /*
         * 由于全消和所有类型糖果结合都需要单独处理。
         * 所以第一层条件判断为：
         * 先检查candy0是全消，candy1是任何类型的情况。
         * 再检查candy1是全消,candy0是其他类型的情况（已知candy0不是全消）。
         * 排除以上条件之后，再处理其他两个都是特殊糖果的情况。
         * 最后剩下两个糖果不都是普通糖果的情况。
        */
        bool found = false;
        //被交换的两个糖果
        CandyControl candy0 = candiesInBoard[swapIndex0.x, swapIndex0.y];
        CandyControl candy1 = candiesInBoard[swapIndex1.x, swapIndex1.y];
        //清理
        //delPositionList = new List<Vector2Int>();
        if (candy0.candyType == CandyType.AllIn)
        {
            DestroyCandyClassified(swapIndex0);
            if (candy1.candyType == CandyType.AllIn)//两个全消交换，直接全屏消。
            {
                DestroyCandyClassified(swapIndex1);
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int x = 0; x < boardWidth; x++)
                    {
                        AddDelPositionList(new Vector2Int(y, x));
                    }
                }
            }
            else if (candy1.candyType == CandyType.Ordinary)
            {
                AllInDestroy(candyColorInBoard[swapIndex1.x, swapIndex1.y]);
            }
            else if (candy1.candyType == CandyType.Diamond)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int x = 0; x < boardWidth; x++)
                    {
                        if (candyColorInBoard[y, x] == candyColorInBoard[swapIndex1.x, swapIndex1.y])
                        {
                            candiesInBoard[y, x].SetCandyType(CandyType.Diamond);
                            AddDelPositionList(new Vector2Int(y, x));
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int x = 0; x < boardWidth; x++)
                    {
                        if (candyColorInBoard[y, x] == candyColorInBoard[swapIndex1.x, swapIndex1.y])
                        {
                            candiesInBoard[y, x].SetCandyType((CandyType)Random.Range(2, 4));
                            AddDelPositionList(new Vector2Int(y, x));
                        }
                    }
                }
            }
            found = true;
        }
        else if (candy1.candyType == CandyType.AllIn)
        {
            DestroyCandyClassified(swapIndex1);
            if (candy0.candyType == CandyType.Ordinary)
            {
                AllInDestroy(candyColorInBoard[swapIndex0.x, swapIndex0.y]);
            }
            else if (candy0.candyType == CandyType.Diamond)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int x = 0; x < boardWidth; x++)
                    {
                        if (candyColorInBoard[y, x] == candyColorInBoard[swapIndex0.x, swapIndex0.y])
                        {
                            candiesInBoard[y, x].SetCandyType(CandyType.Diamond);
                            AddDelPositionList(new Vector2Int(y, x));
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int x = 0; x < boardWidth; x++)
                    {
                        if (candyColorInBoard[y, x] == candyColorInBoard[swapIndex0.x, swapIndex0.y])
                        {
                            candiesInBoard[y, x].SetCandyType((CandyType)Random.Range(2, 4));
                            AddDelPositionList(new Vector2Int(y, x));
                        }
                    }
                }
            }
            found = true;
        }
        else if (candy0.candyType != CandyType.Ordinary && candy1.candyType != CandyType.Ordinary)
        {
            //检查横纵消除和菱形消除结合的情况
            if (candy0.candyType == CandyType.Vertical)
            {
                if (candy1.candyType == CandyType.Diamond)
                {
                    for (int i = 0; i <= diamondRadius * 2; i++)
                    {
                        VerticalDestroy(swapIndex1.y - diamondRadius + i);
                    }
                }
                else if (candy1.candyType == CandyType.Vertical || candy1.candyType == CandyType.Horizontal)
                {
                    VerticalDestroy(swapIndex1.y);
                    HorizontalDestroy(swapIndex1.x);
                    //candy0.candyType = CandyType.Ordinary;
                    //candy1.candyType = CandyType.Ordinary;
                }
            }
            else if (candy0.candyType == CandyType.Horizontal)
            {
                if (candy1.candyType == CandyType.Diamond)
                {
                    for (int i = 0; i <= diamondRadius * 2; i++)
                    {
                        HorizontalDestroy(swapIndex1.x - diamondRadius + i);
                    }
                }
                else if (candy1.candyType == CandyType.Vertical || candy1.candyType == CandyType.Horizontal)
                {
                    VerticalDestroy(swapIndex1.y);
                    HorizontalDestroy(swapIndex1.x);
                    //candy0.candyType = CandyType.Ordinary;
                    //candy1.candyType = CandyType.Ordinary;
                }
            }
            else if (candy0.candyType == CandyType.Diamond)
            {
                if (candy1.candyType == CandyType.Diamond)
                {
                    DiamondDestroy(swapIndex1.x, swapIndex1.y, diamondRadius * 2);
                    //candy0.candyType = CandyType.Ordinary;
                    //candy1.candyType = CandyType.Ordinary;
                }
                else if (candy1.candyType == CandyType.Vertical)
                {
                    for (int i = 0; i <= diamondRadius * 2; i++)
                    {
                        VerticalDestroy(swapIndex1.y - diamondRadius + i);
                    }
                }
                else if (candy1.candyType == CandyType.Horizontal)
                {
                    for (int i = 0; i <= diamondRadius * 2; i++)
                    {
                        HorizontalDestroy(swapIndex1.x - diamondRadius + i);
                    }
                }
            }
            DestroyCandyClassified(swapIndex0);
            DestroyCandyClassified(swapIndex1);
            found = true;
        }
        else
        {
            found = HaveMatch(true);
        }
        return found;
    }
    public bool IsCandy(int y, int x)
    {
        int color = candyColorInBoard[y, x];
        if (color >= 0 && color != int.MaxValue)
            return true;
        else
            return false;

    }
    void AddDelPositionList(Vector2Int vector2Int)
    {
        if (vector2Int.x >= 0 && vector2Int.y >= 0 && vector2Int.x < boardHeight && vector2Int.y < boardWidth)
        {
            if (!delPositionListTemp.Contains(vector2Int) && candyColorInBoard[vector2Int.x, vector2Int.y] != int.MaxValue)
            {
                delPositionListTemp.Add(vector2Int);
            }
        }
    }
    void DestroyCandyClassified(Vector2Int vector2Int)
    {
        if (candiesInBoard[vector2Int.x, vector2Int.y] && candiesInBoard[vector2Int.x, vector2Int.y].gameObject)
        {
            int y = vector2Int.x;
            int x = vector2Int.y;
            int c = candyColorInBoard[y, x];
            if (c < ordinaryCandySprites.Count)
            {
                Instantiate(explosionEffects[c], squareparent.position + new Vector3(x * widthDistance, -y * heightDistance), Quaternion.identity);
                animaCounter++;
            }
            candiesInBoard[y, x].Delete();
            candyColorInBoard[y, x] = int.MaxValue;
        }
    }
    public void DestroyCandyUnclassified(Vector2Int vector2Int)
    {
        int y = vector2Int.x, x = vector2Int.y;
        CandyControl candy = candiesInBoard[y, x];
        switch (candy.candyType)
        {
            case CandyType.Ordinary:
                break;
            case CandyType.Diamond:
                DiamondDestroy(y, x, diamondRadius);
                break;
            case CandyType.Vertical:
                VerticalDestroy(x);
                break;
            case CandyType.Horizontal:
                HorizontalDestroy(y);
                break;
            case CandyType.AllIn:
                AllInDestroy(Random.Range(0, ordinaryCandySprites.Count));
                break;
            default:
                break;
        }
        DestroyCandyClassified(vector2Int);
    }
    void AllInDestroy(int color)
    {
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (candyColorInBoard[y, x] == color)
                {
                    AddDelPositionList(new Vector2Int(y, x));
                }
            }
        }
    }
    void HorizontalDestroy(int Y)
    {
        for (int i = 0; i < boardWidth; i++)
        {
            AddDelPositionList(new Vector2Int(Y, i));
        }
    }
    void VerticalDestroy(int X)
    {
        for (int i = 0; i < boardHeight; i++)
        {
            AddDelPositionList(new Vector2Int(i, X));
        }
    }
    void DiamondDestroy(int Y, int X, int diamondRadius)
    {
        for (int h = 0; h < diamondRadius; h++)
        {
            for (int w = 0; w <= h * 2; w++)
            {
                int dy = Y - diamondRadius + h;
                int dx = X - h + w;
                AddDelPositionList(new Vector2Int(dy, dx));
            }
            for (int w = 0; w <= h * 2; w++)
            {
                int dy = Y + diamondRadius - h;
                int dx = X - h + w;
                AddDelPositionList(new Vector2Int(dy, dx));
            }
        }
        for (int i = 0; i <= diamondRadius * 2; i++)
        {
            int dx = X - diamondRadius + i;
            AddDelPositionList(new Vector2Int(Y, dx));
        }
    }
    public Sprite GetCandyTexture(int color, CandyType candyType)
    {
        Sprite candySprite;
        string s;
        switch (candyType)
        {
            case CandyType.Ordinary:
                candySprite = ordinaryCandySprites[color];
                break;
            case CandyType.Diamond:
                s = candySpritesPath + (color + 1) + candyDiamondSuffix;
                candySprite = Resources.Load<Sprite>(s);
                break;
            case CandyType.Vertical:
                s = candySpritesPath + (color + 1) + candyVerticalSuffix;
                candySprite = Resources.Load<Sprite>(s);
                break;
            case CandyType.Horizontal:
                s = candySpritesPath + (color + 1) + candyHorizontalSuffix;
                candySprite = Resources.Load<Sprite>(s);
                break;
            case CandyType.AllIn:
                candySprite = Resources.Load<Sprite>(candyAllInPath);
                break;
            default:
                candySprite = null;
                break;
        }
        return candySprite;
    }
    public void CreateCandy(int y, int x, int color, CandyType candyType)
    {
        GameObject candyGO = Instantiate(candyPrefab, squareparent.position + new Vector3(x * widthDistance, -y * heightDistance), Quaternion.identity);
        candyGO.transform.parent = squareparent;
        CandyControl candy = candyGO.GetComponent<CandyControl>();
        Sprite candySprite = GetCandyTexture(color, candyType);
        candy.Initialization(candySprite, candyType, color);
        candyColorInBoard[y, x] = color;
        candiesInBoard[y, x] = candy;
    }
    //交换动画结束时调用的方法
    public void FirstStageEnd()
    {
        gameState = GameState.Play;
    }
    List<Vector2Int> delPositionList = new List<Vector2Int>();
    List<Vector2Int> delPositionListTemp = new List<Vector2Int>();
    Dictionary<Vector2Int[], CandyType> createCandyDic = new Dictionary<Vector2Int[], CandyType>();
    List<CandyInfo> createInfoList = new List<CandyInfo>();
    //扫描整个棋盘
    //如果有连续三个及以上，返回T并进行结算
    //如果没有可以合成的组合，返回F
    bool HaveMatch(bool needCheck = false)
    {
        bool found = false;
        int flag = 0;//计数标记
        int theColor = -1;
        Stack<Vector2Int> tempStack = new Stack<Vector2Int>();
        Vector2Int[,] tempVec2Int = new Vector2Int[boardHeight, boardWidth];
        createCandyDic.Clear();
        //行扫描
        for (int y = 0; y < boardHeight; y++)
        {
            theColor = int.MinValue;
            for (int x = 0; x < boardWidth; x++)
            {
                if (candyColorInBoard[y, x] == ordinaryCandySprites.Count)
                {
                    while (tempStack.Count != 0)
                    {
                        Vector2Int vt = tempStack.Pop();
                        tempVec2Int[vt.x, vt.y].x = flag;
                    }
                    theColor = int.MinValue;
                    continue;
                }
                if (candyColorInBoard[y, x] == theColor)
                {
                    flag++;
                    //tempVec2Int[y, x].x = ++flag;
                    tempStack.Push(new Vector2Int(y, x));
                }
                else
                {
                    while (tempStack.Count != 0)
                    {
                        Vector2Int vt = tempStack.Pop();
                        tempVec2Int[vt.x, vt.y].x = flag;
                    }
                    theColor = candyColorInBoard[y, x];
                    flag = 1;
                    //tempVec2Int[y, x].x = flag;
                    tempStack.Push(new Vector2Int(y, x));
                }
                if (x == boardWidth - 1)
                {
                    while (tempStack.Count != 0)
                    {
                        Vector2Int vt = tempStack.Pop();
                        tempVec2Int[vt.x, vt.y].x = flag;
                    }
                }
            }
        }
        //列扫描
        for (int x = 0; x < boardWidth; x++)
        {
            theColor = int.MinValue;
            for (int y = 0; y < boardHeight; y++)
            {
                if (candyColorInBoard[y, x] == ordinaryCandySprites.Count)
                {
                    while (tempStack.Count != 0)
                    {
                        Vector2Int vt = tempStack.Pop();
                        tempVec2Int[vt.x, vt.y].x = flag;
                    }
                    theColor = int.MinValue;
                    continue;
                }
                if (candyColorInBoard[y, x] == theColor)
                {
                    flag++;
                    //tempVec2Int[y, x].x = ++flag;
                    tempStack.Push(new Vector2Int(y, x));
                }
                else
                {
                    while (tempStack.Count != 0)
                    {
                        Vector2Int vt = tempStack.Pop();
                        tempVec2Int[vt.x, vt.y].y = flag;
                    }
                    theColor = candyColorInBoard[y, x];
                    flag = 1;
                    //tempVec2Int[y, x].x = flag;
                    tempStack.Push(new Vector2Int(y, x));
                }
                if (y == boardHeight - 1)
                {
                    while (tempStack.Count != 0)
                    {
                        Vector2Int vt = tempStack.Pop();
                        tempVec2Int[vt.x, vt.y].y = flag;
                    }
                }
            }
        }
        //------------处理扫描结果------------//
        Vector2Int vecTemp;
        //处理扫描结果//如果flag大于2则说明需要删除
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                vecTemp = tempVec2Int[y, x];
                if (vecTemp.x > 2 || vecTemp.y > 2)
                {
                    AddDelPositionList(new Vector2Int(y, x));
                    found = true;
                }
            }
        }
        if (!found)
        {
            return found;
        }
        //如果flag大于5，说明需要生成allin 
        Vector2Int[] tempAllIn;
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                vecTemp = tempVec2Int[y, x];
                if (vecTemp.x >= 5)
                {
                    tempAllIn = new Vector2Int[vecTemp.x];
                    tempAllIn[0] = new Vector2Int(y, x);
                    //tempVec2Int[y, x].x = 0;
                    for (int n = 1; n < vecTemp.x; n++)
                    {
                        tempAllIn[n] = new Vector2Int(y, x + n);
                    }
                    x += vecTemp.x - 1;
                    createCandyDic.Add(tempAllIn, CandyType.AllIn);
                }
            }
        }
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                vecTemp = tempVec2Int[y, x];
                if (vecTemp.y >= 5)
                {
                    tempAllIn = new Vector2Int[vecTemp.y];
                    tempAllIn[0] = new Vector2Int(y, x);
                    for (int n = 1; n < vecTemp.y; n++)
                    {
                        tempAllIn[n] = new Vector2Int(y + n, x);
                    }
                    y += vecTemp.y - 1;
                    createCandyDic.Add(tempAllIn, CandyType.AllIn);
                }
            }
        }
        //处理L型合成
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                vecTemp = tempVec2Int[y, x];
                if ((vecTemp.x == 3 || vecTemp.x == 4) && (vecTemp.y == 3 || vecTemp.y == 4))
                {
                    tempVec2Int[y, x] = new Vector2Int(0, 0);
                    List<Vector2Int> tempL = new List<Vector2Int>();
                    tempL.Add(new Vector2Int(y, x));
                    //向上搜索
                    for (int ty = y - 1; ty >= 0; ty--)
                    {
                        if (candyColorInBoard[ty, x] == candyColorInBoard[y, x])
                        {
                            tempL.Add(new Vector2Int(ty, x));
                            tempVec2Int[ty, x].y = 0;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //向下搜索
                    for (int ty = y + 1; ty < boardHeight; ty++)
                    {
                        if (candyColorInBoard[ty, x] == candyColorInBoard[y, x])
                        {
                            tempL.Add(new Vector2Int(ty, x));
                            tempVec2Int[ty, x].y = 0;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //向左搜索
                    for (int tx = x - 1; tx >= 0; tx--)
                    {
                        if (candyColorInBoard[y, tx] == candyColorInBoard[y, x])
                        {
                            tempL.Add(new Vector2Int(y, tx));
                            tempVec2Int[y, tx].x = 0;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //向右搜索
                    for (int tx = x + 1; tx < boardWidth; tx++)
                    {
                        if (candyColorInBoard[y, tx] == candyColorInBoard[y, x])
                        {
                            tempL.Add(new Vector2Int(y, tx));
                            tempVec2Int[y, tx].x = 0;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (tempL.Count >= 5)
                    {
                        createCandyDic.Add(tempL.ToArray(), CandyType.Diamond);
                    }
                }
            }
        }
        //处理4连合成
        Vector2Int[] tempF;
        bool isL;
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                vecTemp = tempVec2Int[y, x];

                if (vecTemp.x == 4)
                {
                    isL = false;
                    for (int n = 0; n < 4; n++)
                    {
                        if (x + n < boardWidth && (tempVec2Int[y, x + n].y == 3 || tempVec2Int[y, x + n].y == 4))
                        {
                            isL = true;
                        }
                    }
                    if (!isL)
                    {
                        tempF = new Vector2Int[4];
                        for (int a = 0; a < 4; a++)
                        {
                            tempF[a] = new Vector2Int(y, x + a);
                        }
                        createCandyDic.Add(tempF, CandyType.Vertical);
                    }
                    x += 3;
                }
            }
        }
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                vecTemp = tempVec2Int[y, x];
                if (vecTemp.y == 4)
                {
                    isL = false;
                    for (int n = 0; n < 4; n++)
                    {
                        if (y + n < boardHeight && (tempVec2Int[y + n, x].x == 3 || tempVec2Int[y + n, x].x == 4))
                        {
                            isL = true;
                        }
                    }
                    if (!isL)
                    {
                        tempF = new Vector2Int[4];
                        for (int a = 0; a < 4; a++)
                        {
                            tempF[a] = new Vector2Int(y + a, x);
                        }
                        createCandyDic.Add(tempF, CandyType.Horizontal);
                    }
                    y += 3;
                }
            }
        }
        //--------处理合成糖果结果---------//   
        createInfoList.Clear();
        int createX, createY, createColor;
        bool isPlayPos0 = false;
        bool isPlayPos1 = false;
        foreach (var item in createCandyDic)
        {
            if (needCheck)
            {
                foreach (var pos in item.Key)
                {
                    if (pos.x == swapIndex0.x && pos.y == swapIndex0.y)
                    {
                        isPlayPos0 = true;
                    }
                    else if (pos.x == swapIndex1.x && pos.y == swapIndex1.y)
                    {
                        isPlayPos1 = true;
                    }
                }
            }
            if (isPlayPos0)
            {
                createY = swapIndex0.x;
                createX = swapIndex0.y;
                Debug.Log(item.Key.Length);
            }
            else if (isPlayPos1)
            {
                createY = swapIndex1.x;
                createX = swapIndex1.y;
                Debug.Log(item.Key.Length);
            }
            else if (item.Value == CandyType.Diamond)
            {
                createY = item.Key[0].x;
                createX = item.Key[0].y;
            }
            else
            {
                createY = item.Key[item.Key.Length / 2].x;
                createX = item.Key[item.Key.Length / 2].y;
            }
            createColor = candyColorInBoard[createY, createX];
            switch (item.Value)
            {
                case CandyType.Diamond:
                    createInfoList.Add(new CandyInfo(createColor, new Vector2Int(createY, createX), CandyType.Diamond));
                    break;
                case CandyType.Vertical:
                    createInfoList.Add(new CandyInfo(createColor, new Vector2Int(createY, createX), CandyType.Vertical));
                    break;
                case CandyType.Horizontal:
                    createInfoList.Add(new CandyInfo(createColor, new Vector2Int(createY, createX), CandyType.Horizontal));
                    break;
                case CandyType.AllIn:
                    createInfoList.Add(new CandyInfo(ordinaryCandySprites.Count, new Vector2Int(createY, createX), CandyType.AllIn));
                    break;
                default:
                    break;
            }
        }

        return found;
    }

    //计算下落并生成新的棋子
    bool Fall()
    {
        bool isFall = false;
        //遍历第一行，空格子生成新的
        for (int i = 0; i < boardWidth; i++)
        {
            if (candyColorInBoard[0, i] == int.MaxValue)
            {
                //CreateCandy(0, i, Random.Range(0, ordinaryCandySprites.Count), CandyType.Ordinary);
                int color = Random.Range(0, ordinaryCandySprites.Count);
                GameObject candyGO = Instantiate(candyPrefab, squareparent.position + new Vector3(i * widthDistance, 1 * heightDistance), Quaternion.identity);
                candyGO.transform.parent = squareparent;
                CandyControl candy = candyGO.GetComponent<CandyControl>();
                Sprite candySprite = GetCandyTexture(color, CandyType.Ordinary);
                candy.Initialization(candySprite, CandyType.Ordinary, color);
                int theX = i;
                Tweener tweener = candyGO.transform.DOMove(squareparent.position + new Vector3(i * widthDistance, 0), 0.1f);
                tweener.OnComplete(() =>
                {
                    candyColorInBoard[0, theX] = color;
                    candiesInBoard[0, theX] = candy;
                    AnimaPlayEnd();
                });
                animaCounter++;
                isFall = true;
            }
        }
        //向下
        for (int y = 0; y < boardHeight - 1; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                //当前格子有糖果并且当前格子的下一个格子没有糖果
                if (IsCandy(y, x) && candyColorInBoard[y + 1, x] == int.MaxValue)
                {
                    int theY = y;
                    int theX = x;
                    Tweener tweener = candiesInBoard[y, x].transform.DOMove(squareparent.position + new Vector3(x * widthDistance, -(y + 1) * heightDistance), 0.1f);
                    tweener.OnComplete(() =>
                    {
                        candyColorInBoard[theY + 1, theX] = candyColorInBoard[theY, theX];
                        candyColorInBoard[theY, theX] = int.MaxValue;

                        candiesInBoard[theY + 1, theX] = candiesInBoard[theY, theX];
                        candiesInBoard[theY, theX] = null;
                        AnimaPlayEnd();
                    });
                    animaCounter++;

                    isFall = true;
                }
            }
        }
        
        return isFall;
    }
}

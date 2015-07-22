/* Copyright (c) Vander Amaral
 * I've created this code based on a lot of Match3 Codes I found on the Internet
 * I tried to make it the best and easy to change.
 * You can polish it even more, and add functions to it if you bought.
 * It is NOT ok to sell this code, you can only sell the final product ie. the final game
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{

    /// <summary>
    /// 定义面板状态
    /// </summary>
    public enum BState
    {  						//State of the Board
        PLAYING,
        DROP_JEWELS,
        SWAPPING,
        GAMEOVER,
        RESETING
    };
    public static int CurrentlyMovingJewels; 	//控制宝石的移动. Controlled by Jewel.cs
    protected BState theBoardState;				//State of the Board	
    protected bool isActive;					//State of the Active			
    protected bool canSwap;						//State of the if you canSwap			
    /// <summary>
    /// 列表用于记录宝石应该被破坏还是保存
    /// </summary>
    private List<GameObject> jewelRemains;		//列表用于记录宝石应该被破坏还是保存
    private bool needToDropJewels;				//Controls the Dropping Jewels
    private int totalRemovedJewels;				//记录消除了多少个宝石（可用于计算分数） 
    public GameObject SelectedJewel;			//Can be used as Prefab holder for highlighting a selected jewel
    private Jewel tip;							//Control the Jewel Alpha hint
    public AudioClip SwapSound;					//SwapSound
    public AudioClip explodeSound;				//ExplodeSound
    public AudioClip match3Sound;				//Match3Sound
    private bool needToCheckCascades = false;	//检查是否联动
    private bool mouseLeftDown = false;			//Check for mouseUp
    private int totalTriplets = 0;				//连3的计数

    public GameObject Rock;						//Can be used as Rock or special item 
    public GameObject[] jewels;					//这是用在游戏上的宝石至少需要3种不同的
    public static GameObject[,] jewelMapPosition;  //Hold's the jewels virtual position 

    private struct theSwap
    {				//helper for the swap I grab this part of the code from some websites
        public bool twiceClick;				//handles the double click
        public int jewelAx, jewelAy;		//handles the Jewel A position to make the swap
        public int jewelBx, jewelBy;		//handles the Jewel B position to make the swap
        public bool isActive;				//the swap is active?

        //给上述变量初始化
        public void Init()
        {				//Resets the variables above to restart the swap
            jewelAx = jewelAy = -1;
            jewelBx = jewelBy = -1;
            isActive = false;
            twiceClick = false;
        }
    };
    private theSwap swapping;				//if the player is swapping	
    private bool validateSwap;				//Validate the swap	
    public int boardSize;					// That's the board size, needs to be greater than 3
    private float delayDrop;				// 控制掉落宝石的延时

    private List<GameObject> hintJewels;	//controls the Hint Jewels	//提示宝石的序列
    private float hintTimer;				//hint timer
    private float hintDelay;				//hint timer delay

    private int baseScore;					//that's the base score of the game I've set it to 100 but you can change
    private int longestChain;				//controls the longest chain
    protected int mouseClickX, mouseClickY;	//controls the mouseclick based on the jewel position

    private int skullInitial = 100;  		//how many points the skull will reduce消除骷髅减分

    public Texture2D flash;					//Texture for the Screen Flash
    public static bool flashIt;				//Flashes the Screen if true
    private float aFlash = 5;				//Alpha that controls the flash timer 闪烁时间

    private float ScoreBoostTimer;			//You can use this variable to boost your score for some time
    private int ScoreBoostValue;			//value if you want to use the scoreboost
    public Transform TheBoard;				//that's the board GameObject where the board will be placed

    /// <summary>
    /// 程序入口
    /// </summary>
    void Start()
    {
        Reset();
    }
    /// <summary>
    /// 定义好面板后开始给里边填充宝石
    /// </summary>
    private void ResetJewels()
    {
        int x, y;
        //布置面板宝石之前先删除所有的宝石
        for (y = 0; y < boardSize; y++)
            for (x = 0; x < boardSize; x++)
            {
                Destroy(jewelMapPosition[x, y]);
                jewelMapPosition[x, y] = null;
            }
        //开始布置宝石1列1列的排版
        for (y = 0; y < boardSize; y++)
        {
            for (x = 0; x < boardSize; x++)
            {
                // 排布的时候要避免出现3个的情况
                GameObject objectType1 = jewels[UnityEngine.Random.Range(0, jewels.Length)];//随机添加一个宝石
                //获取到宝石后实例化一个宝石预设体
                GameObject jewelPrefab = (GameObject)Instantiate(objectType1, new Vector3(x, y, 0), jewels[0].transform.localRotation);
                jewelMapPosition[x, y] = jewelPrefab;//放入对应坐标
                jewelPrefab.transform.parent = TheBoard;
                jewelPrefab.transform.localPosition = new Vector3(x, y, 0);
                //循环判断连3情况
                while (true)
                {
                    if (CountMatch3() == 0) break;//判断是否存在连3的情况，不存在跳出循环，添加下一组
                    //存在连3情况：销毁当前宝石，重新实例化一个新宝石
                    Destroy(jewelMapPosition[x, y]);
                    GameObject objectType2 = jewels[UnityEngine.Random.Range(0, jewels.Length)];
                    GameObject jewelPrefab2 = (GameObject)Instantiate(objectType2, new Vector3(x, y, 0), jewels[0].transform.localRotation);
                    jewelMapPosition[x, y] = jewelPrefab2;
                    jewelPrefab2.transform.parent = TheBoard;
                    jewelPrefab2.transform.localPosition = new Vector3(x, y, 0);
                }
            }
        }
    }
    /// <summary>
    /// 定义面板
    /// </summary>
    public void Reset()
    {
        if (boardSize < 3) boardSize = 8;
        theBoardState = BState.RESETING;
        jewelMapPosition = new GameObject[boardSize, boardSize];
        mouseClickY = -1;
        mouseClickX = -1;
        CurrentlyMovingJewels = 0;
        needToDropJewels = false;
        totalRemovedJewels = 0;
        validateSwap = false;
        hintTimer = 0;
        hintDelay = 5;
        baseScore = 100;
        longestChain = 0;
        isActive = true;
        canSwap = true;
        hintJewels = new List<GameObject>(16);
        jewelRemains = new List<GameObject>(16);

        // Randomize the jewels
        do
        {
            ResetJewels();
        }
        while ((movesLeft = HowManyMovesLeft()) == 0);

        theBoardState = BState.PLAYING;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tagType"></param>
    /// <param name="howMuch"></param>
    public void DestroyJewelType(string tagType, int howMuch)
    {
        // Destroy the old jewels
        int isRemoveJewel = 0;

        if (howMuch == 0)
        {
            for (int y = 0; y < boardSize; y++)
            {
                for (int x = 0; x < boardSize; x++)
                {
                    if (IsJewelAt(x, y))
                    {
                        if (jewelMapPosition[x, y].tag == tagType)
                        {
                            RemoveJewelAt(x, y);
                            isRemoveJewel++;
                        }
                    }
                }
            }
        }
        else
        {
            for (int y = 0; y < boardSize; y++)
            {
                for (int x = 0; x < boardSize; x++)
                {
                    if (IsJewelAt(x, y))
                    {
                        if (jewelMapPosition[x, y].tag == tagType && isRemoveJewel < howMuch)
                        {
                            RemoveJewelAt(x, y);
                            isRemoveJewel++;
                        }
                    }
                }
            }
        }


        if (isRemoveJewel > 0)
        {  //Was something removed?
            needToDropJewels = true;
            delayDrop = 0.3f;//0.75f;
            canSwap = false;
            GetComponent<AudioSource>().PlayOneShot(explodeSound);
            CameraShake.shakeFor(0.5f, 0.1f); //Shake the screen
            flashIt = true;
        }

    }
    /// <summary>
    /// 
    /// </summary>
    private void PopulateWithNewJewels()
    {
        CurrentlyMovingJewels = 0;
        theBoardState = BState.PLAYING;
        needToDropJewels = false;
        canSwap = true;

        // Destroy the old jewels
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                if (IsJewelAt(x, y))
                {
                    GameObject j = (GameObject)jewelMapPosition[x, y];
                    Jewel jj = j.gameObject.GetComponent<Jewel>();
                    jewelMapPosition[x, y] = null;
                    jj.Die();
                    jewelRemains.Add(j);
                }
            }
        }
        GetComponent<AudioSource>().PlayOneShot(explodeSound);

        // Randomize new jewels
        do
        {
            ResetJewels();
        }
        while ((movesLeft = HowManyMovesLeft()) == 0);

        // Hide them from the view, and start an animation that drops down the
        // jewels, piece by piece
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                GameObject j = jewelMapPosition[x, y];
                Jewel jj = j.gameObject.GetComponent<Jewel>();
                j.transform.localPosition = new Vector3(j.transform.localPosition.x, j.transform.localPosition.y - boardSize, j.transform.localPosition.z);
                jj.Move(3, boardSize);
            }
        }

    }
    /// <summary>
    /// 
    /// </summary>
    void Update()
    {

        if (theBoardState == BState.RESETING) return; //查看面板当前状态

        UpdateJewelRemains();  						//Update the Jewels
        //判断面板是否处于激活状态
        if (!isActive) return; 						//wait until the board is active
        //判断当前可移动的宝石有几个
        if (CurrentlyMovingJewels > 0) return;

        // 降低宝石掉落延时（越掉越快） 
        if (delayDrop > 0)
        {
            delayDrop -= (float)Time.deltaTime;
            return;
        }
        // 如果可移动宝石大于0
        if (CurrentlyMovingJewels > 0) 
            theBoardState = BState.DROP_JEWELS;
        else
        {
            if (needToCheckCascades)
            {
                CheckForCascades();
                needToCheckCascades = false;
            }
            if (validateSwap)
            { 			// Validate the new position for jewels after swap
                int count = CountMatch3();
                if (count == 0)
                {
                    SwapJewels(swapping);	// If not good then swap back
                    // Play a sound
                    //do something due illegal move
                }
                else
                {
                    RemoveTriplets();
                }
                validateSwap = false;
            }
            if (swapping.isActive) theBoardState = BState.SWAPPING;
            else theBoardState = BState.PLAYING;
        }

        if (theBoardState != BState.PLAYING && theBoardState != BState.SWAPPING) return; // Can we interact or not?

        if (theBoardState == BState.PLAYING)
        { 	// Increase the hint timer if there's no action on the board
            hintTimer += (float)Time.deltaTime;
            if (hintTimer >= hintDelay)			// Reset the hint delay
                ShowRandomHint();
        }

        if (Input.GetMouseButton(0))
        { 			// Check if there is a touch or mouse on some tile

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 30))
            {
                if (hit.transform.tag != "Untagged")
                {
                    mouseClickX = (int)hit.transform.localPosition.x;
                    mouseClickY = (int)hit.transform.localPosition.y;
                    //SelectedJewel.transform.position = new Vector3(hit.transform.position.x,hit.transform.position.y,-1.02f);
                }
                else
                {
                    mouseClickY = -1;
                    mouseClickX = -1;
                    //SelectedJewel.transform.position = new Vector3(-6,-6,-1.02f);
                }
            }
        }

        if (swapping.twiceClick == true && swapping.isActive)
        {
            swapping.jewelBx = mouseClickX;
            swapping.jewelBy = mouseClickY;
        }

        if (canSwap && mouseClickX > -1 && Input.GetMouseButton(0))
        { // Start to swap if some jewel is clicked

            if (theBoardState != BState.SWAPPING && !mouseLeftDown)
            {
                swapping.Init();
                swapping.isActive = true;
                swapping.jewelAx = mouseClickX;
                swapping.jewelAy = mouseClickY;
                theBoardState = BState.SWAPPING;
                mouseLeftDown = true;
            }
            else
            {
                swapping.jewelBx = mouseClickX;
                swapping.jewelBy = mouseClickY;

                int dx = swapping.jewelAx - swapping.jewelBx;
                int dy = swapping.jewelAy - swapping.jewelBy;
                if (Mathf.Abs(dx) > 1 || Mathf.Abs(dy) > 1)
                {
                    swapping.isActive = false;
                    theBoardState = BState.PLAYING;
                }

            }
        }

        if (canSwap && Input.GetMouseButton(0))
        { // When the mouse button or touch is released..
            mouseLeftDown = false;
            if (tip)
            {
                tip.alpha = false;
                tip = null;
            }
            if (theBoardState == BState.SWAPPING)
            {
                // If we have released on the same jewel use alternate swapping method (two clicks)
                if (mouseClickX == swapping.jewelAx && mouseClickY == swapping.jewelAy)
                {
                    //swapping.twiceClick = true;
                }
                else if (!IsLegalSwap())
                { 			//Cancels Swap
                    swapping.isActive = false;
                    theBoardState = BState.PLAYING;
                    //SelectedJewel.transform.localPosition = new Vector3(-6,-6,-1.02f);
                }
                else if (!swapping.twiceClick)
                { 		//Swap
                    GetComponent<AudioSource>().pitch = 1;
                    GetComponent<AudioSource>().PlayOneShot(SwapSound);
                    swapping.isActive = false;
                    SwapJewels(swapping);
                    validateSwap = true;				// Need to validate it
                    //SelectedJewel.transform.localPosition = new Vector3(-6,-6,-1.02f);
                }
                mouseClickY = -1;
                mouseClickX = -1;
                //SelectedJewel.transform.localPosition = new Vector3(-6,-6,-1.02f);
            }
        }

        if (needToDropJewels && delayDrop <= 0)
        { // Check for drop jewels
            needToDropJewels = false;
            CheckForFallingJewels();
        }

        if (CurrentlyMovingJewels > 0)
        {
            //do something when the jewels are moving
        }
    }

    private void ShowRandomHint()
    {
        if (hintJewels.Count == 0) return;		// No moves left

        if (tip)
        {
            hintTimer = 0;
            return;
        }

        if (!tip)
        {

            GameObject tipGO = (GameObject)hintJewels[UnityEngine.Random.Range(0, hintJewels.Count)];

            do
            {
                tipGO = (GameObject)hintJewels[UnityEngine.Random.Range(0, hintJewels.Count)];
            }
            while (tipGO.tag == "Rock");
            tip = (Jewel)tipGO.gameObject.GetComponent<Jewel>();
            tip.alpha = true;
        }

        hintTimer = 0;
    }

    private void SwapJewels(theSwap swap)
    {

        int dirA, dirB;
        bool verifyRock = false;

        if (swap.jewelAx > swap.jewelBx)
        {
            dirA = 2;
            dirB = 1;
        }
        else if (swap.jewelBx > swap.jewelAx)
        {
            dirA = 1;
            dirB = 2;
        }
        else if (swap.jewelAy > swap.jewelBy)
        {
            dirA = 4;
            dirB = 3;
        }
        else
        {
            dirA = 3;
            dirB = 4;
        }

        Jewel objIn;

        if (jewelMapPosition[swap.jewelAx, swap.jewelAy].tag == "Rock" || jewelMapPosition[swap.jewelBx, swap.jewelBy].tag == "Rock")
        {
            verifyRock = true;
        }
        else
        {
            verifyRock = false;
        }


        if (!verifyRock)
        { //Handles the swap
            objIn = jewelMapPosition[swap.jewelAx, swap.jewelAy].gameObject.GetComponent<Jewel>();
            objIn.Move(dirA);

            objIn = jewelMapPosition[swap.jewelBx, swap.jewelBy].gameObject.GetComponent<Jewel>();
            objIn.Move(dirB);

            GameObject j = jewelMapPosition[swap.jewelAx, swap.jewelAy];
            jewelMapPosition[swap.jewelAx, swap.jewelAy] = jewelMapPosition[swap.jewelBx, swap.jewelBy];
            jewelMapPosition[swap.jewelBx, swap.jewelBy] = j;
        }

    }

    public bool IsLegalSwap()
    {
        if (swapping.jewelAx < 0 || swapping.jewelBx < 0) return false;
        if (jewelMapPosition[swapping.jewelAx, swapping.jewelAy] == null || jewelMapPosition[swapping.jewelBx, swapping.jewelBy] == null) return false;

        int dx = swapping.jewelAx - swapping.jewelBx;
        int dy = swapping.jewelAy - swapping.jewelBy;
        if (Mathf.Abs(dx) > 1 || Mathf.Abs(dy) > 1) return false;
        if ((Mathf.Abs(dx) == 1 && dy == 0) || (Mathf.Abs(dy) == 1 && dx == 0)) return true;
        return false;
    }


    private int HowBigHoleAt(int x, int y, bool checkUp)
    {
        int length = 0;
        if (checkUp)
        {
            for (int hy = y; hy >= 0; hy--)
            {
                if (!IsJewelAt(x, hy))
                    length++;
                else
                    break;
            }
        }
        else
        {
            for (int hy = y; hy < boardSize; hy++)
            {
                if (!IsJewelAt(x, hy))
                    length++;
                else
                    break;
            }
        }

        return length;
    }


    private void ShiftJewelsDown(int x, int y, int slots)
    {
        for (int yy = y; yy >= 0; yy--)
        {
            jewelMapPosition[x, yy + slots] = jewelMapPosition[x, yy];
            if (jewelMapPosition[x, yy + slots] != null)
            {

                Jewel objIn = jewelMapPosition[x, yy + slots].GetComponent<Jewel>();
                objIn.Move(3, slots);
            }
            jewelMapPosition[x, yy] = null;
        }
    }


    public void CheckForFallingJewels()
    {
        for (int x = 0; x < boardSize; x++)
        {			// Checks every column from left to right, from bottom to up
            int totalSlots = 0;
            for (int y = boardSize - 1; y > 0; y--)
            {	// search for holes
                if (!IsJewelAt(x, y))
                { 				// if there is a hole then check how tall it is
                    int slots = HowBigHoleAt(x, y, true);
                    totalSlots += slots;
                    y -= slots;

                    ShiftJewelsDown(x, y, slots); 		// Shift every jewel in this column above y down the hole slots
                    y += slots;

                    needToCheckCascades = true;
                }
            }
        }

        for (int x = 0; x < boardSize; x++)
        {			// Check the top row for empty slots
            if (!IsJewelAt(x, 0))
            {
                int slots = HowBigHoleAt(x, 0, false);	// if there is a hole then check how tall it is

                for (int y = 0; y < slots; y++)
                {		// Create a new jewel, position it out of the screen to fall down
                    GameObject jewelPrefab = (GameObject)Instantiate(jewels[UnityEngine.Random.Range(0, jewels.Length)], new Vector3(x, y - slots, -1), jewels[0].transform.rotation);
                    jewelPrefab.transform.parent = TheBoard;
                    jewelPrefab.transform.localPosition = new Vector3(x, y - slots, 0);

                    Jewel objIn = jewelPrefab.gameObject.GetComponent<Jewel>();
                    objIn.Move(3, slots);

                    jewelMapPosition[x, y] = jewelPrefab;
                    needToCheckCascades = true;
                }
            }
        }
    }
    /// <summary>
    /// 判断是否存在消除
    /// </summary>
    public void CheckForCascades()
    {
        int total = RemoveTriplets();//判断是否完成消除
        if (total == 0)
        { 						// 没有可消除的情况
            totalTriplets = 0;
            canSwap = true;

            movesLeft = HowManyMovesLeft();// 计算当前可以移动的步数

            if (movesLeft == 0)
            {				// 使用这个如果你想结束的时候就没有左移
                PopulateWithNewJewels();	// 在这种情况下再生新的珠宝
            }
        }
    }

    /// <summary>
    /// 定义宝石的位置
    /// </summary>
    /// <param name="x">x坐标</param>
    /// <param name="y">y坐标</param>
    /// <returns>不能放置返回false</returns>
    public bool IsJewelAt(int x, int y)
    {
        //x、y坐标不能小于0，不能超越边界，边界从0开始计数
        if (x < 0 || y < 0 || x > boardSize - 1 || y > boardSize - 1) return false;
        return jewelMapPosition[x, y] != null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void RemoveJewelAt(int x, int y)
    {
        if (!IsJewelAt(x, y)) return;
        if (x >= 0 && x < boardSize && y >= 0 && y < boardSize)
        {
            GameObject j = (GameObject)jewelMapPosition[x, y];
            jewelMapPosition[x, y] = null;
            Jewel jj = j.gameObject.GetComponent<Jewel>();//获取到Jewel方法
            jj.Die();
            jewelRemains.Add(j);
            totalRemovedJewels++;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void UpdateJewelRemains()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (GameObject j in jewelRemains)
        {
            Jewel jj = j.gameObject.GetComponent<Jewel>();
            if (jj.Dead)
                toRemove.Add(j);
        }

        foreach (GameObject j in toRemove)
            jewelRemains.Remove(j);
        toRemove.Clear();
    }

    /// <summary>
    /// 判断是否存在连3
    /// </summary>
    /// <returns value='triple'>若不为0则存在连3情况</returns>
    public int CountMatch3()
    {
        int triple = 0;

        for (int y = 0; y < boardSize; y++)
        { 		// Horizontal
            int counter = 1;
            for (int x = 1; x < boardSize; x++)
            {
                //判断该位置可放置，水平前一个位置可放置并且二者tag相同
                if (IsJewelAt(x, y) && IsJewelAt(x - 1, y) && jewelMapPosition[x, y].tag == jewelMapPosition[x - 1, y].tag)
                {
                    counter++;
                    if (counter >= 3 && jewelMapPosition[x, y].tag != "Rock")
                    {
                        triple++;					// 存在连3情况，计数+1
                    }
                }
                else
                {
                    counter = 1;
                }
            }
        }

        for (int x = 0; x < boardSize; x++)
        {		// Vertical
            int counter = 1;
            for (int y = 1; y < boardSize; y++)
            {
                if (IsJewelAt(x, y) && IsJewelAt(x, y - 1) && jewelMapPosition[x, y].tag == jewelMapPosition[x, y - 1].tag)
                {
                    counter++;
                    if (counter >= 3 && jewelMapPosition[x, y].tag != "Rock")
                    {
                        triple++;					// Three or more were found
                    }
                }
                else
                {
                    counter = 1;
                }
            }
        }
        return triple;
    }

    /// <summary>
    /// 连3消除后计算积分
    /// </summary>
    /// <param name="jewels">消除宝石个数</param>
    /// <param name="start">开始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="lineRow">行数</param>
    /// <param name="horizontal">是否水平</param>
    private void ScoreMatch3(int jewels, int start, int end, int lineRow, bool horizontal)
    {

        int tripletScore = (jewels - 2) * baseScore;
        string itemMatched = "";
        bool foundSkull = false;
        totalTriplets++;
       //如果连3超过1个
        if (totalTriplets > 1)
            tripletScore += 30 * (totalTriplets - 1);//多一个加30分

        if (totalTriplets > longestChain)			// Calculate the longest chain
            longestChain = totalTriplets;
        //播放声音文件
        GetComponent<AudioSource>().pitch = 1;
        GetComponent<AudioSource>().pitch = 1 + (totalTriplets * 0.2f);

        if (horizontal)
        {
            // Horizontal match 3 or more
            PointCount(jewels, tripletScore, start, lineRow, end, jewelMapPosition[start, lineRow].tag, new Vector2(start + 1, lineRow));
            if (jewelMapPosition[start, lineRow].tag == "Item6") foundSkull = true;//如果是骷髅
            itemMatched = jewelMapPosition[start, lineRow].tag;
            if (foundSkull) 
                PointsAni(-skullInitial, new Vector2(start + 1, lineRow)); 
            else 
                PointsAni(tripletScore, new Vector2(start + 1, lineRow));
        }
        else
        {
            // Vertical match 3 or more
            PointCount(jewels, tripletScore, lineRow, start, end, jewelMapPosition[lineRow, start].tag, new Vector2(lineRow, start + 1));
            if (jewelMapPosition[lineRow, start].tag == "Item6") foundSkull = true;
            itemMatched = jewelMapPosition[lineRow, start].tag;
            if (foundSkull) PointsAni(-skullInitial, new Vector2(lineRow, start + 1)); else PointsAni(tripletScore, new Vector2(lineRow, start + 1));
        }

        //if totalTriplets is greater than 4 lets explode some skulls and put some effect on the screen
        if (totalTriplets == 4) { DestroyJewelType("Item6", 0); CameraShake.shakeFor(1f, 0.1f); flashIt = true; }

        //Special effects on the screen 
        //Show messages when the user matches triplets
        Transform camPos = Camera.main.transform;
        if (totalTriplets > 1 && !foundSkull && !GameObject.FindGameObjectWithTag("MMsg"))
        {

            //Show the message img according to the pts
            GameObject msg = (GameObject)Instantiate(Resources.Load("Prefabs/puzzleUI/MatchMessage"));
            msg.transform.position = new Vector3(camPos.position.x, camPos.position.y, camPos.position.z + 6f);
            msg.transform.rotation.SetLookRotation(camPos.position);

            //Loads the Textures
            if (totalTriplets == 2) msg.GetComponent<Renderer>().material.mainTexture = (Texture2D)Resources.Load("Textures/2Dtextures/P1");
            if (totalTriplets == 3) msg.GetComponent<Renderer>().material.mainTexture = (Texture2D)Resources.Load("Textures/2Dtextures/P2");
            if (totalTriplets == 4) msg.GetComponent<Renderer>().material.mainTexture = (Texture2D)Resources.Load("Textures/2Dtextures/P3");
            if (totalTriplets >= 5) msg.GetComponent<Renderer>().material.mainTexture = (Texture2D)Resources.Load("Textures/2Dtextures/P4");
        }

        //Special effects on the screen 
        //Show messages when the user matches 3 or more skulls
        if (foundSkull && !GameObject.FindGameObjectWithTag("MMsg"))
        {
            GameObject skullmsg = (GameObject)Instantiate(Resources.Load("Prefabs/puzzleUI/MatchMessage"));
            skullmsg.transform.position = new Vector3(camPos.position.x, camPos.position.y, camPos.position.z + 6f);
            skullmsg.transform.rotation.SetLookRotation(camPos.position);
            skullmsg.GetComponent<Renderer>().material.mainTexture = (Texture2D)Resources.Load("Textures/2Dtextures/P5");
        }

        if (foundSkull)
        {
            //some code here if found skull
        }


        if (tripletScore < 0) tripletScore = 0;

        if (!foundSkull)
        { //Count Score
            if (ScoreBoostTimer > Time.timeSinceLevelLoad)
            {
                // add more score here if you want to use the boost timer
                // TotalScore += tripletScore*ScoreBoostValue;
            }
            else
            {
                //TotalScore += tripletScore;
                ScoreBoostValue = 0;
            }
        }

    }

    //Use this function if you want to put some special effects on the Screen
    /// <summary>
    /// 在屏幕上添加特效
    /// </summary>
    /// <param name="jewels">消除的宝石数量</param>
    /// <param name="score">当前获得的分数</param>
    /// <param name="start">开始位置</param>
    /// <param name="lineRow">行数</param>
    /// <param name="end">结束位置</param>
    /// <param name="item">宝石类型</param>
    /// <param name="middle">中点</param>
    private void PointCount(int jewels, int score, int start, int lineRow, int end, string item, Vector2 middle)
    {

        int itemCh = 0;

        switch (item)
        {
            case "Item1":
                itemCh = 0;
                break;
            case "Item2":
                itemCh = 1;
                break;
            case "Item3":
                itemCh = 2;
                break;
            case "Item4":
                itemCh = 3;
                break;
            case "Item5":
                itemCh = 4;
                break;
            case "Item7":   //hp
                itemCh = 5;
                break;
            default:
                itemCh = -1;
                //just break if no options          
                break;
        }

        if (item == "Item6")
        {  //if Skull
            GetComponent<AudioSource>().PlayOneShot(explodeSound);
            CameraShake.shakeFor(0.5f, 0.1f);
        }
        else
        {
            GetComponent<AudioSource>().PlayOneShot(match3Sound);
        }

        //Example
        //if(itemCh == 0) Debug.Log("you matched"+ jewels);


    }

    private void refreshAll()
    {
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                if (IsJewelAt(x, y))
                {
                    if (jewelMapPosition[x, y].transform.localPosition != new Vector3(x, y, -1))
                    {
                        Jewel jj = jewelMapPosition[x, y].GetComponent<Jewel>();
                        jj.MoveOut(new Vector3(x, y, -1));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 消除连3情况
    /// </summary>
    /// <returns></returns>
    private int RemoveTriplets()
    {
        int triplets = CountMatch3();
        if (triplets == 0) return 0;			// 消除完成
        //创建一个二维数组来存在当前位置是否可以消除
        bool[,] markedForRemoval = new bool[boardSize, boardSize];


        for (int y = 0; y < boardSize; y++)
        {	// Horizontal
            int counter = 1;
            int startsAt = 0;
            int endsAt = -1;

            for (int x = 1; x < boardSize; x++)
            {
                //如果该位置和横向前一个位置都存在、并且二者的tag相同、并且该宝石tag不是rock
                if (IsJewelAt(x, y) && IsJewelAt(x - 1, y) && jewelMapPosition[x, y].tag == jewelMapPosition[x - 1, y].tag && jewelMapPosition[x, y].tag != "Rock")
                {
                    counter++;
                    endsAt = x;//新X坐标替换原坐标
                }
                else
                {
                    if (counter >= 3)
                    {			// Mark for removal
                        ScoreMatch3(counter, startsAt, endsAt, y, true);
                        for (int rx = startsAt; rx <= endsAt; rx++)
                            markedForRemoval[rx, y] = true;
                    }

                    counter = 1;
                    startsAt = x;
                    endsAt = 0;
                }
            }

            if (counter >= 3 && endsAt == boardSize - 1)
            {
                ScoreMatch3(counter, startsAt, endsAt, y, true);
                for (int rx = startsAt; rx <= endsAt; rx++)
                    markedForRemoval[rx, y] = true;
            }
        }

        for (int x = 0; x < boardSize; x++)
        {	// Vertical
            int counter = 1;
            int startsAt = 0;
            int endsAt = -1;

            for (int y = 1; y < boardSize; y++)
            {
                if (IsJewelAt(x, y) && IsJewelAt(x, y - 1) && jewelMapPosition[x, y].tag == jewelMapPosition[x, y - 1].tag && jewelMapPosition[x, y].tag != "Rock")
                {
                    counter++;
                    endsAt = y;
                }
                else
                {
                    if (counter >= 3)
                    { // Mark for removal
                        ScoreMatch3(counter, startsAt, endsAt, x, false);
                        for (int ry = startsAt; ry <= endsAt; ry++)
                            markedForRemoval[x, ry] = true;
                    }

                    counter = 1;
                    startsAt = y;
                    endsAt = 0;
                }

                if (counter >= 3 && endsAt == boardSize - 1)
                {
                    ScoreMatch3(counter, startsAt, endsAt, x, false);
                    for (int ry = startsAt; ry <= endsAt; ry++)
                        markedForRemoval[x, ry] = true;
                }
            }
        }


        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                if (markedForRemoval[x, y]) RemoveJewelAt(x, y);
            }
        }

        needToDropJewels = true;
        delayDrop = 0.3f;
        canSwap = false;

        return triplets;
    }

    private int movesLeft = 0;

    /// <summary>
    /// 计算当前可移动的数量
    /// </summary>
    /// <returns></returns>
    public int HowManyMovesLeft()
    {
        int moves = 0;
        hintJewels.Clear();
        hintTimer = 0;

        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                if (!IsJewelAt(x, y)) continue;
                // Swap with the jewel line
                if (IsJewelAt(x + 1, y))
                {
                    GameObject swap = jewelMapPosition[x, y];
                    jewelMapPosition[x, y] = jewelMapPosition[x + 1, y];
                    jewelMapPosition[x + 1, y] = swap;
                    int m = CountMatch3();
                    if (m > 0)
                    {
                        hintJewels.Add(jewelMapPosition[x, y]);
                        hintJewels.Add(jewelMapPosition[x + 1, y]);
                    }
                    moves += m;

                    // Swap back
                    jewelMapPosition[x + 1, y] = jewelMapPosition[x, y];
                    jewelMapPosition[x, y] = swap;
                }

                // Swap with the jewel row
                if (IsJewelAt(x, y + 1))
                {
                    GameObject swap = jewelMapPosition[x, y];
                    jewelMapPosition[x, y] = jewelMapPosition[x, y + 1];
                    jewelMapPosition[x, y + 1] = swap;
                    int m = CountMatch3();
                    if (m > 0)
                    {
                        hintJewels.Add(jewelMapPosition[x, y]);
                        hintJewels.Add(jewelMapPosition[x, y + 1]);
                    }
                    moves += m;

                    // Swap back
                    jewelMapPosition[x, y + 1] = jewelMapPosition[x, y];
                    jewelMapPosition[x, y] = swap;
                }
            }
        }

        return moves;
    }


    void OnGUI()
    {
        if (flashIt)
        { //Just flashes the Screen
            aFlash -= 0.01f;
            GUI.color = new Color(1, 1, 1, aFlash);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flash);
            if (aFlash < 0.01f)
            {
                flashIt = false;
                aFlash = 1;
            }
        }
    }

    /// <summary>
    /// 这个函数调用预置点会显示在屏幕上
    /// </summary>
    /// <param name="a">分数</param>
    /// <param name="position">位置</param>
    void PointsAni(int a, Vector2 position)
    {
        GameObject msg2 = (GameObject)Instantiate(Resources.Load("Prefabs/puzzleUI/MatchMessage2"));
        msg2.transform.position = new Vector3(TheBoard.position.x - position.x, TheBoard.position.y - position.y, msg2.transform.position.z);
        MessageMatch m = msg2.GetComponent<MessageMatch>();
        TextMesh PointText = msg2.GetComponent<TextMesh>();
        if (a > 0) msg2.GetComponent<Renderer>().material.color = Color.green;
        if (a < 0) msg2.GetComponent<Renderer>().material.color = Color.red;
        m.direction = 1;
        m.wait = true; m.howLong = 0.3f;
        if (a > 0) PointText.text = "+" + a.ToString() + " pts"; else PointText.text = a.ToString() + " pts";
    }

    //Score boost 
    void ScoreBoost(int howMuch, int howLong)
    {
        ScoreBoostTimer = Time.timeSinceLevelLoad + howLong;
        ScoreBoostValue = howMuch;
    }

}

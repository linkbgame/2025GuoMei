using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FatherChess : MonoBehaviour
{
    public OneChess[] allChess;
    /// <summary>
    /// ÖØÍ¶¿ÉÒÔ
    /// </summary>
    public bool CanTakeAllAgain = false;
    public int GetChessNum()
    {
        if (allChess[0].isWhite && allChess[1].isWhite
            && allChess[2].isWhite && allChess[3].isWhite
            && allChess[4].isWhite) 
        {
            CanTakeAllAgain = true;
            return 16;//°×°×°×°×°×
        }
        else if (!allChess[0].isWhite && !allChess[1].isWhite
            && !allChess[2].isWhite && !allChess[3].isWhite
            && !allChess[4].isWhite)
        {
            CanTakeAllAgain = true;
            return 8;//ºÚºÚºÚºÚºÚ
        }
        else if (allChess[0].isWhite && allChess[1].isWhite
            && allChess[2].isWhite && !allChess[3].isWhite
            && !allChess[4].isWhite)
        {
            CanTakeAllAgain = true;
            return 14;//°×°×°×ºÚºÚ
        }
        else if (!allChess[0].isWhite && !allChess[1].isWhite
            && !allChess[2].isWhite && allChess[3].isWhite
            && allChess[4].isWhite)
        {
            CanTakeAllAgain = true;
            return 10;//ºÚºÚºÚ°×°×
        }
        else if (!allChess[0].isWhite && !allChess[1].isWhite
            && !allChess[2].isWhite && allChess[3].isWhite
            && !allChess[4].isWhite)
        {
            CanTakeAllAgain = false;
            return 12;//ºÚºÚºÚ°×ºÚ
        }
        else if (allChess[0].isWhite && allChess[1].isWhite
            && allChess[2].isWhite && allChess[3].isWhite
            && !allChess[4].isWhite)
        {
            CanTakeAllAgain = false;
            return 11;//°×°×°×°×ºÚ
        }
        else if (!allChess[0].isWhite && !allChess[1].isWhite
            && allChess[2].isWhite && !allChess[3].isWhite
            && !allChess[4].isWhite)
        {
            CanTakeAllAgain = false;
            return 5;//ºÚºÚ°×ºÚºÚ
        }
        else if (allChess[0].isWhite && allChess[1].isWhite
            && !allChess[2].isWhite && allChess[3].isWhite
            && allChess[4].isWhite)
        {
            CanTakeAllAgain = false;
            return 4;//°×°×ºÚ°×°×
        }
        else
        {
            CanTakeAllAgain = false;
            int num=0;
            for(int i = 0; i < 5; i++)
            {
                if(allChess[i].isWhite) { num++; }
            }
            if(num==3)
                return 3;//Èý°×¶þºÚ
            else if(num==2)
            {
                return 2;//ÈýºÚ¶þ°×
            }
            return 0;//È«¶¼²»·ûºÏ
        }
    }
}

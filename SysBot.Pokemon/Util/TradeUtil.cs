using SysBot.Base;
using System;
using System.Collections.Generic;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon;

public static class TradeUtil
{
    public static int GetCodeDigit(int code, int c)
    {
        for (int i = 7; i > c; i--)
            code /= 10;
        return code % 10;
    }

    public static IEnumerable<SwitchButton> GetPresses(int code)
    {
        var end = 1;
        for (int i = 0; i < 8; i++)
        {
            var key = GetCodeDigit(code, i);
            foreach (var k in MoveCursor(end, key))
                yield return k;
            yield return A;
            end = key;
        }
    }

    private static IEnumerable<SwitchButton> MoveCursor(int start, int dest) // 0-9
    {
        if (start == dest)
            yield break;
        if (dest == 0)
        {
            int row = (start - 1) / 3;
            for (int i = row; i < 3; i++)
                yield return DDOWN; // down
            yield break;
        }
        if (start == 0)
        {
            yield return DUP; // up
            start = 8;
        }

        foreach (var m in MoveSquare(start, dest))
            yield return m;
    }

    private static IEnumerable<SwitchButton> MoveSquare(int start, int dest)
    {
        int dindex = dest - 1;
        int cindex = start - 1;
        int dcol = dindex % 3;
        int ccol = cindex % 3;
        int drow = dindex / 3;
        int crow = cindex / 3;

        if (drow != crow)
        {
            var dir = drow > crow ? DDOWN : DUP;
            var count = Math.Abs(drow - crow);
            for (int i = 0; i < count; i++)
                yield return dir;
        }
        if (dcol != ccol)
        {
            var dir = dcol > ccol ? DRIGHT : DLEFT;
            var count = Math.Abs(dcol - ccol);
            for (int i = 0; i < count; i++)
                yield return dir;
        }
    }
}

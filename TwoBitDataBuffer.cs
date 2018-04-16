using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class TwoBitDataBuffer{
    public int dataCount { get; private set; }
    
    /// <summary>
    /// Construct a new data buffer with the given size.
    /// </summary>
    /// <param name="dataCount"></param>
    public TwoBitDataBuffer(int dataCount)
    {
        this.dataCount = dataCount;
        data = new char[GetCharCount(dataCount)];
    }

    /// <summary>
    /// Loads a data buffer from the given char array.
    /// </summary>
    public TwoBitDataBuffer(int dataCount, char[] charArray)
    {
        this.data = charArray;
        this.dataCount = dataCount;
    }
    
    /// <summary>
    /// Sets a value at given index. Value should be 0 ~ 3
    /// </summary>
    public void SetValue(int index, int targetValue)
    {
        if (index >= dataCount)
        {
            Debug.LogError("Index exceeds data count!");
            return;
        }

        targetValue = Mathf.Clamp(targetValue, 0, _DataMaxValue);
        int charIndex = index / _ElementPerChar;
        int charOffset = index % _ElementPerChar;

        var charElement = data[charIndex];

        charElement = SetBit(charElement, charOffset * 2, targetValue >= 2);
        charElement = SetBit(charElement, charOffset * 2 + 1, targetValue % 2 != 0);
        data[charIndex] = charElement;
    }
    
    /// <summary>
    /// returns a value ranged in 0 ~ 3
    /// </summary>
    public int GetValue(int index)
    {
        if (index >= dataCount)
        {
            Debug.LogError("Index exceeds data count!");
            return 0;
        }

        int charIndex = index / _ElementPerChar;
        int charOffset = index % _ElementPerChar;
        var charElement = data[charIndex];

        int upper = GetBit(charElement, charOffset * 2) ? 2 : 0;
        int lower = GetBit(charElement, charOffset * 2 + 1) ? 1 : 0;
        return upper + lower;
    }
    
    public char[] GetPackedData()
    {
        return data;
    }

    public void PrintValues()
    {
        var str = "values: ";
        foreach(var bit in data)
        {
            str += bit.ToString() + ",";
        }
        Debug.Log(str);
    }

    #region Helper
    private char[] data;
    private const int _ElementPerChar = 4;
    private const int _DataMaxValue = 3;
    
    private int GetCharCount(int dataCount)
    {
        int offset = dataCount % _ElementPerChar > 0 ? 1 : 0;
        return dataCount / _ElementPerChar + offset;
    }


    private char SetBit(char element, int bit, bool value)
    {
        if (value)
        {
            return (char) (element | 1 << bit);
        }
        else
        {
            return (char) (element & (~(1 << bit)));
        }
    }

    private bool GetBit(char element, int bit)
    {
        return (element & (1 << bit)) != 0;
    }

    #endregion
}

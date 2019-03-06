using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helpers : MonoBehaviour
{
    public static string PrintV3Arr(Vector3[] arr) {
        string str = "";
        foreach (Vector3 vec in arr) {
            str += vec + " ";
        }
        return str;
    }

    public static string PrintV2Arr(Vector2[] arr) {
        string str = "";
        foreach (Vector2 vec in arr) {
            str += vec + " ";
        }
        return str;
    }

    public static string PrintIntArr(int[] arr) {
        string str = "";
        foreach (int n in arr) {
            str += n + " ";
        }
        return str;
    }

    public static string Print2DIntArr(int[,] arr) {
        string str = "";
        for (int x = 0; x < arr.GetUpperBound(0); x++) {
            for (int y = 0; y < arr.GetUpperBound(1); y++) {
                str += arr[x,y] + " ";
            }
            str += "\n";
        }
        return str;
    }

    public static int HashableInt(Vector2Int vector)
    {
        int x = Mathf.RoundToInt(vector.x);
        int y = Mathf.RoundToInt(vector.y);
        return x * 1000 + y * 1000000;
    }

    public static int HashableInt(int x, int y)
    {
        return x * 1000 + y * 1000000;
    }

    public static Vector2Int UnhashInt(int hashedInt) {
        /*if (hashedInt % 1000000 != 0) {
            Debug.Log("Cant unhash int " + hashedInt + "!");
            return Vector2Int.zero;
        }*/

        int yVal = hashedInt / 1000000;
        int xVal = (hashedInt - (yVal * 1000000))/1000;
        return new Vector2Int(xVal, yVal);
    }

    public static List<int> GetListFromArr(int[] arr) {
        List<int> newList = new List<int>();
        foreach (int i in arr) {
            newList.Add(i);
        }
        return newList;
    }

    public static int[] GetArrFromList(List<int> li) {
        if (li == null) {
            return null;
        }
        int[] newArr = new int[li.Count];
        int index = 0;
        foreach (int i in li) {
            newArr[index] = i;
            index++;
        }
        return newArr;
    }

    public static string ArrToString(int[] arr) {
        string arrString = "";
        foreach (int i in arr) {
            arrString += (i + " ");
        }
        return arrString;
	}
}

using UnityEngine;
using UnityEditor;
 
public class RuleTileAsset
{
	public static void CreateAsset (string path, string fileName)
	{
		ScriptableObjectUtility.CreateAsset<RuleTile> (path, fileName);
	}
}
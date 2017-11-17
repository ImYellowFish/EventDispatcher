using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CurveRampGenerator : MonoBehaviour {
    public AnimationCurve curve;
    public int width = 256;
    public int height = 8;
    public string savePath;
    public string fileName;

    [ContextMenu("Create")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void Create() {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        for(int i = 0; i < width; i++) {
            float value = curve.Evaluate((float)i / width);
            Color c = new Color(value, value, value);

            for(int j = 0; j < width; j++) {
                tex.SetPixel(i, j, c);    
            }
        }
        tex.Apply();

        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + savePath + "/" + fileName + ".png", bytes);
        UnityEditor.AssetDatabase.Refresh();
    }
}

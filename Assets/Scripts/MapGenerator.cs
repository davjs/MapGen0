using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MapGenerator : MonoBehaviour {
    private const int Width = 800;
    private const int Height = 800;
    private readonly float[,] _heightMap = new float[800,800];
    private Texture2D _outTexture;
    private TextureDrawer _textureDrawer;

    private void Start()
    {
        _outTexture = new Texture2D(Width,Height);
        _textureDrawer = new TextureDrawer();
        GenerateHeightMap();
    }

    private void GenerateHeightMap() {
        var offset = Random.Range(0, 100000);
        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                var value = Mathf.PerlinNoise(x*0.01f + offset, y*0.01f);
                _heightMap[x, y] = value;
                _outTexture.SetPixel(x,y, new Color(value,value,value));
            }   
        }
        _outTexture.Apply();
    }

    private void Update() {
        _textureDrawer.Draw(_outTexture, Width, Height);
    }

    private void OnGUI() {
        const int height = 40;
        if (GUI.Button(new Rect(10, 70 + height * 0, 100, height), "Regen")) {
            GenerateHeightMap();
        }
        if (GUI.Button(new Rect(10, 70 + height * 1, 100, height), "Save")) {
            _textureDrawer.Save(_outTexture);
        }
        
        if (GUI.Button(new Rect(10, 110 + height * 2, 100, height), "Debug")) {
            _textureDrawer.SetDebug();
        }
        if (GUI.Button(new Rect(10, 110 + height * 3, 100, height), "Standard")) {
            _textureDrawer.SetStandard();
        }
    }
}

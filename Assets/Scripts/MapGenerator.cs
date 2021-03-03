using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;


public class MapGenerator : MonoBehaviour {

    struct WaterCell {
        public int X;
        public int Y;
        public int Amount;
    }
    
    private const int Width = 800;
    private const int Height = 800;
    private readonly float[,] _heightMap = new float[800,800];
    private readonly WaterCell[,] _waterMap = new WaterCell[800,800];
    private Texture2D _outTexture;
    private TextureDrawer _textureDrawer;


    float packWaterCell(int direction, int velocity) {
        Assert.IsTrue(direction <= 3);
        Assert.IsTrue(velocity <= 1000);
        return direction * 1000 + velocity;
    }

    Vector2Int randomAxialVector() {
        switch (Mathf.RoundToInt(Random.Range(0, 4))) {
            case 0: return Vector2Int.right;
            case 1: return Vector2Int.up;
            case 2: return Vector2Int.left;
            case 3: return Vector2Int.down;
            default: return Vector2Int.right;
        }
    }

    private void Start()
    {
        _outTexture = new Texture2D(Width,Height, TextureFormat.RGBAFloat, false);
        _textureDrawer = new TextureDrawer();
        GenerateHeightMap();

        for (int i = 0; i < Width; i++) {
            var v = randomAxialVector();
            _waterMap[i, Height / 2] = new WaterCell {
                Amount = 1,
                X = v.x,
                Y = v.y
            };
        }
        
        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                _outTexture.SetPixel(x,y, new Color(_heightMap[x,y],_waterMap[x,y].X, _waterMap[x,y].Y,_waterMap[x,y].Amount));
            }   
        }
        _outTexture.Apply();
    }

    private void GenerateHeightMap() {
        var offset = Random.Range(0, 100000);
        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                var value = Mathf.PerlinNoise(x*0.01f + offset, y*0.01f);
                _heightMap[x, y] = value;
            }   
        }
    }

    private void Update() {
        _textureDrawer.Draw(_outTexture, Width, Height, zoom);

        if (Input.mouseScrollDelta.magnitude > 0.1f) {
            zoom = Mathf.Clamp(zoom + Input.mouseScrollDelta.y * zoom * 0.2f, 0.8f, 100.0f);
        }
    }

    private float zoom = 85.0f;

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

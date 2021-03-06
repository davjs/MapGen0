using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;


public class MapGenerator : MonoBehaviour {
    struct WaterCell {
        public int X;
        public int Y;
        public int Amount;
    }

    private const int Width = 800;
    private const int Height = 800;
    private readonly float[,] _heightMap = new float[800, 800];
    private readonly WaterCell[,] _waterMap = new WaterCell[800, 800];
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

    private void Start() {
        _outTexture = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false);
        _textureDrawer = new TextureDrawer();

        LoadHeightMapFromFile();
        ReGenerate();
    }

    private void LoadHeightMapFromFile() {
        const string path = "Assets/datafile.png";
        if (File.Exists(path)) {
            var fileData = File.ReadAllBytes(path);
            _outTexture.LoadImage(fileData);
            _textureDrawer.CreateAssetAndSave(_outTexture);
        }
        else {
            ReGenerate();
            _textureDrawer.CreateAssetAndSave(_outTexture);
        }

        if (UnityEngine.Windows.File.Exists("zoomLevel")) {
            zoom = BitConverter.ToSingle(UnityEngine.Windows.File.ReadAllBytes("zoomLevel"),0);
        }
    }


    private void ReGenerate() {
        GenerateHeightMap();
        GenerateRivers();

        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                _outTexture.SetPixel(x, y,
                    new Color(_heightMap[x, y], _waterMap[x, y].X, _waterMap[x, y].Y, _waterMap[x, y].Amount));
            }
        }

        _outTexture.Apply();
    }

    private void GenerateRivers() {
        // Find hills/mountains
        var mointainTops = GetSpreadOutMointainTops();
        var targetCells = new List<int2>() {
            new int2(+1, 0),
            new int2(0, +1),
            new int2(-1, 0),
            new int2(0, -1),
        };


        foreach (var mountainTop in mointainTops) {
            var xx = mountainTop.Item1;
            var yy = mountainTop.Item2;
            var lastDir = new int2(0, 1);
            while (xx < Width -1 && xx > 1 && yy > 1 && yy < Height -1) {
                var cellWithHeight = targetCells.Select(c => (c, _heightMap[xx + c.x,yy + c.y]));
                var ordered = cellWithHeight.OrderBy(x => x.Item2).ToList();
                var lowest = ordered.First();
                var height = _heightMap[xx,yy];
                
                if (lowest.Item2 < height) {
                    _waterMap[xx,yy ] = new WaterCell {
                        Amount = 1,
                        X = lowest.c.x,
                        Y = lowest.c.y
                    };
                    lastDir = lowest.c;
                    xx = xx + lastDir.x;
                    yy = yy + lastDir.y;
                }
                else {
                    _waterMap[xx,yy ] = new WaterCell {
                        Amount = 1,
                        X = lastDir.x,
                        Y = lastDir.y
                    };
                    break;
                }
//                
                
                
            }
        }
    }

    private List<Tuple<int, int, float>> GetSpreadOutMointainTops() {
        var Queue = new List<Tuple<int, int, float>>(3);
        for (int xx = 0; xx < Height; xx++) {
            for (int yy = 0; yy < Width; yy++) {
                var height = _heightMap[xx, yy];
                if (Queue.Count < 3) {
                    Queue.Add(Tuple.Create(xx, yy, height));
                    Queue.Sort((a, b) => a.Item3.CompareTo(b.Item3));
                }
                else {
                    var awayFromAll = Queue.All(x => {
                        var dist = (new Vector2(x.Item1, x.Item2) - new Vector2(xx, yy)).magnitude;
                        return dist > 100.0;
                    });

                    if (awayFromAll && Queue.Any(x => x.Item3 < height)) {
                        var awayFromAll2 = Queue.All(x => {
                            var dist = (new Vector2(x.Item1, x.Item2) - new Vector2(xx, yy)).magnitude;
                            return dist > 100.0;
                        });
                        if (awayFromAll2) {
                            Queue.RemoveAt(0);
                            Queue.Add(Tuple.Create(xx, yy, height));
                            Queue.Sort((a, b) => a.Item3.CompareTo(b.Item3));
                        }
                    }
                }
            }
        }

        return Queue;
    }

    private void GenerateHeightMap() {
        var offset = Random.Range(0, 100000);
        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                var value = Mathf.PerlinNoise(x * 0.01f + offset, y * 0.01f);
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
            ReGenerate();
        }

        if (GUI.Button(new Rect(10, 70 + height * 1, 100, height), "Save")) {
            _textureDrawer.UpdateSave(_outTexture, zoom);
        }

        if (GUI.Button(new Rect(10, 110 + height * 2, 100, height), "Debug")) {
            _textureDrawer.SetDebug();
        }

        if (GUI.Button(new Rect(10, 110 + height * 3, 100, height), "Standard")) {
            _textureDrawer.SetStandard();
        }
    }
}
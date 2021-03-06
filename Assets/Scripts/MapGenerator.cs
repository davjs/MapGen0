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


//TODO: shouldnt need to be monobehaviour
public class MapGenerator {
    
    struct WaterCell {
        public int X;
        public int Y;
        public int Amount;
    }

    private readonly int _width;
    private readonly int _height;
    private readonly float[,] _heightMap;
    private readonly WaterCell[,] _waterMap;
    private readonly Texture2D _outTexture;
    private readonly TextureDrawer _textureDrawer;

    public Texture2D GetTexture() {
        return _outTexture;
    }

    public MapGenerator(int height, int width) {
        _width = width;
        _height = height;
        _heightMap = new float[width, height];
        _waterMap = new WaterCell[width, height];
        _outTexture = new Texture2D(_width, _height, TextureFormat.RGBAFloat, false);
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

    }


    public void ReGenerate() {
        GenerateHeightMap();
        GenerateRivers();

        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                _outTexture.SetPixel(x, y,
                    new Color(_heightMap[x, y], _waterMap[x, y].X * 0.5f + 0.5f, _waterMap[x, y].Y * 0.5f + 0.5f, _waterMap[x, y].Amount));
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
            while (xx < _width -1 && xx > 1 && yy > 1 && yy < _height -1) {
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
        for (int xx = 0; xx < _height; xx++) {
            for (int yy = 0; yy < _width; yy++) {
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

}
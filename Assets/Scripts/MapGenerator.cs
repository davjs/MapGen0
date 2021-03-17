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
    private WaterCell[,] _waterMap;
    private readonly Texture2D _outTexture;
    private readonly TextureDrawer _textureDrawer;
    public float KeepFlowDirFactor = 0.005f;

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
        GenerateMountainChain();
        GenerateRivers();

        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                _outTexture.SetPixel(x, y,
                    new Color(_heightMap[x, y], _waterMap[x, y].X * 0.5f + 0.5f, _waterMap[x, y].Y * 0.5f + 0.5f, _waterMap[x, y].Amount));
            }
        }

        _outTexture.Apply();
    }

    public void EnterHeightMapMode() {
        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                _outTexture.SetPixel(x, y, new Color(_heightMap[x, y], _heightMap[x, y], _heightMap[x, y]));
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
        _waterMap = new WaterCell[_width,_height];
        

        foreach (var mountainTop in mointainTops) {
            var xx = mountainTop.Item1;
            var yy = mountainTop.Item2;
            var lastDir = new int2(0, 0);
            int maxLength = Math.Max(_width, _height);
            int i = 0;
            while (xx < _width -1 && xx > 1 && yy > 1 && yy < _height -1 && i++ < maxLength) {
                var dir = lastDir;
                var cellWithHeight = targetCells.Select(c => {
                    var d = math.distance(c, dir) * KeepFlowDirFactor;
                    return (c,
                            _heightMap[xx + c.x, yy + c.y] +
                            _waterMap[xx + c.x, yy + c.y].Amount +
                            d
                        );
                });
                var ordered = cellWithHeight.OrderBy(x => x.Item2).ToList();
                var lowest = ordered.First();
                var height = _heightMap[xx,yy];
                
                if (lowest.Item2 < height + 0.25f) {
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
        for (var xx = 0; xx < _height; xx++) {
            for (var yy = 0; yy < _width; yy++) {
                var height = _heightMap[xx, yy];
                if (Queue.Count < 3) {
                    Queue.Add(Tuple.Create(xx, yy, height));
                    Queue.Sort((a, b) => a.Item3.CompareTo(b.Item3));
                }
                else {
                    var awayFromAll = Queue.All(x => {
                        var dist = (new Vector2(x.Item1, x.Item2) - new Vector2(xx, yy)).magnitude;
                        return dist > Random.Range(50, 100);
                    });

                    if (awayFromAll && Queue.Any(x => x.Item3 < height)) {
                        var awayFromAll2 = Queue.All(x => {
                            var dist = (new Vector2(x.Item1, x.Item2) - new Vector2(xx, yy)).magnitude;
                            return dist > Random.Range(50, 100);;
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

    float noise(float2 st) {
        return Mathf.PerlinNoise(st.x, st.y);
    }
    
    float fbm (float2 st) {
        // Initial values
        var value = 0.0f;
        var amplitude = .5f;
        var frequency = 0.0f;
        //
        // Loop of octaves
        for (var i = 0; i < 6; i++) {
            value += amplitude * noise(st);
            st *= 2.0f;
            amplitude *= .5f;
        }
        return value;
    }
    
    List<Tuple<int2,float>> GenerateRandPeaks() {
        var peaks = new List<Tuple<int2,float>>();

//        for (int i = 0; i < 8; i++) {
//            var xx = Random.Range(Mathf.RoundToInt(_width * 0.3f), Mathf.RoundToInt(_width * 0.7f));
//            var yy = Random.Range(Mathf.RoundToInt(_height * 0.4f), Mathf.RoundToInt(_height * 0.6f));
//            peaks.Add(Tuple.Create(new int2(xx, yy), fbm(new float2(xx * 0.02f,yy * 0.02f)) * 0.5f  + 0.5f));
//        }
        int xx = _width / 2;
        int yy = _height / 2;
        peaks.Add(Tuple.Create(new int2(xx, yy), 1.0f));
        
        return peaks;
    }
    
    public Vector2 FindNearestPointOnLine(Vector2 origin, Vector2 end, Vector2 point)
    {
        //Get heading
        var heading = (end - origin);
        var magnitudeMax = heading.magnitude;
        heading.Normalize();

        //Do projection from the point but clamp it
        var lhs = point - origin;
        var dotP = Vector2.Dot(lhs, heading);
        dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
        return origin + heading * dotP;
    }

    private void GenerateHeightMapUsingStamp() {
        const string path = "Assets/resources/mountain.png";
        var fileData = File.ReadAllBytes(path);
        var img = new Texture2D(2,2);
        img.LoadImage(fileData);

        for (int xx = 0; xx < img.width; xx++) {
            for (int yy = 0; yy < img.height; yy++) {
                _heightMap[_width / 2 + xx - img.width /2, _height / 2 + yy - img.height /2] = img.GetPixel(xx,yy).r;
            }
        }
    }


    private void GenerateHeightMap() {
        var peaks = GenerateRandPeaks();
        var offset = Random.Range(0, 100000);
        
        var chainStart = new Vector2(_width/3.0f, _height/2.0f);
        var chainEnd = new Vector2(_width * 2.0f/3.0f, _height/2.0f);
        
        var id = Random.Range(0,10000);
        
        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                var height = 0.0f;

                var mountainCoord = FindNearestPointOnLine(chainStart, chainEnd, new Vector2(x,y));
                var pos = new Vector2(x, y);
                var vicinity = 1.0f - (math.distance(pos, mountainCoord) * 2.0f) / _width;
                var dir = math.normalize(pos - mountainCoord) + id + mountainCoord.x * 10.0f;
                var distModifier = fbm(dir * 2.0f) * 0.1f - noise(dir* 2.0f) * 0.1f;
                var peakHeight = 1.0f;
                    
                //height = (vicinity + distModifier) * peakHeight;
                foreach (var peak in peaks) {

                    vicinity = 1.0f - (math.distance(pos, peak.Item1) * 4.0f) / _width;
                    dir = math.normalize(pos - new Vector2(peak.Item1.x, peak.Item1.y)) + id + peak.Item1.x * 10.0f;
                    distModifier = fbm(dir * 2.0f) * 0.1f - noise(dir* 2.0f) * 0.1f;
                    peakHeight = peak.Item2;
                    height = Mathf.Max((vicinity + distModifier) * peakHeight, height);
                }

                _heightMap[x, y] = height * 0.8f + Mathf.PerlinNoise(x * 0.02f + offset, y * 0.02f) * 0.025f;
            }
        }
    }
    
    
    List<Tuple<int2,float>> GeneratePeakChain() {
        var peaks = new List<Tuple<int2,float>>();

        for (var xx = _width /3 ; xx < _width * 2 / 3; xx+=1) {
            var offset = Mathf.RoundToInt(fbm(new float2(xx * 0.001f, _height)) * 400.0f);
            peaks.Add(Tuple.Create(new int2(xx, _height / 2 + offset), fbm(new float2(xx * 0.01f,_height / 2.0f)) * 0.7f  + 0.3f));
        }
        
        return peaks;
    }

    
    private void GenerateMountainChain() {
        var peaks = GeneratePeakChain();
        var offset = Random.Range(0, 100000);
        
        var chainStart = new Vector2(_width/3.0f, _height/2.0f);
        var chainEnd = new Vector2(_width * 2.0f/3.0f, _height/2.0f);
        
        var id = Random.Range(0,10000);
        
        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                var height = 0.0f;
                var pos = new Vector2(x, y);
                    
                //height = (vicinity + distModifier) * peakHeight;
                foreach (var peak in peaks) {
                    var vicinity = 1.0f - (math.distance(pos, peak.Item1) * 4.0f) / _width;
                    var dir = math.normalize(pos - new Vector2(peak.Item1.x, peak.Item1.y)) + id + peak.Item1.x * 10.0f;
                    var distModifier = fbm(dir * 2.0f) * 0.1f - noise(dir* 2.0f) * 0.01f;
                    var peakHeight = peak.Item2;
//                    height = Mathf.Max(vicinity * peakHeight + distModifier, height);
                    height = Mathf.Max(Mathf.Pow(vicinity * peakHeight + distModifier, 1.5f), height);
                }

                _heightMap[x, y] = height; // + Mathf.PerlinNoise(x * 0.02f + offset, y * 0.02f) * 0.025f;
            }
        }
    }

    private void NoiseMap() {
        var offset = Random.Range(0, 100000);
        for (var x = 0; x < 800; x++) {
            for (var y = 0; y < 800; y++) {
                var value = Mathf.PerlinNoise(x * 0.01f + offset, y * 0.01f);
                // BLUR
                if (Mathf.Abs(value - 0.5f) < 0.2f) {
                    value = 0.5f + (value - 0.5f) * 0.5f;
                }
//                value = Mathf.PerlinNoise(x * 0.0001f - offset, y * 0.0001f + offset) * 0.9f + value * 0.1f;
                //value = Mathf.PerlinNoise(x * 0.001f + offset, y * 0.001f);
                _heightMap[x, y] = value;
            }
        }
    }

}
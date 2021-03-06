using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

class TextureDrawer {
    private readonly Mesh _quadMesh;
    private readonly Material _pixelMaterial;
    private readonly Material _finishMaterial;
    private Material _drawMaterial;
    private static readonly int Zoom = Shader.PropertyToID("_Zoom");

    public TextureDrawer() {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _quadMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(gameObject);
        
        _finishMaterial = Resources.Load<Material>("FinishMaterial");
        _pixelMaterial = Resources.Load<Material>("PixelMaterial");
        _drawMaterial = _finishMaterial;
    }

    public void Draw(Texture texture, int width, int height, float zoom) {
        // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
        _drawMaterial.SetTexture("_BaseMap", texture);
        // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
        _drawMaterial.SetTexture("_MainTex", texture);
        _drawMaterial.SetFloat(Zoom, zoom);
        Debug.Assert(Camera.main != null, "Camera.main != null");
        var mat = Matrix4x4.TRS(float3.zero + Camera.main.transform.position * new float3(1,1,0), Quaternion.identity, new Vector3(width, height, 1));
        Graphics.DrawMesh(_quadMesh, mat, _drawMaterial,0);
    }

    public void SetStandard() {
        _drawMaterial = _finishMaterial;
    }
    
    
    public void SetDebug() {
        _drawMaterial = _pixelMaterial;
    }

    // Can only be called once per texture
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public void CreateAssetAndSave(Texture2D texture) {
        const string path = "Assets/resources/heightmap";
        AssetDatabase.CreateAsset(texture, path);
        File.WriteAllBytes("Assets/datafile.png", texture.EncodeToPNG());
    }

    // Can be called multiple times
    public void UpdateSave(Texture2D texture, float zoom) {
        File.WriteAllBytes("Assets/datafile.png", texture.EncodeToPNG());
        EditorApplication.isPlaying = false;
        File.WriteAllBytes("zoomLevel", BitConverter.GetBytes(zoom));
    }
}
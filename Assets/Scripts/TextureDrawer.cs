using UnityEditor;
using UnityEngine;

class TextureDrawer {
    private readonly Mesh _quadMesh;
    private readonly Material _pixelMaterial;
    private readonly Material _finishMaterial;
    private Material _drawMaterial;

    public TextureDrawer() {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _quadMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(gameObject);
        
        _finishMaterial = Resources.Load<Material>("FinishMaterial");
        _pixelMaterial = Resources.Load<Material>("PixelMaterial");
        _drawMaterial = _finishMaterial;
    }

    public void Draw(Texture texture, int width, int height) {
        // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
        _drawMaterial.SetTexture("_BaseMap", texture);
        // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
        _drawMaterial.SetTexture("_MainTex", texture);
        var mat = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(width, height, 1));
        Graphics.DrawMesh(_quadMesh, mat, _drawMaterial,0);
    }

    public void SetStandard() {
        _drawMaterial = _finishMaterial;
    }
    
    
    public void SetDebug() {
        _drawMaterial = _pixelMaterial;
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public void Save(Texture texture) {
        AssetDatabase.CreateAsset(texture, "Assets/heightmap");
    }
}
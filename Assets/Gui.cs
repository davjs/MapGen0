using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using Debug = System.Diagnostics.Debug;

public class Gui : MonoBehaviour
{
    private float _zoom = 85.0f;
    private const int Width = 800;
    private const int Height = 800;
    private MapGenerator _mapGenerator;
    private Camera _camera;

    // Start is called before the first frame update
    void Start()
    {
        _camera = Camera.main;
        if (UnityEngine.Windows.File.Exists("zoomLevel")) {
            _zoom = BitConverter.ToSingle(UnityEngine.Windows.File.ReadAllBytes("zoomLevel"),0);
        }
        _mapGenerator = new MapGenerator(Width, Height);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.mouseScrollDelta.magnitude > 0.1f) {
            _zoom = Mathf.Clamp(_zoom + Input.mouseScrollDelta.y * _zoom * 0.2f, 0.8f, 100.0f);
        }

        var t = _camera.transform;
        if (Input.GetKey(KeyCode.W)) {
            var position = t.position;
            t.position = new Vector3(position.x,position.y+10.0f/_zoom,position.z);
        }
        if (Input.GetKey(KeyCode.S)) {
            var position = t.position;
            t.position = new Vector3(position.x,position.y-10.0f/_zoom,position.z);
        }
        if (Input.GetKey(KeyCode.A)) {
            var position = t.position;
            t.position = new Vector3(position.x-10.0f/_zoom,position.y,position.z);
        }
        if (Input.GetKey(KeyCode.D)) {
            var position = t.position;
            t.position = new Vector3(position.x+10.0f/_zoom,position.y,position.z);
        }

        if (Input.GetMouseButton(0)) {
            _mapGenerator.Draw(new int2((int) Input.mousePosition.x, (int) Input.mousePosition.y));
        }
        
        TextureDrawer.Instance.Draw(_mapGenerator.GetTexture(), Width, Height, _zoom);
    }
    
    private void OnGUI() {
        const int height = 40;
        if (GUI.Button(new Rect(10, 70 + height * 0, 100, height), "Regen")) {
            _mapGenerator.ReGenerate();
        }

        if (GUI.Button(new Rect(10, 70 + height * 1, 100, height), "Save")) {
            TextureDrawer.Instance.UpdateSave(_mapGenerator.GetTexture(),_zoom);
        }

        if (GUI.Button(new Rect(10, 110 + height * 2, 100, height), "Debug")) {
            TextureDrawer.Instance.SetDebug();
        }

        if (GUI.Button(new Rect(10, 110 + height * 3, 100, height), "Standard")) {
            TextureDrawer.Instance.SetStandard();
        }
        
        if (GUI.Button(new Rect(10, 110 + height * 4, 100, height), "EnterHeightMapMode")) {
            _mapGenerator.EnterHeightMapMode();
        }
    }
}

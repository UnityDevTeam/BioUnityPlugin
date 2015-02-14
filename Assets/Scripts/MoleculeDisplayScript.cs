using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[ExecuteInEditMode]
public class MoleculeDisplayScript : MonoBehaviour
{
    public Shader MolShader;
    public Shader AtomShader;
    public Shader DepthNormalsBlitShader;

    [RangeAttribute(0, 1)]
    public float HighlightIntensity = 0.5f;

    /*****/
        
    private const int NumMoleculeInstancesMax = 25000;

    private bool _showAtomColors = false;

    private int _numMoleculeInstances = 0;
        
    private float _scale = 0;
    
    private Material _molMaterial;
    private Material _atomMaterial;
    private Material _depthNormalsBlitMaterial;

    private ComputeBuffer _drawArgsBuffer;
    private ComputeBuffer _atomDataBuffer;
    private ComputeBuffer _atomDataPdbBuffer;
    private ComputeBuffer _molColorsBuffer;
	private ComputeBuffer _molAtomCountBuffer;
	private ComputeBuffer _molAtomStartBuffer;
    private ComputeBuffer _molPositionsBuffer;
    private ComputeBuffer _molRotationsBuffer;
    private ComputeBuffer _molStatesBuffer;
    private ComputeBuffer _molTypesBuffer;

    private ComputeBuffer _atomRadiiBuffer;
    private ComputeBuffer _atomColorsBuffer;

    [NonSerialized]
    private List<int> _atomCount = new List<int>();

    [NonSerialized]
    private List<int> _atomStart = new List<int>();

    [NonSerialized]
    private List<Vector4> _atomDataPdb = new List<Vector4>();

    /*****/  

	public void Start()
	{
        camera.depthTextureMode |= DepthTextureMode.Depth;
        camera.depthTextureMode |= DepthTextureMode.DepthNormals;
    }

    void CreateResources()
    {
        if (_molTypesBuffer == null) _molTypesBuffer = new ComputeBuffer(NumMoleculeInstancesMax, 4);
        if (_molStatesBuffer == null) _molStatesBuffer = new ComputeBuffer(NumMoleculeInstancesMax, 4);
        if (_molPositionsBuffer == null) _molPositionsBuffer = new ComputeBuffer(NumMoleculeInstancesMax, 16);
        if (_molRotationsBuffer == null) _molRotationsBuffer = new ComputeBuffer(NumMoleculeInstancesMax, 16);

        if (_molAtomCountBuffer == null) _molAtomCountBuffer = new ComputeBuffer(1000, 4);
        if (_molAtomStartBuffer == null) _molAtomStartBuffer = new ComputeBuffer(1000, 4);
        if (_atomDataPdbBuffer == null) _atomDataPdbBuffer = new ComputeBuffer(1000000, 16);
        if (_molColorsBuffer == null) _molColorsBuffer = new ComputeBuffer(1000, 16);
        
        if (_atomRadiiBuffer == null)
        {
            _atomRadiiBuffer = new ComputeBuffer(PdbReader.AtomSymbols.Length, 4);
            _atomRadiiBuffer.SetData(PdbReader.AtomRadii);
        }

        if (_atomColorsBuffer == null)
        {
            _atomColorsBuffer = new ComputeBuffer(PdbReader.AtomSymbols.Length, 16);
            _atomColorsBuffer.SetData(PdbReader.AtomColors);
        }

        if (_drawArgsBuffer == null)
        {
            _drawArgsBuffer = new ComputeBuffer(1, 16, ComputeBufferType.DrawIndirect);
            _drawArgsBuffer.SetData(new[] { 0, 1, 0, 0 });
        }
        
        if (_atomDataBuffer == null)
        {
            // This number is somewhat arbitrary (1920 * 1080) ... let's hope we do not overflow this
            _atomDataBuffer = new ComputeBuffer(2073600, 32, ComputeBufferType.Append);
        }  

        if (_molMaterial == null) _molMaterial = new Material(MolShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_atomMaterial == null) _atomMaterial = new Material(AtomShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_depthNormalsBlitMaterial == null) _depthNormalsBlitMaterial = new Material(DepthNormalsBlitShader) { hideFlags = HideFlags.HideAndDontSave };
    }

    private void ReleaseResources()
    {
        //Debug.Log("Release buffers");

        if (_drawArgsBuffer != null) _drawArgsBuffer.Release();
        if (_atomDataBuffer != null) _atomDataBuffer.Release();        
        if (_molTypesBuffer != null) _molTypesBuffer.Release();
        if (_molStatesBuffer != null) _molStatesBuffer.Release();
        if (_molColorsBuffer != null) _molColorsBuffer.Release();
        if (_atomRadiiBuffer != null) _atomRadiiBuffer.Release();
        if (_atomColorsBuffer != null) _atomColorsBuffer.Release();
        if (_atomDataPdbBuffer != null) _atomDataPdbBuffer.Release();
        if (_molAtomCountBuffer != null) _molAtomCountBuffer.Release();
        if (_molAtomStartBuffer != null) _molAtomStartBuffer.Release();
        if (_molPositionsBuffer != null) _molPositionsBuffer.Release();
        if (_molRotationsBuffer != null) _molRotationsBuffer.Release();

        if (_molMaterial != null)
        {
            DestroyImmediate(_molMaterial);
            _molMaterial = null;
        }

        if (_atomMaterial != null)
        {
            DestroyImmediate(_atomMaterial);
            _atomMaterial = null;
        }

        if (_depthNormalsBlitMaterial != null)
        {
            DestroyImmediate(_depthNormalsBlitMaterial);
            _depthNormalsBlitMaterial = null;
        }
    }

    void OnDisable()
    {
        ReleaseResources();
    }

    public void AddMoleculeType(Vector4[] atoms)
    {
        CreateResources();
        
        _atomCount.Add(atoms.Length);
        _atomStart.Add(_atomDataPdb.Count);
        _atomDataPdb.AddRange(atoms);
        
        _molAtomCountBuffer.SetData(_atomCount.ToArray());        
        _molAtomStartBuffer.SetData(_atomStart.ToArray());        
        _atomDataPdbBuffer.SetData(_atomDataPdb.ToArray());
    }

    public void UpdateMoleculeData(Vector4[] positions, Vector4[] rotations, int[] types, int[] states, Color[] colors, float scale, bool showAtomColors)
    {
        if (enabled == false) return;

        CreateResources();

        _numMoleculeInstances = positions.Length;

        if (_numMoleculeInstances > NumMoleculeInstancesMax) throw new Exception("Too much instances to draw, resize compute buffers");

        _molPositionsBuffer.SetData(positions);
        _molRotationsBuffer.SetData(rotations);
        _molTypesBuffer.SetData(types);
        _molStatesBuffer.SetData(states);
        _molColorsBuffer.SetData(colors);

        _scale = scale;
        _showAtomColors = showAtomColors;
    }

    

    private void OnGUI()
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            //Debug.Log("Mouse click: " + Event.current.mousePosition);
            
            _leftMouseDown = true;
            _mousePos = Event.current.mousePosition;
        }
    }

    private bool _leftMouseDown = false;
    private Vector2 _mousePos = new Vector2();

    public int SelectedMolecule = -1;
    
    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        CreateResources();      

        // Return if no instances to draw
        if (_numMoleculeInstances == 0) { Graphics.Blit(src, dst); return; }
        
        //*** Cull atoms ***//

        _molMaterial.SetFloat("scale", _scale);
        _molMaterial.SetBuffer("molTypes", _molTypesBuffer);
        _molMaterial.SetBuffer("molStates", _molStatesBuffer);
        _molMaterial.SetBuffer("molPositions", _molPositionsBuffer);
        _molMaterial.SetBuffer("molRotations", _molRotationsBuffer);
        _molMaterial.SetBuffer("atomDataPDBBuffer", _atomDataPdbBuffer);
        _molMaterial.SetBuffer("molAtomCountBuffer", _molAtomCountBuffer);
        _molMaterial.SetBuffer("molAtomStartBuffer", _molAtomStartBuffer);

        var posTexture = RenderTexture.GetTemporary(src.width, src.height, 24, RenderTextureFormat.ARGBFloat);
        var infoTexture = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGBFloat);

        // Clear the temporary render targets
        Graphics.SetRenderTarget(new[] { posTexture.colorBuffer, infoTexture.colorBuffer }, posTexture.depthBuffer);
        GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));        

        _molMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, _numMoleculeInstances);

        _molMaterial.SetTexture("posTex", posTexture);
        _molMaterial.SetTexture("infoTex", infoTexture);

        Graphics.SetRandomWriteTarget(1, _atomDataBuffer);
        Graphics.Blit(null, dst, _molMaterial, 1);
        Graphics.ClearRandomWriteTargets();
        ComputeBuffer.CopyCount(_atomDataBuffer, _drawArgsBuffer, 0);

        RenderTexture.ReleaseTemporary(infoTexture);
        RenderTexture.ReleaseTemporary(posTexture);        

        //*** Render atoms ***//

        _atomMaterial.SetInt("showAtomColors", (_showAtomColors) ? 1 : 0);

        _atomMaterial.SetFloat("scale", _scale);
        _atomMaterial.SetFloat("highlightIntensity", HighlightIntensity);    
  
        _atomMaterial.SetBuffer("molTypes", _molTypesBuffer);
        _atomMaterial.SetBuffer("molStates", _molStatesBuffer);
        _atomMaterial.SetBuffer("molColors", _molColorsBuffer);
        _atomMaterial.SetBuffer("atomRadii", _atomRadiiBuffer);
        _atomMaterial.SetBuffer("atomColors", _atomColorsBuffer);
        _atomMaterial.SetBuffer("atomDataBuffer", _atomDataBuffer);

        var idBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        var cameraDepthBuffer = RenderTexture.GetTemporary(src.width, src.height, 24, RenderTextureFormat.Depth);
        var cameraDepthNormalBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        
        // Clear id buffer
        Graphics.SetRenderTarget(idBuffer);
        GL.Clear(true, true, new Color(1,1,1,1));

        // Fetch depth and normals from Unity
        Graphics.SetRenderTarget(cameraDepthNormalBuffer.colorBuffer, cameraDepthBuffer.depthBuffer);
        Graphics.Blit(src, _depthNormalsBlitMaterial, 0);
        
        Graphics.SetRenderTarget(new[] { src.colorBuffer, cameraDepthNormalBuffer.colorBuffer, idBuffer.colorBuffer }, cameraDepthBuffer.depthBuffer);     
        _atomMaterial.SetPass(0);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, _drawArgsBuffer);

        Shader.SetGlobalTexture("_CameraDepthTexture", cameraDepthBuffer);
        //Shader.SetGlobalTexture("_CameraDepthNormalsTexture", cameraDepthNormalBuffer);
        
        if (_leftMouseDown)
        {
            var idTexture2D = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false);
            
            RenderTexture.active = idBuffer;
            idTexture2D.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            idTexture2D.Apply();

            var color = idTexture2D.GetPixel((int)_mousePos.x, src.height - (int)_mousePos.y);

            var b = (int) (color.b*255.0f);
            var g = (int) (color.g*255.0f) << 8;
            var r = (int) (color.r*255.0f) << 16;
            
            var id = r + g + b;

            if (id == 16777215) id = -1;

            SelectedMolecule = id;
            
            //Debug.Log("Mouse click at position: " + _mousePos + " color: " + color + " id: " + id);

            DestroyImmediate(idTexture2D);
            _leftMouseDown = false;
        }

        Graphics.Blit(src, dst);

        RenderTexture.ReleaseTemporary(idBuffer);
        RenderTexture.ReleaseTemporary(cameraDepthBuffer);
        RenderTexture.ReleaseTemporary(cameraDepthNormalBuffer);
	}
}
        
		
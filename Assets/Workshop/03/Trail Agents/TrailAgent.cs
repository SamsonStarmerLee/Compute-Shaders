using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TrailAgent : MonoBehaviour
{
    [Header("Trail Agent Params")]

    [SerializeField, Range(64, 1000000)]
    int agentsCount = 1;

    [SerializeField]
    bool debugMovement = false;

    [Header("Setup")]

    [SerializeField, Range(8, 2048)]
    int rez = 8;

    [SerializeField, Range(0, 50)]
    int stepsPerFrame = 0;

    [SerializeField, Range(1, 50)]
    int stepMod = 1;

    [SerializeField, Range(0, 1)]
    float trailDecayFactor = .9f;

    [Header("Mouse Input")]

    [SerializeField, Range(0, 100)]
    int brushSize = 10;

    [SerializeField]
    GameObject interactivePlane;

    [SerializeField]
    Material outMat;

    [SerializeField]
    ComputeShader cs;

    RenderTexture readTex;
    RenderTexture writeTex;
    RenderTexture outTex;
    RenderTexture debugTex;

    int agentsDebugKernel;
    int moveAgentsKernel;
    int writeTrailsKernel;
    int renderKernel;
    int diffusionKernel;

    ComputeBuffer agentsBuffer;
    List<ComputeBuffer> buffers = new List<ComputeBuffer>();
    List<RenderTexture> textures = new List<RenderTexture>();

    int stepn = -1;
    Vector2 hitXY;

    private void Start()
    {
        Reset();
    }

    [Button]

    public void Reset()
    {
        Release();

        agentsDebugKernel = cs.FindKernel("AgentsDebugKernel");
        moveAgentsKernel = cs.FindKernel("MoveAgentsKernel");
        writeTrailsKernel = cs.FindKernel("WriteTrailsKernel");
        renderKernel = cs.FindKernel("RenderKernel");
        diffusionKernel = cs.FindKernel("DiffusionKernel");

        readTex = CreateTexture(rez, FilterMode.Point);
        writeTex = CreateTexture(rez, FilterMode.Point);
        outTex = CreateTexture(rez, FilterMode.Point);
        debugTex = CreateTexture(rez, FilterMode.Point);

        agentsBuffer = new ComputeBuffer(agentsCount, sizeof(float) * 4);
        buffers.Add(agentsBuffer);

        GPUResetKernel();
        Render();
    }

    private void GPUResetKernel()
    {
        cs.SetInt("rez", rez);
        cs.SetInt("time", Time.frameCount);

        var kernel = cs.FindKernel("ResetTextureKernel");

        cs.SetTexture(kernel, "writeTex", writeTex);
        cs.Dispatch(kernel, rez, rez, 1);

        cs.SetTexture(kernel, "writeTex", readTex);
        cs.Dispatch(kernel, rez, rez, 1);

        kernel = cs.FindKernel("ResetAgentsKernel");
        cs.SetBuffer(kernel, "agentsBuffer", agentsBuffer);
        cs.Dispatch(kernel, agentsCount / 64, 1, 1);
    }

    private void Update()
    {
        if (Time.frameCount % stepMod == 0)
        {
            for (var i = 0; i < stepsPerFrame; i++)
            {
                Step();
            }
        }
    }

    [Button]
    private void Step()
    {
        HandleInput();

        stepn += 1;
        cs.SetInt("stepn", stepn);
        cs.SetInt("time", Time.frameCount);
        cs.SetInt("brushSize", brushSize);
        cs.SetVector("hitXY", hitXY);

        MoveAgentsKernel();

        if (stepn % 2 == 1)
        {
            DiffusionKernel();
            WriteTrailsKernel();
            SwapTex();
        }

        Render();
    }

    void HandleInput()
    {
        if (!Input.GetMouseButton(0))
        {
            hitXY = Vector2.zero;
            return;
        }

        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            return;
        }

        if (hit.transform != interactivePlane.transform)
        {
            return;
        }

        hitXY = hit.textureCoord * rez;
    }

    public void DiffusionKernel()
    {
        cs.SetTexture(diffusionKernel, "readTex", readTex);
        cs.SetTexture(diffusionKernel, "writeTex", writeTex);
        cs.SetFloat("trailDecayFactor", trailDecayFactor);

        cs.Dispatch(diffusionKernel, rez, rez, 1);
    }

    private void MoveAgentsKernel()
    {
        cs.SetBuffer(moveAgentsKernel, "agentsBuffer", agentsBuffer);
        cs.SetTexture(moveAgentsKernel, "readTex", readTex);
        cs.SetTexture(moveAgentsKernel, "debugTex", debugTex);

        cs.Dispatch(moveAgentsKernel, agentsCount / 64, 1, 1);
    }

    private void WriteTrailsKernel()
    {
        cs.SetBuffer(writeTrailsKernel, "agentsBuffer", agentsBuffer);
        cs.SetTexture(writeTrailsKernel, "writeTex", writeTex);

        cs.Dispatch(writeTrailsKernel, agentsCount / 64, 1, 1);
    }

    private void SwapTex()
    {
        var tmp = readTex;
        readTex = writeTex;
        writeTex = tmp;
    }

    private void Render()
    {
        RenderKernel();
        
        if (debugMovement)
        {
            AgentsDebugKernel();
        }

        outMat.SetTexture("_UnlitColorMap", outTex);

        if (!Application.isPlaying)
        {
            UnityEditor.SceneView.RepaintAll();
        }
    }

    private void RenderKernel()
    {
        cs.SetTexture(renderKernel, "readTex", readTex);
        cs.SetTexture(renderKernel, "outTex", outTex);
        cs.SetTexture(renderKernel, "debugTex", debugTex);

        cs.Dispatch(renderKernel, rez, rez, 1);
    }

    private void AgentsDebugKernel()
    {
        cs.SetBuffer(agentsDebugKernel, "agentsBuffer", agentsBuffer);
        cs.SetTexture(agentsDebugKernel, "outTex", outTex);

        cs.Dispatch(agentsDebugKernel, agentsCount / 64, 1, 1);
    }

    public void Release()
    {
        if (buffers != null)
        {
            foreach (var buffer in buffers)
            {
                if (buffer != null)
                {
                    buffer.Release();
                }
            }
        }

        buffers = new List<ComputeBuffer>();

        if (textures != null)
        {
            foreach (var tex in textures)
            {
                if (tex != null)
                {
                    tex.Release();
                }
            }
        }

        textures = new List<RenderTexture>();
    }

    private void OnDestroy()
    {
        Release();
    }

    private void OnEnable()
    {
        Release();
    }

    private void OnDisable()
    {
        Release();
    }

    private RenderTexture CreateTexture(int r, FilterMode filtermode)
    {
        var texture = new RenderTexture(r, r, 1, RenderTextureFormat.ARGBFloat);

        texture.name = "out";
        texture.enableRandomWrite = true;
        texture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        texture.volumeDepth = 1;
        texture.filterMode = filtermode;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.autoGenerateMips = false;
        texture.useMipMap = false;
        texture.Create();

        textures.Add(texture);
        return texture;
    }
}
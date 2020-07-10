using NaughtyAttributes;
using UnityEngine;

[ExecuteAlways]
public class ConwayGPUSim : MonoBehaviour
{
    [Range(0, 10)]
    public int stepsPerFrame;

    [Range(0, 1024)]
    public int rez = 32;

    [Range(0, 10)]
    public float offset = .2f;

    public Material outputMaterial;
    public Material postProcessMaterial;

    public ComputeShader compute;

    private RenderTexture texA;
    private RenderTexture texB;
    private RenderTexture postTex;

    void Start()
    {
        Reset();
    }

    [Button]
    public void Reset()
    {
        texA = CreateTexture(RenderTextureFormat.ARGBFloat);
        texB = CreateTexture(RenderTextureFormat.ARGBFloat);
        postTex = CreateTexture(RenderTextureFormat.ARGBFloat);

        GPUResetKernel();

        outputMaterial.SetTexture("_UnlitColorMap", texA);
        postProcessMaterial.SetTexture("_UnlitColorMap", postTex);
    }

    void GPUResetKernel()
    {
        var kernel = compute.FindKernel("ResetKernel"); // Find that function on the GPU
        compute.SetTexture(kernel, "writeTex", texA);
        compute.SetInt("rez", rez);
        compute.SetFloat("offset", offset);
        compute.Dispatch(kernel, rez, rez, 1);
    }

    void Update()
    {
        for (var i = 0; i < stepsPerFrame; i++)
        {
            Step();
        }
    }

    [Button]
    public void Step()
    {
        GPUStepKernel();
        GPURenderKernel();

        outputMaterial.SetTexture("_UnlitColorMap", texA);
        postProcessMaterial.SetTexture("_UnlitColorMap", postTex);
    }

    private void GPUStepKernel()
    {
        var kernel = compute.FindKernel("StepKernel");
        compute.SetTexture(kernel, "readTex", texA);
        compute.SetTexture(kernel, "writeTex", texB);
        compute.Dispatch(kernel, rez, rez, 1);

        SwapTextures();
    }

    private void GPURenderKernel()
    {
        var kernel = compute.FindKernel("RenderKernel");
        compute.SetTexture(kernel, "readTex", texA);
        compute.SetTexture(kernel, "postTex", postTex);
        compute.Dispatch(kernel, rez, rez, 1);
    }

    private void SwapTextures()
    {
        var tmp = texA;
        texA = texB;
        texB = tmp;
    }

    protected RenderTexture CreateTexture(RenderTextureFormat format)
    {
        var texture = new RenderTexture(rez, rez, 1, format);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.useMipMap = false;
        texture.Create();
        return texture;
    }
}

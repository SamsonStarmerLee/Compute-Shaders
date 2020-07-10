using NaughtyAttributes;
using UnityEngine;

[ExecuteAlways]
public class RPSSim : MonoBehaviour
{
    [Range(0, 10)]
    public int stepsPerFrame;

    [Range(0, 1024)]
    [SerializeField] int rez = 256;

    [Range(0, 100)]
    [SerializeField] int hands = 3;

    [Range(0, 9)]
    [SerializeField] int threshold = 3;

    [Range(0, 9)]
    [SerializeField] int jitter = 2;

    [SerializeField] ComputeShader compute;
    [SerializeField] Material material;

    private int ThreadRez => rez / 8;

    private RenderTexture texA;
    private RenderTexture texB;
    private RenderTexture postTex;

    void Update()
    {
        for (var i = 0; i < stepsPerFrame; i++)
        {
            Step();
        }
    }

    private void Start()
    {
        Reset();
    }

    [Button]
    private void Reset()
    {
        texA = CreateTexture(RenderTextureFormat.RFloat);
        texB = CreateTexture(RenderTextureFormat.RFloat);
        postTex = CreateTexture(RenderTextureFormat.ARGBFloat);

        ResetKernel();

        material.SetTexture("_UnlitColorMap", postTex);
    }

    [Button]
    private void Step()
    {
        StepKernel();
        SwapTextures();
        RenderKernel();

        material.SetTexture("_UnlitColorMap", postTex);
    }

    private void ResetKernel()
    {
        var kernel = compute.FindKernel("ResetKernel");
        compute.SetTexture(kernel, "writeTex", texA);
        compute.SetInt("hands", hands);
        compute.SetInt("rez", rez);
        compute.SetInt("threshold", threshold);
        compute.SetInt("jitter", jitter);
        compute.Dispatch(kernel, ThreadRez, ThreadRez, 1);
    }

    private void StepKernel()
    {
        var kernel = compute.FindKernel("StepKernel");
        compute.SetTexture(kernel, "readTex", texA);
        compute.SetTexture(kernel, "writeTex", texB);
        compute.SetFloat("time", Time.time);
        compute.Dispatch(kernel, ThreadRez, ThreadRez, 1);
    }

    private void RenderKernel()
    {
        var kernel = compute.FindKernel("RenderKernel");
        compute.SetTexture(kernel, "readTex", texA);
        compute.SetTexture(kernel, "postTex", postTex);
        compute.Dispatch(kernel, ThreadRez, ThreadRez, 1);
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

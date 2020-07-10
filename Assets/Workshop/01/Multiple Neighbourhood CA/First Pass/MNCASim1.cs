using NaughtyAttributes;
using UnityEngine;

[ExecuteAlways]
public class MNCASim1 : MonoBehaviour
{
    [Range(0, 10)]
    public int stepsPerFrame;

    [Range(0, 1024)]
    public int Rez = 256;

    public Material OutputMaterial;
    public ComputeShader Compute;

    private RenderTexture texA;
    private RenderTexture texB;
    private RenderTexture postTex;

    [Button]
    public void Reset()
    {
        texA = CreateTexture(RenderTextureFormat.RFloat);
        texB = CreateTexture(RenderTextureFormat.RFloat);
        postTex = CreateTexture(RenderTextureFormat.ARGBFloat);

        ResetKernel();

        OutputMaterial.SetTexture("_UnlitColorMap", postTex);
    }

    void ResetKernel()
    {
        var kernel = Compute.FindKernel("ResetKernel");
        Compute.SetTexture(kernel, "writeTex", texA);
        Compute.SetInt("rez", Rez);
        Compute.Dispatch(kernel, Rez, Rez, 1);
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
        StepKernel();
        RenderKernel();

        OutputMaterial.SetTexture("_UnlitColorMap", postTex);
    }

    void StepKernel()
    {
        var kernel = Compute.FindKernel("StepKernel");
        Compute.SetTexture(kernel, "readTex", texA);
        Compute.SetTexture(kernel, "writeTex", texB);
        Compute.Dispatch(kernel, Rez, Rez, 1);

        SwapTextures();
    }

    void RenderKernel()
    {
        var kernel = Compute.FindKernel("RenderKernel");
        Compute.SetTexture(kernel, "readTex", texA);
        Compute.SetTexture(kernel, "postTex", postTex);
        Compute.Dispatch(kernel, Rez, Rez, 1);
    }

    private void SwapTextures()
    {
        var tmp = texA;
        texA = texB;
        texB = tmp;
    }

    private RenderTexture CreateTexture(RenderTextureFormat format)
    {
        var texture = new RenderTexture(Rez, Rez, 1, format);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.useMipMap = false;
        texture.Create();
        return texture;
    }
}


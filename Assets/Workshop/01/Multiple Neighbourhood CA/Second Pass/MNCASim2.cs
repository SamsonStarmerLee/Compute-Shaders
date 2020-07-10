using NaughtyAttributes;
using UnityEngine;

[ExecuteAlways]
public class MNCASim2 : MonoBehaviour
{
    [Range(0, 100)]
    public int stepsPerFrame;

    [Range(0, 100)]
    public int stepMod;

    [Range(0, 1024)]
    public int Rez = 256;
    public int ThreadedRez => Rez / 8;

    public Material OutputMaterial;
    public ComputeShader Compute;

    private RenderTexture texA;
    private RenderTexture texB;

    public Neighborhood neighbourhood0;
    public Neighborhood neighbourhood1;
    public Neighborhood neighbourhood2;
    public Neighborhood neighbourhood3;

    [Button]
    void Reset()
    {
        texA = CreateTexture(RenderTextureFormat.ARGBFloat);
        texB = CreateTexture(RenderTextureFormat.ARGBFloat);

        ResetKernel();

        OutputMaterial.SetTexture("_UnlitColorMap", texA);
    }

    void ResetKernel()
    {
        var kernel = Compute.FindKernel("ResetKernel");
        Compute.SetTexture(kernel, "writeTex", texA);
        Compute.SetInt("rez", Rez);

        var n0 = neighbourhood0.GetOffsets();
        Compute.SetVectorArray("neighbourhood0", n0);
        Compute.SetInt("entries0", n0.Length);

        var n1 = neighbourhood1.GetOffsets();
        Compute.SetVectorArray("neighbourhood1", n1);
        Compute.SetInt("entries1", n1.Length);

        var n2 = neighbourhood2.GetOffsets();
        Compute.SetVectorArray("neighbourhood2", n2);
        Compute.SetInt("entries2", n2.Length);

        var n3 = neighbourhood3.GetOffsets();
        Compute.SetVectorArray("neighbourhood3", n3);
        Compute.SetInt("entries3", n3.Length);

        Compute.Dispatch(kernel, ThreadedRez, ThreadedRez, 1);
    }

    void Update()
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
    void Step()
    {
        StepKernel();

        OutputMaterial.SetTexture("_UnlitColorMap", texA);
    }

    void StepKernel()
    {
        var kernel = Compute.FindKernel("StepKernel");
        Compute.SetTexture(kernel, "readTex", texA);
        Compute.SetTexture(kernel, "writeTex", texB);

        Compute.Dispatch(kernel, ThreadedRez, ThreadedRez, 1);

        SwapTextures();
    }

    void SwapTextures()
    {
        var tmp = texA;
        texA = texB;
        texB = tmp;
    }

    RenderTexture CreateTexture(RenderTextureFormat format)
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

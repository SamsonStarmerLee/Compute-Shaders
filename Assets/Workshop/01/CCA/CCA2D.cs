using UnityEngine;
using NaughtyAttributes;

public class CCA2D : MonoBehaviour
{
    [Header("CCA Primary Params")]

    const int MAX_RANGE = 10;
    [Range(1, MAX_RANGE)]
    public int range = 1;

    const int MAX_THRESHOLD = 25;
    [Range(0, MAX_THRESHOLD)]
    public int threshold = 3;

    const int MAX_STATES = 20;
    [Range(0, MAX_STATES)]
    public int nstates = 3;

    public bool moore = true;

    [Header("CCA Secondary Params")]

    [Range(1, MAX_RANGE)]
    public int range2 = 1;

    [Range(0, MAX_THRESHOLD)]
    public int threshold2 = 3;

    [Range(0, MAX_STATES)]
    public int nstates2 = 3;

    public bool moore2 = true;

    [Header("Setup")]

    [Range(8, 2048)]
    public int rez = 8;

    [Range(0, 50)]
    public int stepsPerFrame = 0;

    [Range(1, 50)]
    public int stepMod = 1;

    public ComputeShader cs;
    public Material outMat;

    private RenderTexture outTex;
    private RenderTexture readTex;
    private RenderTexture writeTex;

    private int stepKernel;

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

    private void Start()
    {
        Reset();
    }

    [Button]
    private void Reset()
    {
        readTex = CreateTexture(RenderTextureFormat.RFloat);
        writeTex = CreateTexture(RenderTextureFormat.RFloat);
        outTex = CreateTexture(RenderTextureFormat.ARGBFloat);

        stepKernel = cs.FindKernel("StepKernel");

        GPUResetKernel();
    }

    [Button]
    private void RandomizeParams()
    {
        range = Random.Range(1, MAX_RANGE + 1);
        threshold = Random.Range(0, MAX_THRESHOLD + 1);
        nstates = Random.Range(0, MAX_STATES + 1);
        moore = Random.Range(0f, 1f) <= 0.5;

        cs.SetInt("range", range);
        cs.SetInt("threshold", threshold);
        cs.SetInt("nstates", nstates);
        cs.SetBool("moore", moore);
    }

    [Button]
    private void RandomizeSecondaryParams()
    {
        range2 = Random.Range(1, MAX_RANGE + 1);
        threshold2 = Random.Range(0, MAX_THRESHOLD + 1);
        nstates2 = Random.Range(0, MAX_STATES + 1);
        moore2 = Random.Range(0f, 1f) <= 0.5;

        cs.SetInt("range", range2);
        cs.SetInt("threshold", threshold2);
        cs.SetInt("nstates", nstates2);
        cs.SetBool("moore", moore2);
    }

    [Button]
    public void RandomizeSecondaryParamsWithNoise()
    {
        RandomizeSecondaryParams();
        AddNoise();
    }

    [Button]
    public void AddNoise()
    {
        var kernel = cs.FindKernel("SecondaryNoiseKernel");
        cs.SetTexture(kernel, "readTex", readTex);
        cs.SetTexture(kernel, "writeTex", writeTex);
        cs.Dispatch(kernel, rez / 8, rez / 8, 1);

        SwapTex();
    }

    [Button]
    public void SetPrimaryParams()
    {
        cs.SetInt("range", range);
        cs.SetInt("threshold", threshold);
        cs.SetInt("nstates", nstates);
        cs.SetBool("moore", moore);
    }

    [Button]
    public void SetSecondaryParams()
    {
        cs.SetInt("range", range2);
        cs.SetInt("threshold", threshold2);
        cs.SetInt("nstates", nstates2);
        cs.SetBool("moore", moore2);
    }

    [Button]
    private void ResetAndRandomize()
    {
        RandomizeParams();
        RandomizeColors();
        Reset();
    }

    [Button]
    public void RandomizeColors()
    {
        // Step 1: Create a gradient.
        var g = new Gradient();
        var keycount = 8; // We might like to set this to equal MAX_STATES, but 8 is the limit.
        var c = new GradientColorKey[keycount];
        var a = new GradientAlphaKey[keycount];

        float hueMax = 1f, hueMin = 0f, sMax = 2f, sMin = 0f, vMax = 1f, vMin = 0f;

        for (var i = 0; i < keycount; i++)
        {
            var h = Random.Range(0f, 1f) * (hueMax - hueMin) + hueMin;
            var s = Random.Range(0f, 1f) * (sMax - sMin) + sMin;
            var v = Random.Range(0f, 1f) * (vMax - vMin) + vMin;
            var nc = Color.HSVToRGB(h, s, v);
            c[i].color = nc;
            a[i].time = c[i].time = (i * (1f / keycount));
            a[i].alpha = 1f;
        }

        g.SetKeys(c, a);

        // Step 2: Sample colours from the gradient
        // This gives more 'related' colors than just selecting random ones
        var colors = new Vector4[nstates];

        for (var i = 0; i < nstates; i++)
        {
            var t = Random.Range(0f, 1f);
            colors[i] = g.Evaluate(t);
        }

        cs.SetVectorArray("colors", colors);
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

    private void GPUResetKernel()
    {
        var k = cs.FindKernel("ResetKernel");
        cs.SetTexture(k, "writeTex", writeTex);

        cs.SetInt("range", range);
        cs.SetInt("threshold", threshold);
        cs.SetInt("nstates", nstates);
        cs.SetBool("moore", moore);

        cs.SetInt("rez", rez);
        cs.Dispatch(k, rez / 8, rez / 8, 1);
        SwapTex();
    }

    [Button]
    public void Step()
    {
        cs.SetTexture(stepKernel, "readTex", readTex);
        cs.SetTexture(stepKernel, "writeTex", writeTex);
        cs.SetTexture(stepKernel, "outTex", outTex);

        cs.Dispatch(stepKernel, rez / 8, rez / 8, 1);

        SwapTex();

        outMat.SetTexture("_UnlitColorMap", outTex);
    }

    private void SwapTex()
    {
        var tmp = readTex;
        readTex = writeTex;
        writeTex = tmp;
    }
}

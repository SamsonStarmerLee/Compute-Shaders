using UnityEngine;

[ExecuteAlways]
public class TexInstancer : MonoBehaviour
{
    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material material;

    [SerializeField]
    Material inputMaterial;

    [SerializeField, Range(0, 10)]
    float sizeY = .5f;

    [SerializeField, Range(0f, .5f)]
    float size = .5f;

    [SerializeField, Range(0f, .5f)]
    float spacing = .5f;

    ComputeBuffer argBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0, };

    readonly Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 100);

    private void Update()
    {
        if (argBuffer != null)
        {
            argBuffer.Release();
        }


        var tex = inputMaterial.GetTexture("_UnlitColorMap");
        if (tex)
        {
            int rez = tex.width;

            argBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            material.SetTexture("tex", tex);

            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)(rez * rez);
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            argBuffer.SetData(args);

            material.SetMatrix("mat", transform.localToWorldMatrix);
            material.SetVector("position", transform.position);
            material.SetInt("rez", rez);
            material.SetFloat("size", size);
            material.SetFloat("spacing", spacing);
            material.SetFloat("sizey", sizeY);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argBuffer);
        }
    }
}

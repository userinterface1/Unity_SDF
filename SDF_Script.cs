using System.IO;
using UnityEngine;

public class SDF_Script : MonoBehaviour
{
    public ComputeShader SDF_Compute;
    public Texture2D inputTexture;
    public RenderTexture RT1;
    public RenderTexture RT2;

    public ComputeShader SDF_Length_Compute;

    public ComputeShader SDF_Province_Compute;

    void Start()
    {
        GenerateSDF();
        //SDF_Province_Generate();
    }

    void GenerateSDF()
    {
        int kernelHandle = SDF_Compute.FindKernel("CSMain");

        SDF_Compute.SetTexture(kernelHandle, "Input", inputTexture);

        RT1 = new RenderTexture(inputTexture.width, inputTexture.height, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
        RT1.enableRandomWrite = true;
        RT1.Create();

        RT2 = new RenderTexture(inputTexture.width, inputTexture.height, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
        RT2.enableRandomWrite = true;
        RT2.Create();

        // 커널 디스패치 (텍스처 크기에 맞게 설정)
        int threadGroupsX = Mathf.CeilToInt(inputTexture.width / 16.0f);
        int threadGroupsY = Mathf.CeilToInt(inputTexture.height / 16.0f);

        SDF_Compute.SetInt("step", 32);
        SDF_Compute.SetVector("seedColor", new Vector4(0, 0, 0, 1));
        SDF_Compute.SetTexture(kernelHandle, "Output", RT1);

        SDF_Compute.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

        SDF_Compute.SetTexture(kernelHandle, "Input", RT1);
        SDF_Compute.SetTexture(kernelHandle, "Output", RT2);
        SDF_Compute.SetInt("step", 31);

        SDF_Compute.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

        for (int i = 30; i > 0; i -= 2)
        {
            //1
            SDF_Compute.SetTexture(kernelHandle, "Input", RT2);
            SDF_Compute.SetTexture(kernelHandle, "Output", RT1);
            SDF_Compute.SetInt("step", i + 1);

            SDF_Compute.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

            //2
            SDF_Compute.SetTexture(kernelHandle, "Input", RT1);
            SDF_Compute.SetTexture(kernelHandle, "Output", RT2);
            SDF_Compute.SetInt("step", i);

            SDF_Compute.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

        }

        int kernelHandle2 = SDF_Length_Compute.FindKernel("CSMain2");
        SDF_Length_Compute.SetTexture(kernelHandle2, "Input", RT2);
        SDF_Length_Compute.SetTexture(kernelHandle2, "Output", RT1);

        SDF_Length_Compute.Dispatch(kernelHandle2, threadGroupsX, threadGroupsY, 1);

        SaveRenderTextureToPNG(RT1);
    }

    private void SDF_Province_Generate()
    {
        int kernelHandle = SDF_Province_Compute.FindKernel("CSMain3");

        RT1 = new RenderTexture(inputTexture.width, inputTexture.height, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
        RT1.enableRandomWrite = true;
        RT1.Create();

        RT2 = new RenderTexture(inputTexture.width, inputTexture.height, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
        RT2.enableRandomWrite = true;
        RT2.Create();

        int threadGroupsX = Mathf.CeilToInt(inputTexture.width / 16.0f);
        int threadGroupsY = Mathf.CeilToInt(inputTexture.height / 16.0f);

        SDF_Province_Compute.SetTexture(kernelHandle, "Input", inputTexture);
        SDF_Province_Compute.SetTexture(kernelHandle, "Output", RT1);

        SDF_Province_Compute.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

        SaveRenderTextureToPNG(RT1);
    }

    void SaveRenderTextureToPNG(RenderTexture rt)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/SDF.png", bytes);

        Debug.Log(Application.dataPath + "/SDF.png");
    }
}

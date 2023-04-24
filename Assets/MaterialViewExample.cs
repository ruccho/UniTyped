using UnityEngine;
using UniTyped;

public class MaterialViewExample : MonoBehaviour
{
    [SerializeField] private Material mat = default;

    void Update()
    {
        var view = new NewShaderGraphView()
        {
            Target = mat
        };
        

        view._Color = Color.HSVToRGB(Time.time % 1f, 1f, 1f);

    }
}

[UniTypedMaterialView("New Shader Graph.shadergraph")]
public partial struct NewShaderGraphView
{
    
}



[UniTypedMaterialView("NewUnlitShader.shader")]
public partial struct NewUnlitShaderView
{
    
}
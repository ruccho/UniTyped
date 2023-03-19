using UnityEngine;
using UniTyped;

public class MaterialViewExample : MonoBehaviour
{
    [SerializeField] private Material mat = default;

    void Update()
    {
        var view = new NewUnlitShaderView()
        {
            Target = mat
        };
        

        view._Color = Color.HSVToRGB(Time.time % 1f, 1f, 1f);

    }
}

[UniTypedMaterialView("NewUnlitShader.shader")]
public partial struct NewUnlitShaderView
{
    
}

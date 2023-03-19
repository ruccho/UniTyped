using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniTyped;

public class MaterialViewExample : MonoBehaviour
{
    [SerializeField] private Material unlitMat = default;
    
    void Start()
    {
        
    }

    void Update()
    {
        var view = new NewUnlitShaderView()
        {
            Target = unlitMat
        };
        

        view._Color = Color.HSVToRGB(Time.time % 1f, 1f, 1f);

    }
}

[UniTypedMaterialView("NewUnlitShader.shader")]
public partial struct NewUnlitShaderView
{
    
}

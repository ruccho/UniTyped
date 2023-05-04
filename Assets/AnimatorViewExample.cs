using UnityEngine;
using UniTyped;

[UniTypedAnimatorView("New Animator Controller.controller")]
public partial struct NewAnimatorControllerView
{
    
}

public class AnimatorViewExample : MonoBehaviour
{

    private Animator animator;
    
    void Update()
    {
        if (!animator && !TryGetComponent(out animator)) return;
        
        var view = new NewAnimatorControllerView()
        {
            Target = animator
        };

        // Float
        view.FloatParameter = Time.time;
        view.SetFloatParameter(Time.time, 0f, 0f);
        
        // Int
        view.IntParameter = Time.frameCount;
        
        // Bool
        view.BoolParameter = true;
        
        // Trigger
        view.TriggerA();
        view.ResetTriggerA();
    }
}
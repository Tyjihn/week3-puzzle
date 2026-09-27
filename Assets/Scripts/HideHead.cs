using UnityEngine;
using UnityEngine.Rendering;

public class HideHead : MonoBehaviour
{
    [Tooltip("Drag the 'head' bone from your Armature here")]
    public Transform headBone;

    [Tooltip("Spawn an invisible copy of the body that only casts a shadow, so the shadow keeps its head")]
    public bool keepHeadInShadow = true;

    // Awake runs before any Start, so PlayerAnimation finds the shadow copy's Animator too
    void Awake()
    {
        if (!keepHeadInShadow || headBone == null) return;

        Animator visibleModel = headBone.GetComponentInParent<Animator>();
        if (visibleModel == null) return;

        // The copy has its own bones, so hiding the visible head won't affect it
        GameObject shadowModel = Instantiate(visibleModel.gameObject, visibleModel.transform.parent);
        shadowModel.name = visibleModel.name + " (Shadow)";

        foreach (Renderer r in shadowModel.GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        // The visible body no longer casts a shadow, since the copy handles it
        foreach (Renderer r in visibleModel.GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    // LateUpdate runs AFTER the Animator updates, preventing the animation from resetting the scale
    void LateUpdate()
    {
        if (headBone != null)
        {
            headBone.localScale = Vector3.zero;
        }
    }
}

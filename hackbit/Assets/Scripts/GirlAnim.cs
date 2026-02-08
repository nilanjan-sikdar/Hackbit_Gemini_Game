using UnityEngine;

public class AnimationLooper : MonoBehaviour
{
    public Animator animator;       // Assign your Animator in Inspector
    public string animationName;    // The name of the animation clip

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            // Start playing the animation
            animator.Play(animationName, 0, 0f);

            // Make sure it loops
            animator.SetBool("Loop", true);
        }
    }
}

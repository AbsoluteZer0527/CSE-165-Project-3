using UnityEngine;
using UnityEngine.InputSystem;

public class AnimTest : MonoBehaviour
{
    private Animator anim;
    void Start() => anim = GetComponent<Animator>();

    void Update()
    {
        // Hold Space = walk, release = idle
        anim.SetBool("isWalking", Keyboard.current.spaceKey.isPressed);
    }
}
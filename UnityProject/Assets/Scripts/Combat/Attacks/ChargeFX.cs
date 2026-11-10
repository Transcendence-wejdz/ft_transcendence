using UnityEngine;

// Flipbook of a held charge: the start frames once, then the loop frames until the player destroys it.
public class ChargeFX : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] startFrames;
    [SerializeField] private Sprite[] loopFrames;
    [SerializeField] private float frameRate = 12f;

    private float elapsed;

    public void Init(Color color)
    {
        spriteRenderer.color = color;
    }

    private Sprite FrameAt(int index)
    {
        if (index < startFrames.Length)
            return startFrames[index];
        return loopFrames[(index - startFrames.Length) % loopFrames.Length];
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        spriteRenderer.sprite = FrameAt(Mathf.FloorToInt(elapsed * frameRate));
    }
}

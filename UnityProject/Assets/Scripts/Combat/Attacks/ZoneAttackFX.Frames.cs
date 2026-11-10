using System.Collections.Generic;
using UnityEngine;

// Zone flipbook: phases Start, Loop, End with an optional overlay layer.
public partial class ZoneAttackFX
{
    private static Sprite[] OrEmpty(Sprite[] frames)
    {
        if (frames == null)
            return new Sprite[0];
        return frames;
    }

    private Sprite[] Frames(Phase p)
    {
        if (p == Phase.Start)
            return OrEmpty(startFrames);
        if (p == Phase.Loop)
            return OrEmpty(loopFrames);
        return OrEmpty(endFrames);
    }

    private Sprite[] OverlayFrames(Phase p)
    {
        if (p == Phase.Start)
            return OrEmpty(overlayStartFrames);
        if (p == Phase.Loop)
            return OrEmpty(overlayLoopFrames);
        return OrEmpty(overlayEndFrames);
    }

    // Shows frame index of the current phase on the zone and, when it has one, on the overlay.
    private void SetFrame(int index)
    {
        Sprite[] overlay;

        if (spriteRenderer != null)
            spriteRenderer.sprite = Frames(phase)[index];
        if (overlayRenderer == null)
            return;
        overlay = OverlayFrames(phase);
        if (index < overlay.Length)
            overlayRenderer.sprite = overlay[index];
        else
            overlayRenderer.sprite = null;
    }

    // Start leads into Loop, End past the last phase (which destroys the FX).
    private Phase NextPhase()
    {
        if (phase == Phase.Start)
            return Phase.Loop;
        return Phase.End + 1;
    }

    // Steps one frame; the loop wraps, other phases move on; false once the phase changed.
    private bool StepFrame(int frameCount)
    {
        frameIndex++;
        if (frameIndex < frameCount)
        {
            SetFrame(frameIndex);
            return true;
        }
        if (phase == Phase.Loop)
        {
            frameIndex = 0;
            SetFrame(0);
            return true;
        }
        EnterPhase(NextPhase());
        return false;
    }

    private void AdvanceFrames()
    {
        Sprite[] frames = Frames(phase);
        float frameDuration = 1f / frameRate;

        if (frames.Length == 0)
            return;
        frameTimer += Time.deltaTime;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            if (!StepFrame(frames.Length))
                return;
        }
    }
}

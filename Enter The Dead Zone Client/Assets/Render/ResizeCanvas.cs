using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

static class CanvasExtensions
{
    public static Vector2 SizeToParent(this RawImage Image, float Padding = 0)
    {
        var Parent = Image.transform.parent.GetComponentInParent<RectTransform>();
        var Transform = Image.GetComponent<RectTransform>();
        if (!Parent) { return Transform.sizeDelta; } //if we don't have a parent, just return our current width
        Padding = 1 - Padding;
        float Ratio = Image.texture.width / (float)Image.texture.height;
        var Bounds = new Rect(0, 0, Parent.rect.width, Parent.rect.height);
        if (Mathf.RoundToInt(Transform.eulerAngles.z) % 180 == 90)
        {
            //Invert the bounds if the image is rotated
            Bounds.size = new Vector2(Bounds.height, Bounds.width);
        }
        //Size by height first
        float Height = Bounds.height * Padding;
        float Width = Height * Ratio;
        if (Width > Bounds.width * Padding)
        { //If it doesn't fit, fallback to width
            Width = Bounds.width * Padding;
            Height = Width / Ratio;
        }
        Transform.sizeDelta = new Vector2(Width, Height);
        return Transform.sizeDelta;
    }
}

public class ResizeCanvas : MonoBehaviour
{
    RawImage Render;

    // Start is called before the first frame update
    void Start()
    {
        Render = GetComponent<RawImage>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Render != null) Render.SizeToParent();
    }
}

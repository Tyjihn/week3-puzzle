using UnityEngine;

// Draws an OnGUI label with a gray backing box sized to fit the text, for readable hover prompts.
public static class HoverPromptGUI
{
    static readonly Color BackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
    const float PaddingX = 14f;
    const float PaddingY = 6f;
    const float CornerRadius = 8f;

    public static void Draw(Rect area, string text, GUIStyle style)
    {
        GUIContent content = new GUIContent(text);
        Vector2 textSize = style.CalcSize(content);

        float width = Mathf.Min(textSize.x + PaddingX * 2f, area.width);
        float height = textSize.y + PaddingY * 2f;

        Rect box = new Rect(
            area.center.x - width / 2f,
            area.center.y - height / 2f,
            width,
            height
        );

        GUI.DrawTexture(
            box,
            Texture2D.whiteTexture,
            ScaleMode.StretchToFill,
            true,
            0f,
            BackgroundColor,
            0f,
            CornerRadius
        );

        // Keep the text color fixed so it doesn't change when the mouse hovers over it.
        GUIStyle labelStyle = new GUIStyle(style);
        Color textColor = style.normal.textColor;
        labelStyle.hover.textColor = textColor;
        labelStyle.active.textColor = textColor;
        labelStyle.focused.textColor = textColor;
        labelStyle.onNormal.textColor = textColor;
        labelStyle.onHover.textColor = textColor;
        labelStyle.onActive.textColor = textColor;
        labelStyle.onFocused.textColor = textColor;

        GUI.Label(area, content, labelStyle);
    }
}

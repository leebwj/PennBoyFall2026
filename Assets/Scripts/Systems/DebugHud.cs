using UnityEngine;

public class DebugHud : MonoBehaviour
{
    [SerializeField] private Damageable playerHealth;
    [SerializeField] private PlayerController player;
    [SerializeField] private bool visible = true;

    private GUIStyle label;
    private Texture2D barFill;
    private Texture2D barBack;

    void Awake()
    {
        barFill = MakeTexture(new Color(0.85f, 0.25f, 0.3f));
        barBack = MakeTexture(new Color(0f, 0f, 0f, 0.45f));
    }

    void OnGUI()
    {
        if (!visible) { return; }

        label ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };

        GUILayout.BeginArea(new Rect(12f, 12f, 260f, 150f));
        if (playerHealth != null)
        {
            Rect bar = GUILayoutUtility.GetRect(236f, 16f);
            GUI.DrawTexture(bar, barBack);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * playerHealth.Fraction, bar.height), barFill);
            GUILayout.Label($"health {Mathf.CeilToInt(playerHealth.Current)} / {Mathf.CeilToInt(playerHealth.Max)}", label);
        }

        if (GameManager.Instance != null)
        {
            GUILayout.Label($"score {GameManager.Instance.Score}    deaths {GameManager.Instance.Deaths}", label);
        }

        if (player != null)
        {
            string dash = player.DashCharge >= 1f ? "ready" : $"{player.DashCharge * 100f:0}%";
            GUILayout.Label($"dash {dash}", label);
        }
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(12f, Screen.height - 60f, 520f, 50f));
        GUILayout.Label("wasd move    shift sprint    space dash    left click fire    scroll zoom", label);
        GUILayout.EndArea();
    }

    private static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}

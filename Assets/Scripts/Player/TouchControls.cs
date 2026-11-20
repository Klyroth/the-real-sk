using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TouchControls — The real SK (Android touch controls, added 2026-11-20)
// Play-test #2 (Build #3 APK) found the game unplayable on the phone:
// no joystick, no fire button, no scope button — the player could not
// move, and every screen tap both fired the gun and dragged the camera.
// This script builds its own touch UI at runtime and writes into the
// static input hooks the gameplay scripts already expose:
//   PlayerMovement.touchMoveInput, CameraController.touchLookDelta,
//   ShootingController.touchFireHeld / touchFirePressed,
//   ScopeSystem.touchScopeToggle, GameManager.touchWeaponIndex.
// UI is built ONLY on touch devices (Input.touchSupported), so PC
// behaviour is unchanged. Attach to a GameObject in Map1/Map2 only
// (NOT MainMenu).
public class TouchControls : MonoBehaviour
{
    private const float JoystickRadius = 130f;

    private RectTransform joystickKnob;
    private RectTransform canvasRect;
    private Vector2 joystickBasePos;
    private bool joystickActive;

    void Start()
    {
        if (!Input.touchSupported)
        {
            // No touchscreen (PC/Editor): no touch UI, no behaviour change.
            enabled = false;
            return;
        }
        BuildUI();
    }

    // ---------- UI construction ----------
    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("TouchControlsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // above PlayerUI (0), below ScopeCanvas (10)
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasRect = canvasGo.GetComponent<RectTransform>();
        Transform t = canvasGo.transform;

        // Look area: right half of the screen, behind the buttons.
        // Dragging here rotates the camera (touch look).
        GameObject lookGo = new GameObject("LookArea", typeof(RectTransform));
        lookGo.transform.SetParent(t, false);
        RectTransform lookRt = lookGo.GetComponent<RectTransform>();
        lookRt.anchorMin = new Vector2(0.5f, 0f);
        lookRt.anchorMax = new Vector2(1f, 1f);
        lookRt.offsetMin = Vector2.zero;
        lookRt.offsetMax = Vector2.zero;
        Image lookImg = lookGo.AddComponent<Image>();
        lookImg.color = new Color(0f, 0f, 0f, 0f); // invisible, still raycastable
        AddTrigger(lookGo, EventTriggerType.Drag, (data) =>
        {
            CameraController.touchLookDelta += data.delta;
        });

        // Joystick base (bottom-left) + knob
        GameObject baseGo = new GameObject("JoystickBase", typeof(RectTransform));
        baseGo.transform.SetParent(t, false);
        RectTransform baseRt = baseGo.GetComponent<RectTransform>();
        baseRt.anchorMin = new Vector2(0f, 0f);
        baseRt.anchorMax = new Vector2(0f, 0f);
        baseRt.anchoredPosition = new Vector2(260f, 260f);
        baseRt.sizeDelta = new Vector2(JoystickRadius * 2f, JoystickRadius * 2f);
        Image baseImg = baseGo.AddComponent<Image>();
        baseImg.sprite = MakeCircleSprite(new Color(1f, 1f, 1f, 0.15f));
        baseImg.color = Color.white;

        GameObject knobGo = new GameObject("JoystickKnob", typeof(RectTransform));
        knobGo.transform.SetParent(baseGo.transform, false);
        RectTransform knobRt = knobGo.GetComponent<RectTransform>();
        knobRt.anchorMin = new Vector2(0.5f, 0.5f);
        knobRt.anchorMax = new Vector2(0.5f, 0.5f);
        knobRt.anchoredPosition = Vector2.zero;
        knobRt.sizeDelta = new Vector2(110f, 110f);
        Image knobImg = knobGo.AddComponent<Image>();
        knobImg.sprite = MakeCircleSprite(new Color(1f, 1f, 1f, 0.45f));
        knobImg.color = Color.white;
        joystickKnob = knobRt;
        joystickBasePos = baseRt.anchoredPosition;

        AddTrigger(baseGo, EventTriggerType.PointerDown, (data) => { joystickActive = true; OnJoystickDrag(data); });
        AddTrigger(baseGo, EventTriggerType.Drag, (data) => { if (joystickActive) OnJoystickDrag(data); });
        AddTrigger(baseGo, EventTriggerType.PointerUp, (data) => { joystickActive = false; ResetJoystick(); });
        // Also accept drags that start on the knob itself
        AddTrigger(knobGo, EventTriggerType.PointerDown, (data) => { joystickActive = true; OnJoystickDrag(data); });
        AddTrigger(knobGo, EventTriggerType.Drag, (data) => { if (joystickActive) OnJoystickDrag(data); });
        AddTrigger(knobGo, EventTriggerType.PointerUp, (data) => { joystickActive = false; ResetJoystick(); });

        // FIRE button (bottom-right, hold to fire)
        Button fireBtn = MakeRoundButton(t, "FireButton", "FIRE", new Vector2(1f, 0f), new Vector2(-190f, 230f), 180f,
            new Color(0.8f, 0.15f, 0.15f, 0.55f), 30);
        AddTrigger(fireBtn.gameObject, EventTriggerType.PointerDown, (data) =>
        {
            ShootingController.touchFireHeld = true;
            ShootingController.touchFirePressed = true;
        });
        AddTrigger(fireBtn.gameObject, EventTriggerType.PointerUp, (data) =>
        {
            ShootingController.touchFireHeld = false;
        });

        // SCOPE button (above FIRE)
        Button scopeBtn = MakeRoundButton(t, "ScopeButton", "SCOPE", new Vector2(1f, 0f), new Vector2(-190f, 450f), 120f,
            new Color(0.15f, 0.35f, 0.8f, 0.55f), 22);
        AddTrigger(scopeBtn.gameObject, EventTriggerType.PointerClick, (data) =>
        {
            ScopeSystem.touchScopeToggle = true;
        });

        // WEAPON button (left of FIRE)
        Button weaponBtn = MakeRoundButton(t, "WeaponButton", "SWAP", new Vector2(1f, 0f), new Vector2(-410f, 230f), 120f,
            new Color(0.15f, 0.15f, 0.2f, 0.55f), 22);
        AddTrigger(weaponBtn.gameObject, EventTriggerType.PointerClick, (data) =>
        {
            int count = GameManager.Instance != null ? GameManager.Instance.WeaponCount : 0;
            if (count <= 0) return;
            int current = GameManager.Instance != null ? GameManager.Instance.CurrentWeaponIndex : 0;
            GameManager.touchWeaponIndex = (current + 1) % count;
        });
    }

    private void OnJoystickDrag(PointerEventData data)
    {
        // Pointer position in the canvas's local (reference-resolution) space.
        RectTransform canvasRt = canvasRect;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, data.position, null, out local))
            return;
        // Canvas local origin is the centre; convert to bottom-left origin.
        Vector2 p = local + new Vector2(canvasRt.rect.width * 0.5f, canvasRt.rect.height * 0.5f);
        Vector2 offset = p - joystickBasePos;
        if (offset.magnitude > JoystickRadius)
            offset = offset.normalized * JoystickRadius;
        joystickKnob.anchoredPosition = offset;
        // UI +Y is up, so pushing the stick up gives +Y = forward.
        PlayerMovement.touchMoveInput = offset / JoystickRadius;
    }

    private void ResetJoystick()
    {
        joystickKnob.anchoredPosition = Vector2.zero;
        PlayerMovement.touchMoveInput = Vector2.zero;
    }

    // ---------- helpers ----------
    private static Button MakeRoundButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, float size, Color bg, int fontSize)
    {
        GameObject go = DefaultControls.CreateButton(new DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);
        Image img = go.GetComponent<Image>();
        img.sprite = MakeCircleSprite(bg);
        img.color = Color.white;
        Text txt = go.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = label;
            txt.fontSize = fontSize;
            txt.color = Color.white;
        }
        return go.GetComponent<Button>();
    }

    private static void AddTrigger(GameObject go, EventTriggerType type, System.Action<PointerEventData> action)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener((data) => action((PointerEventData)data));
        trigger.triggers.Add(entry);
    }

    // Soft-edged disc sprite, drawn at runtime so no asset file is needed.
    private static Sprite MakeCircleSprite(Color color)
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                float a = Mathf.Clamp01((1f - d) * 8f); // soft edge
                Color c = color;
                c.a *= a;
                pixels[y * size + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}

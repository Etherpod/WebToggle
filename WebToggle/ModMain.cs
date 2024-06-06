using HarmonyLib;
using modweaver.core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WebToggle;

[ModMainClass]
public class ModMain : Mod
{
    public bool WebInputToggled
    {
        get
        {
            return Keyboard.current.leftShiftKey.isPressed;
        }
        private set { WebInputToggled = value; }
    }

    public static ModMain Instance;

    public override void Init()
    {
        Instance = this;
        Harmony harmony = new Harmony(Metadata.id);
        harmony.PatchAll();
    }

    public override void Ready() { }

    public override void OnGUI(ModsMenuPopup ui) { }
}

[HarmonyPatch]
public class PatchClass
{
    public static Vector2 GetWebDirection(SpiderController __instance)
    {
        Vector2 movementVector = __instance.IsOwner ? __instance._movementInputLocal : __instance._movementInput.Value;
        if (__instance._usingKeyboard)
        {
            if (ModMain.Instance.WebInputToggled)
            {
                if (GameSettings.MouseAimWebEnabled())
                {
                    return movementVector;
                }
                else
                {
                    return __instance.IsOwner ? __instance._aimInputLocal : __instance._aimInput.Value;
                }
            }
            else
            {
                if (!GameSettings.MouseAimWebEnabled())
                {
                    return movementVector;
                }
                else
                {
                    return __instance.IsOwner ? __instance._aimInputLocal : __instance._aimInput.Value;
                }
            }
        }
        else
        {
            return movementVector;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SpiderController), nameof(SpiderController.DoubleJump))]
    public static void WebTogglePatch(SpiderController __instance, ref Vector2 inputDirection)
    {
        inputDirection = GetWebDirection(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SpiderController), nameof(SpiderController.GetAimDirection))]
    public static void GetAimDirectionPatch(SpiderController __instance, ref Vector2 __result)
    {
        __result = GetWebDirection(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SpiderController), nameof(SpiderController.MoveWebTargetIndicator))]
    public static bool WebTargetPatch(SpiderController __instance)
    {
        if (!__instance.IsOwner)
        {
            __instance.webTargetIndicator.gameObject.SetActive(false);
            return false;
        }
        Vector2 vector = GetWebDirection(__instance);
        if (!__instance.bodyStabilizer.grounded && vector.magnitude != 0f)
        {
            __instance.webTargetIndicator.gameObject.SetActive(true);
            Vector3 vector2 = __instance.bodyRigidbody2D.transform.position + (Vector3)vector.normalized * 10f;
            __instance.webTargetIndicator.transform.position =
                Vector3.Lerp(__instance.webTargetIndicator.transform.position, vector2, 50f * Time.deltaTime);
            float num = Mathf.Atan2(vector.y, vector.x) * 57.29578f;
            __instance.webTargetIndicator.transform.rotation = Quaternion.AngleAxis(num, Vector3.forward);
            return false;
        }
        __instance.webTargetIndicator.gameObject.SetActive(false);
        return false;
    }
}
using System;
using Ravenfield;
using UnityEngine;
using BepInEx;
using System.Collections.Generic;
using System.Collections;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityStandardAssets.Characters.FirstPerson;
using BepInEx.Configuration;
using Ravenfield.Trigger;
using LevelSystem;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine.Audio;
namespace personplus
{
    [BepInPlugin("com.personperhaps.ravenimpactfx", "ravenimpactfx", "1.2")]
    public class ravenimpactfx : BaseUnityPlugin
    {
        public static ravenimpactfx instance = null;
        public static AudioSource impactFXSource = null;
        void Start()
        {
            Debug.Log("ravenimpactfx: Loading!");
            Harmony harmony = new Harmony("ravenimpactfx");
            harmony.PatchAll();
            StartCoroutine(LoadAssetBundle(Assembly.GetExecutingAssembly().GetManifestResourceStream("personplus.assets.impact")));
            StartCoroutine(LoadAudioAssetBundle(Assembly.GetExecutingAssembly().GetManifestResourceStream("personplus.assets.impactaudio")));
            instance = this;
            impactFXSource = new GameObject().AddComponent<AudioSource>();
            noKickTuck = Config.Bind("General.Toggles",
                                                "No Kick Tuck",
                                                false,
                                                "Kicks no longer apply gun tuck animation (allowing you to shoot/reload while kicking)");
            gungho = Config.Bind("General.Toggles",
                                                "Gungho",
                                                false,
                                                "Allows shooting while kicking or sprinting");
            reloadDuringKick = Config.Bind("General.Toggles",
                                                "Kicking while Reloading",
                                                false,
                                                "Allows reloading while kicking");

            autoJump = Config.Bind("General.Toggles",
                                                "Autojump",
                                                false,
                                                "Allows autojump (bunnyhopping :) )");

            tacticalMovement = Config.Bind("General.Toggles",
                                               "Tactical Movement",
                                               true,
                                               "Lots of lerps and such. makes movement and camera smooth");

            groundMovementSmoothing = Config.Bind("Tactical.Toggles",
                                               "groundMovementSmoothing",
                                               16f,
                                               "ground smooth movement");
            slideMovementSmoothing = Config.Bind("Tactical.Toggles",
                                               "slideMovementSmoothing",
                                               16f,
                                               "ground smooth movement");
            airMovementSmoothing = Config.Bind("Tactical.Toggles",
                                               "Air Movement Smoothing",
                                               8f,
                                               "air smooth movement");
            airDiveMovementSmoothing = Config.Bind("Tactical.Toggles",
                                               "Air Dive Smoothing",
                                               0f,
                                               "air prone smooth movement");

            aimSmoothing = Config.Bind("Tactical.Toggles",
                                               "Aim Smoothing",
                                               4f,
                                               "aim movement");


            extraWeapons = Config.Bind("Funny.Toggles",
                                               "Extra Weapons",
                                               false,
                                               "Gives a few extra weapon slots per loadout");

            impactFXEnabled = Config.Bind("General.Toggles",
                                               "Impact FX",
                                               true,
                                               "Toggles custom impact VFX");
            configImpactFXCooldown = Config.Bind("General.Toggles",
                                                "ImpactFX Cooldown",
                                                0.005f,
                                                "Time between impact fx");


            damageScreenShakeReduction = Config.Bind("General.Toggles",
                                                "Damage Screenshake Reduction",
                                                2f,
                                                "Divides screenshake amount by x");
            jumpScreenShakeMultiplier = Config.Bind("General.Toggles",
                                                "Jump Screenshake Multiplier",
                                                1f,
                                                "Multiplies jumping screenshake amount by x");

            weaponSnapDurationMultiplier = Config.Bind("Recoil.Toggles",
                                                "Weapon Snap Duration Multiplier",
                                                1f,
                                                "Multiplies universal weapon snap duration by x");
            weaponSnapMagnitudeMultiplier = Config.Bind("Recoil.Toggles",
                                                "Weapon Snap Magnitude Multiplier",
                                                1f,
                                                "Multiplies universal weapon snap magnitude by x");
            weaponSnapFrequencyMultiplier = Config.Bind("Recoil.Toggles",
                                                "Weapon Snap Frequency Multiplier",
                                                1f,
                                                "Multiplies universal weapon snap frequency by x");

            weaponPositionSpring = Config.Bind("Weapon.Toggles",
                                                "Weapon Position Spring",
                                                100f,
                                                "fpcamera");
            weaponPositionDrag = Config.Bind("Weapon.Toggles",
                                                "Weapon Position Drag",
                                                6f,
                                                "fpcamera");

            weaponRotationSpring = Config.Bind("Weapon.Toggles",
                                                "Weapon Rotation Spring",
                                                100f,
                                                "fpcamera");
            weaponRotationDrag = Config.Bind("Weapon.Toggles",
                                                "Weapon Rotation Drag",
                                                7f,
                                                "fpcamera");

            anyWeapon = Config.Bind("Weapon.Toggles",
                                                "Any Loadout Slot",
                                                false,
                                                "Any Loudout slot can be used");

            NonFixedTime = Config.Bind("Tactical.General",
                                                "NonFixedTime",
                                                false,
                                                "NonFixedTime");

        }

        public ConfigEntry<float> weaponSnapDurationMultiplier;
        public ConfigEntry<float> weaponSnapMagnitudeMultiplier;
        public ConfigEntry<float> weaponSnapFrequencyMultiplier;

        public ConfigEntry<float> weaponPositionSpring;
        public ConfigEntry<float> weaponPositionDrag;

        public ConfigEntry<float> weaponRotationSpring;
        public ConfigEntry<float> weaponRotationDrag;

        public ConfigEntry<bool> NonFixedTime;

        [HarmonyPatch(typeof(PlayerFpParent), nameof(PlayerFpParent.ApplyWeaponSnap))]
        public class ApplyRecoilPatch
        {
            public static void Prefix(PlayerFpParent __instance, ref float magnitude, ref float duration, ref float frequency)
            {
                magnitude *= instance.weaponSnapMagnitudeMultiplier.Value;
                frequency *= instance.weaponSnapFrequencyMultiplier.Value;
                duration *= instance.weaponSnapDurationMultiplier.Value;
            }
        }

        public static AudioMixerGroup mixer;

        static GameObject impactFX;
        static GameObject largerImpactFX;
        static List<AudioClip> impactSounds = new List<AudioClip>();

        static List<GameObject> impactFXActivePool = new List<GameObject>();
        static List<GameObject> impactFXInactivePool = new List<GameObject>();

        public ConfigEntry<bool> reloadDuringKick;

        public ConfigEntry<bool> anyWeapon;

        public ConfigEntry<bool> gungho;

        public ConfigEntry<bool> autoJump;

        public ConfigEntry<bool> noKickTuck;

        public ConfigEntry<bool> tacticalMovement;

        public ConfigEntry<bool> extraWeapons;

        public ConfigEntry<bool> impactFXEnabled;

        public ConfigEntry<bool> smoothEverything;

        public ConfigEntry<float> groundMovementSmoothing;

        public ConfigEntry<float> slideMovementSmoothing;

        public ConfigEntry<float> aimSmoothing;



        public ConfigEntry<float> airMovementSmoothing;

        public ConfigEntry<float> airDiveMovementSmoothing;

        public ConfigEntry<float> configImpactFXCooldown;

        public ConfigEntry<float> damageScreenShakeReduction;

        public ConfigEntry<float> jumpScreenShakeMultiplier;

        void FixedUpdate()
        {

            List<GameObject> toBeRemoved = new List<GameObject>();
            foreach (GameObject fx in impactFXActivePool)
            {
                if (fx == null)
                {
                    toBeRemoved.Add(fx);
                    return;
                }
                if (fx.activeInHierarchy == false)
                {
                    impactFXInactivePool.Add(fx);
                    toBeRemoved.Add(fx);
                }
            }
            foreach (GameObject fx in toBeRemoved)
            {
                impactFXActivePool.Remove(fx);
            }
        }

        IEnumerator LoadAssetBundle(Stream path)
        {
            if (path == null)
            {

                Logger.LogError("uh oh no path");
                yield break;
            }
            var bundleLoadRequest = AssetBundle.LoadFromStreamAsync(path);
            yield return bundleLoadRequest;

            var assetBundle = bundleLoadRequest.assetBundle;
            if (assetBundle == null)
            {
                Logger.LogError("uh oh no bundle");
                yield break;
            }
            var assetLoadRequest = assetBundle.LoadAllAssetsAsync<GameObject>();
            yield return assetLoadRequest;
            if (assetLoadRequest.allAssets == null)
            {
                Logger.LogError("uh oh loading assets failed");
                yield break;
            }
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>(assetLoadRequest.allAssets);
            foreach (UnityEngine.Object asset in objects)
            {
                Logger.LogInfo("Found " + asset.name);
                if (asset.name == "impact")
                {
                    Logger.LogInfo("Added " + asset.name + " to impact list");
                    impactFX = asset as GameObject;
                }
                if (asset.name == "largerimpact")
                {
                    Logger.LogInfo("Added " + asset.name + " as large impact");
                    largerImpactFX = asset as GameObject;
                }
            }
            assetBundle.Unload(false);
        }
        IEnumerator LoadAudioAssetBundle(Stream path)
        {
            if (path == null)
            {

                Logger.LogError("uh oh no path");
                yield break;
            }
            var bundleLoadRequest = AssetBundle.LoadFromStreamAsync(path);
            yield return bundleLoadRequest;

            var assetBundle = bundleLoadRequest.assetBundle;
            if (assetBundle == null)
            {
                Logger.LogError("uh oh no bundle");
                yield break;
            }
            var assetLoadRequest = assetBundle.LoadAllAssetsAsync<AudioClip>();
            yield return assetLoadRequest;
            if (assetLoadRequest.allAssets == null)
            {
                Logger.LogError("uh oh loading assets failed");
                yield break;
            }
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>(assetLoadRequest.allAssets);
            foreach (UnityEngine.Object asset in objects)
            {
                Logger.LogInfo("Found " + asset.name);
                if (asset.name.StartsWith("concrete"))
                {
                    Logger.LogInfo("Added " + asset.name + " to impact list");
                    impactSounds.Add(asset as AudioClip);
                }
                if (asset.name.StartsWith("plastic"))
                {
                    Logger.LogInfo("Added " + asset.name + " to impact list");
                    impactSounds.Add(asset as AudioClip);
                }
                if (asset.name.StartsWith("cardboard"))
                {
                    Logger.LogInfo("Added " + asset.name + " to impact list");
                    impactSounds.Add(asset as AudioClip);
                }
            }
            assetBundle.Unload(false);
        }

        [HarmonyPatch(typeof(FirstPersonControllerInput), "Update")]
        public class AutoJumpPatch
        {
            public static void Postfix(FirstPersonControllerInput __instance)
            {
                if (instance.autoJump.Value)
                {
                    __instance.gameObject.GetComponent<FirstPersonController>().SetInput(-SteelInput.GetAxis(SteelInput.KeyBinds.Horizontal), SteelInput.GetAxis(SteelInput.KeyBinds.Vertical), SteelInput.GetButton(SteelInput.KeyBinds.Jump), -SteelInput.GetAxis(SteelInput.KeyBinds.AimX), SteelInput.GetAxis(SteelInput.KeyBinds.AimY));
                }
            }
        }

        public static float RealDelta()
        {
            return instance.NonFixedTime.Value ? Time.deltaTime : Time.fixedDeltaTime;
        }

        [HarmonyPatch(typeof(FpsActorController), "Update")]
        public class SmoothCameraPatch
        {
            public static void Prefix(out float __state, FpsActorController __instance)
            {
                __state = __instance.fpParent.lean;
            }
            public static void Postfix(float __state, FpsActorController __instance)
            {
                if (instance.tacticalMovement.Value)
                {
                    __instance.fpParent.lean = Mathf.Lerp(__state, __instance.Lean() * 1.4f, Time.fixedDeltaTime * 5);
                }
            }
        }

        [HarmonyPatch(typeof(Animator), nameof(Animator.SetBool), new[] { typeof(int), typeof(bool) })]
        public class TuckPatch
        {
            public static bool Prefix(int id, bool value, Animator __instance)
            {
                if (instance.noKickTuck.Value)
                {
                    if (id == Animator.StringToHash("tuck"))
                    {
                        return false;
                    }
                    return true;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Animator), nameof(Animator.SetTrigger), new[] { typeof(string) })]
        public class TuckPatch2
        {
            public static bool Prefix(string name, Animator __instance)
            {
                if (name == "kick" && instance.noKickTuck.Value)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FpsActorController), nameof(FpsActorController.WantsToFire))]
        public class WantsToFirePatch
        {
            public static void Postfix(FpsActorController __instance, ref bool __result)
            {
                if (instance.gungho.Value)
                {
                    __result = __instance.inputEnabled && !GameManager.gameOver && (SteelInput.GetButton(SteelInput.KeyBinds.Fire) && !__instance.IsCursorFree());
                }
            }
        }
        [HarmonyPatch(typeof(FpsActorController), "IsReloading")]
        public class ReloadCheckPatch
        {
            public static void Postfix(FpsActorController __instance, ref bool __result)
            {
                if (instance.gungho.Value)
                {
                    __result = false;
                }
            }
        }
        [HarmonyPatch(typeof(FpsActorController), nameof(FpsActorController.Reload))]
        public class ReloadPatch
        {
            public static void Postfix(FpsActorController __instance, ref bool __result)
            {
                if (instance.reloadDuringKick.Value)
                {
                    __result = __instance.inputEnabled && SteelInput.GetButton(SteelInput.KeyBinds.Reload) && !__instance.IsCursorFree() && !GameManager.gameOver;
                }
            }
        }
        [HarmonyPatch(typeof(FpsActorController), nameof(FpsActorController.IsSprinting))]
        public class IsSprintingPatch
        {
            public static void Postfix(FpsActorController __instance, ref bool __result)
            {
                if (instance.noKickTuck.Value)
                {
                    __result = __instance.actor.stance == Actor.Stance.Stand && !__instance.Aiming() && __instance.HoldingSprint() && !__instance.actor.IsSeated();
                }
            }
        }
        [HarmonyPatch(typeof(FpsActorController), nameof(FpsActorController.ReceivedDamage))]
        public class ReceiveDamagePatch
        {
            public static bool Prefix(FpsActorController __instance, bool friendlyFire, float damage, float balanceDamage, Vector3 point, Vector3 direction, Vector3 force)
            {
                if (balanceDamage > 5f)
                {
                    __instance.fpParent.ApplyScreenshake(balanceDamage / (6f * instance.damageScreenShakeReduction.Value), Mathf.CeilToInt(balanceDamage / (20f * instance.damageScreenShakeReduction.Value)));
                }
                if (damage > 5f)
                {
                    __instance.fpParent.KickCamera(new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-5f, 5f)) / instance.damageScreenShakeReduction.Value);
                }
                Vector3 vector = __instance.GetActiveCamera().transform.worldToLocalMatrix.MultiplyVector(-direction);
                float angle = Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f;
                DamageUI.instance.ShowDamageIndicator(angle, damage < 2f && balanceDamage > damage);
                return false;
            }
        }
        /*
        [HarmonyPatch(typeof(FpsActorController), "UpdateGameplay")]
        public class HeightChangePatch
        {
            static float cameraHeight = 1.5300001f;
            public static void Postfix(FpsActorController __instance)
            {
                if (!__instance.actor.IsSeated() && !__instance.actor.IsOnLadder())
                {
                    cameraHeight = (float)typeof(FpsActorController).GetField("cameraHeight", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                    cameraHeight = Mathf.Lerp(cameraHeight, this.targetCameraHeight, 4f * Time.deltaTime);
                    __instance.fpCameraRoot.localPosition = new Vector3(0f, this.cameraHeight, 0f);
                }
            }
        }
        */
        [HarmonyPatch(typeof(FpsActorController), "UpdateGameplay")]
        public class StancePatch
        {
            static float beforeCameraHeight = 1.5f;
            static float beforeTargetCameraHeight = 1.5f;
            public static void Prefix(FpsActorController __instance)
            {
                if (instance.tacticalMovement.Value && !__instance.actor.IsSeated() && !__instance.actor.IsOnLadder())
                {
                    beforeCameraHeight = (float)typeof(FpsActorController).GetField("cameraHeight", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                    beforeTargetCameraHeight = (float)typeof(FpsActorController).GetField("targetCameraHeight", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                }
            }

            public static void Postfix(FpsActorController __instance)
            {
                if (instance.tacticalMovement.Value && !__instance.actor.IsSeated() && !__instance.actor.IsOnLadder())
                {
                    float lerpHeight = EasingFunction.EaseOutSine(beforeCameraHeight, beforeTargetCameraHeight, 4f * RealDelta());
                    typeof(FpsActorController).GetField("cameraHeight", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, lerpHeight);
                    Transform cameraRoot = (Transform)typeof(FpsActorController).GetField("fpCameraRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

                    cameraRoot.localPosition = new Vector3(0f, lerpHeight, 0f);
                }
            }

        }
        [HarmonyPatch(typeof(PlayerFpParent), "Awake")]
        public class FpSpringsPatch
        {
            public static void Postfix(PlayerFpParent __instance)
            {
                Spring positionSpring = new Spring(instance.weaponPositionSpring.Value, instance.weaponPositionDrag.Value, -Vector3.one * 0.2f, Vector3.one * 0.2f, 8);
                typeof(PlayerFpParent).GetField("positionSpring", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, positionSpring);
                Spring rotationSpring = new Spring(instance.weaponRotationSpring.Value, instance.weaponRotationDrag.Value, -Vector3.one * 15f, Vector3.one * 15f, 8);
                typeof(PlayerFpParent).GetField("rotationSpring", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, rotationSpring);
                foreach (Camera camera in Camera.allCameras)
                {
                    camera.depthTextureMode = camera.depthTextureMode | DepthTextureMode.DepthNormals;
                }
            }
        }
        [HarmonyPatch(typeof(PlayerFpParent), "FixedUpdate")]
        public class FovPatch
        {
            static float beforeFovRatio = 1.5f;
            public static void Prefix(PlayerFpParent __instance)
            {
                beforeFovRatio = (float)typeof(PlayerFpParent).GetField("fovRatio", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

            }
            static public float swayAmount = 0;
            public static void Postfix(PlayerFpParent __instance)
            {
                float fovSpeed = instance.aimSmoothing.Value;
                bool aiming = (bool)typeof(PlayerFpParent).GetField("aiming", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

                float lerpHeight = Mathf.Lerp(beforeFovRatio, aiming ? 1f : 0f, RealDelta() * fovSpeed);
                typeof(PlayerFpParent).GetField("fovRatio", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, lerpHeight);

                FieldInfo cameraRootField = typeof(PlayerFpParent).GetField("fpCameraRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                if (cameraRootField != null)
                {
                    Camera cameraRoot = (Camera)cameraRootField.GetValue(__instance);
                    if (cameraRoot != null)
                    {
                        float zoomFov = (float)typeof(PlayerFpParent).GetField("zoomFov", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                        float normalFov = (float)typeof(PlayerFpParent).GetField("normalFov", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

                        cameraRoot.fieldOfView = EasingFunction.EaseInOutQuint(normalFov, zoomFov, lerpHeight) + __instance.GetKickLocalEuler().magnitude * 5 + GetSnap(__instance) * 20;
                    }
                }

            }
        }
        public static float GetSnap(PlayerFpParent __instance)
        {
            TimedAction weaponSnapAction = (TimedAction)typeof(PlayerFpParent).GetField("weaponSnapAction", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            float weaponSnapFrequency = (float)typeof(PlayerFpParent).GetField("weaponSnapFrequency", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            float weaponSnapMagnitude = (float)typeof(PlayerFpParent).GetField("weaponSnapMagnitude", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            return Mathf.Sin(weaponSnapAction.Ratio() * (0.1f + 1f - weaponSnapAction.Ratio()) * weaponSnapFrequency / 3) * weaponSnapMagnitude * 2;
        }
        [HarmonyPatch(typeof(Weapon), "Equip")]
        public class EquipFix
        {
            public static void Postfix(Weapon __instance)
            {
                if (__instance.UserIsPlayer() && __instance.arms != null)
                {
                    CoolTiltingPatch.bones = __instance.arms.bones.ToDictionary(x => x, x => new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero));
                }
            }
        }
        [HarmonyPatch(typeof(PlayerFpParent), "LateUpdate")]
        public class CoolTiltingPatch
        {
            static public float swayAmount = 0;

            static public SpringPlus ravenCoil = new SpringPlus(70, 7, -Vector3.one * 20f, Vector3.one * 20f, 16, 1f, 0.5f);

            static public Spring shakeCoil = new Spring(10, 5, -Vector3.one * 7f, Vector3.one * 7f, 8);

            static public Vector3 averageDelta = Vector3.zero;

            static public Dictionary<Transform, Tuple<Vector3, Vector3>> bones = new Dictionary<Transform, Tuple<Vector3, Vector3>>();

            static public float lastSnap = 0;
            public static void Postfix(PlayerFpParent __instance)
            {
                Vector3 targetAverage = Vector3.zero;
                Dictionary<Transform, Tuple<Vector3, Vector3>> replaceBones = new Dictionary<Transform, Tuple<Vector3, Vector3>>();
                foreach (Transform transform in bones.Keys)
                {
                    if(transform.localPosition == null)
                    {
                        break;
                    }
                    Vector3 inversePoint = transform.localPosition;
                    replaceBones.Add(transform, new Tuple<Vector3, Vector3>(bones[transform].Item1, bones[transform].Item2));
                    targetAverage += (bones[transform].Item1-  transform.localRotation.eulerAngles) / 5;
                    targetAverage += (transform.localPosition - bones[transform].Item2) * 1000;
                }
                if(FpsActorController.instance.actor != null)
                {
                    if (FpsActorController.instance.actor.activeWeapon != null)
                    {
                        if (FpsActorController.instance.actor.activeWeapon.arms != null)
                        {
                            Dictionary < Transform, Tuple < Vector3, Vector3 >> dict = FpsActorController.instance.actor.activeWeapon.arms.bones.ToDictionary(x => x, x => new Tuple<Vector3, Vector3>(x.localRotation.eulerAngles, x.localPosition));
                            CoolTiltingPatch.bones = dict;
                        }
                    }
                }

                

                if (averageDelta.sqrMagnitude > 0.1f)
                {
                    shakeCoil.velocity = averageDelta;
                }
                averageDelta = Vector3.Lerp(averageDelta, targetAverage, Time.deltaTime * (Mathf.Log10(Mathf.Abs((averageDelta - targetAverage).magnitude + 0.05f)) + 2.3f) * 2);
                Debug.Log(averageDelta);

                //averageDelta = new Vector3(averageDelta.x, averageDelta.y, 0);
                ravenCoil.Update();
                shakeCoil.Update();

                swayAmount = Mathf.Lerp(swayAmount, SteelInput.GetAxis(SteelInput.KeyBinds.Horizontal), RealDelta() * 2);
                __instance.weaponParent.transform.localEulerAngles = __instance.weaponParent.transform.localEulerAngles + Vector3.back * -swayAmount * 6;
                lastSnap = Mathf.Lerp(lastSnap, GetSnap(__instance), RealDelta() * 10);
                __instance.fpCameraParent.localEulerAngles = __instance.fpCameraParent.localEulerAngles + Vector3.back * -swayAmount * 2 + __instance.GetSpringLocalEuler(true) / 2 + new Vector3(lastSnap * -6f, 0f, 0f) + ravenCoil.position + shakeCoil.position * 2.4f;
            }
            public static Dictionary<TKey, TValue> CloneDictionaryCloningValues<TKey, TValue>
   (Dictionary<TKey, TValue> original) where TValue : ICloneable
            {
                Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(original.Count,
                                                                        original.Comparer);
                foreach (KeyValuePair<TKey, TValue> entry in original)
                {
                    ret.Add(entry.Key, (TValue)entry.Value.Clone());
                }
                return ret;
            }
        }
        [HarmonyPatch(typeof(Weapon), "ApplyRecoil")]
        public class BetterRecoilPatch
        {
            public static void Postfix(Weapon __instance)
            {
                if (__instance.UserIsPlayer() || !__instance.IsMountedWeapon())
                {
                    float t = __instance.configuration.auto ? 1.8f : 0.6f;

                    Vector3 vector = __instance.configuration.kickback * Vector3.back + UnityEngine.Random.insideUnitSphere * __instance.configuration.randomKick * 1.2f * __instance.configuration.snapDuration * t;

                    if (__instance.user.stance == Actor.Stance.Prone)
                    {
                        vector *= __instance.configuration.kickbackProneMultiplier;
                    }
                    vector = new Vector3(vector.z, vector.x, -vector.x);
                    CoolTiltingPatch.ravenCoil.AddVelocity(vector * 50);
                    CoolTiltingPatch.ravenCoil.distanceCoefficient = __instance.configuration.cooldown / 3;
                }
            }
        }
        [HarmonyPatch(typeof(FpsActorController), "OnJump")]
        public class OnJumpPatch
        {
            public static bool Prefix(FpsActorController __instance)
            {
                if (__instance.actor.IsSeated())
                {
                    return true;
                }
                Vector3 vector = new Vector3(0f, 0.2f * instance.jumpScreenShakeMultiplier.Value, 0f);
                if (__instance.IsSprinting())
                {
                    Vector3 vector2 = __instance.fpCamera.transform.worldToLocalMatrix.MultiplyVector(__instance.Velocity());
                    vector -= Vector3.ClampMagnitude(vector2, 1f);
                }
                __instance.fpParent.ApplyRecoil(vector, true);
                return false;
            }
        }
        [HarmonyPatch(typeof(FpsActorController), "OnLand")]
        public class OnLandPatch
        {
            public static bool Prefix(FpsActorController __instance)
            {
                if (__instance.actor.IsSeated())
                {
                    return false;
                }
                float num = Mathf.Clamp((-__instance.actor.Velocity().y - 2f) * 0.3f, 0f, 2f) * instance.jumpScreenShakeMultiplier.Value;
                if (__instance.IsSprinting())
                {
                    num *= 2f;
                }
                __instance.fpParent.ApplyRecoil(new Vector3(0f, -num * 0.3f, 0f), true);
                __instance.fpParent.KickCamera(new Vector3(num, 0f, 0f));
                return false;
            }
        }


        [HarmonyPatch(typeof(FirstPersonController), "GetInput")]
        public class SmoothMovementPatch
        {
            static float beforeSpeed = 1;
            public static void Postfix(FirstPersonController __instance, out float speed)
            {
                if (instance.tacticalMovement.Value)
                {
                    float m_SwamSpeed = (float)typeof(FirstPersonController).GetField("m_SwamSpeed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                    bool m_IsWalking = (bool)typeof(FirstPersonController).GetField("m_IsWalking", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                    float xspeed = __instance.m_WalkSpeed;
                    if (__instance.inWater)
                    {
                        xspeed = m_SwamSpeed;
                    }
                    else
                    {
                        xspeed = (m_IsWalking ? __instance.m_WalkSpeed : __instance.m_RunSpeed);
                    }
                    speed = Mathf.Lerp(beforeSpeed, xspeed, RealDelta() * 5);
                    beforeSpeed = speed;
                }
                else
                {
                    float m_SwamSpeed = (float)typeof(FirstPersonController).GetField("m_SwamSpeed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                    bool m_IsWalking = (bool)typeof(FirstPersonController).GetField("m_IsWalking", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                    if (__instance.inWater)
                    {
                        speed = m_SwamSpeed;
                    }
                    else
                    {
                        speed = (m_IsWalking ? __instance.m_WalkSpeed : __instance.m_RunSpeed);
                    }
                }
            }

        }
        static float MoveActorCalled = 0;
        [HarmonyPatch(typeof(Lua.Wrapper.WPlayer), nameof(Lua.Wrapper.WPlayer.MoveActor))]
        public class SmoothMovementPatch3
        {
            public static void Prefix(Vector3 delta)
            {
                MoveActorCalled = 1;
            }
        }
        [HarmonyPatch(typeof(CharacterController), "Move")]
        public class SmoothMovementPatch2
        {
            static Vector3 beforeSpeed = Vector3.zero;

            static float playerSpeed = 0;
            public static void Prefix(CharacterController __instance, ref Vector3 motion)
            {
                if (instance.tacticalMovement.Value && __instance.gameObject.name == "Player Fps Actor(Clone)" && !ActorManager.instance.player.IsSeated() && MoveActorCalled <= 0)
                {
                    if (!__instance.isGrounded)
                    {
                        if (ActorManager.instance.player.controller.Prone())
                        {
                            float mx = Mathf.Lerp(beforeSpeed.x, motion.x, RealDelta() * instance.airDiveMovementSmoothing.Value * Mathf.Pow(3, Mathf.Abs(motion.x - beforeSpeed.x)));
                            float mz = Mathf.Lerp(beforeSpeed.z, motion.z, RealDelta() * instance.airDiveMovementSmoothing.Value * Mathf.Pow(3, Mathf.Abs(motion.z - beforeSpeed.z)));
                            motion = new Vector3(mx, motion.y, mz);
                        }
                        else
                        {
                            float mx = Mathf.Lerp(beforeSpeed.x, motion.x, RealDelta() * instance.airMovementSmoothing.Value * Mathf.Pow(3, Mathf.Abs(motion.x - beforeSpeed.x)));
                            float mz = Mathf.Lerp(beforeSpeed.z, motion.z, RealDelta() * instance.airMovementSmoothing.Value * Mathf.Pow(3, Mathf.Abs(motion.z - beforeSpeed.z)));
                            motion = new Vector3(mx, motion.y, mz);
                        }
                    }
                    else
                    {
                        if (playerSpeed < ActorManager.instance.player.speedMultiplier)
                        {
                            return;
                        }
                        else
                        {
                            if (ActorManager.instance.player.controller.Crouch())
                            {
                                float mx = Mathf.Lerp(beforeSpeed.x, motion.x, RealDelta() * instance.slideMovementSmoothing.Value * Mathf.Pow(3, Mathf.Abs(motion.x - beforeSpeed.x)));
                                float mz = Mathf.Lerp(beforeSpeed.z, motion.z, RealDelta() * instance.slideMovementSmoothing.Value * Mathf.Pow(3, Mathf.Abs(motion.z - beforeSpeed.z)));
                                motion = new Vector3(mx, motion.y, mz);
                            }
                            else
                            {
                                float mx = Mathf.Lerp(beforeSpeed.x, motion.x, RealDelta() * instance.groundMovementSmoothing.Value * Mathf.Pow(3, Mathf.Abs(motion.x - beforeSpeed.x)));
                                float mz = Mathf.Lerp(beforeSpeed.z, motion.z, RealDelta() * instance.groundMovementSmoothing.Value * Mathf.Pow(3, Mathf.Abs(motion.z - beforeSpeed.z)));
                                motion = new Vector3(mx, motion.y, mz);
                            }
                        }
                    }
                }
                playerSpeed = ActorManager.instance.player.speedMultiplier;

            }
            public static void Postfix(CharacterController __instance, Vector3 motion)
            {
                if (instance.tacticalMovement.Value && __instance.gameObject.name == "Player Fps Actor(Clone)" && !ActorManager.instance.player.IsSeated() && MoveActorCalled <= 0)
                {
                    beforeSpeed = motion;
                }

            }

        }
        public IEnumerator SpawnImpactFX(RaycastHit hitInfo, Projectile __instance)
        {
            GameObject fx = Instantiate(impactFX, hitInfo.point, Quaternion.Euler(hitInfo.normal));
            fx.transform.up = hitInfo.normal;
            fx.transform.localScale = Vector3.one * Mathf.Clamp(Mathf.Pow(__instance.configuration.damage / 20, 1 / 2) * 1.5f, 0.6f, 3f);
            ParticleSystem p = fx.GetComponent<ParticleSystem>();
            var main = p.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var emission = p.emission;
            emission.burstCount *= Mathf.RoundToInt(Mathf.Clamp(Mathf.Pow(__instance.configuration.damage / 20, 1 / 2) * 1.5f, 1f, 3f));
            foreach (ParticleSystem sys in p.GetComponentsInChildren<ParticleSystem>())
            {
                var main2 = sys.main;
                main2.scalingMode = ParticleSystemScalingMode.Hierarchy;
                var emission2 = sys.emission;
                emission2.burstCount *= Mathf.RoundToInt(Mathf.Clamp(Mathf.Pow(__instance.configuration.damage / 20, 1 / 2) * 1.5f, 1f, 4f));
            }
            p.Play();
            if (impactFXSource == null)
            {
                impactFXSource = new GameObject().AddComponent<AudioSource>();
                impactFXSource.priority = 127;
                impactFXSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                impactFXSource.spatialBlend = 1f;
                impactFXSource.volume = 3f;
                impactFXSource.dopplerLevel = 1;
                impactFXSource.rolloffMode = AudioRolloffMode.Logarithmic;
                impactFXSource.minDistance = 2;
                impactFXSource.maxDistance = 7;
            }
            impactFXSource.transform.position = hitInfo.point;
            AudioClip clip = impactSounds[UnityEngine.Random.Range(0, impactSounds.Count)];
            impactFXSource.PlayOneShot(clip, 1f);
            yield break;
        }
        [HarmonyPatch(typeof(Projectile), "SpawnDecal")]
        public class SpawnDecalPatch
        {
            public static void Prefix(RaycastHit hitInfo, Projectile __instance)
            {
                if (GameManager.instance.defaultWaterSplashLargePrefab.TryGetComponent<AudioSource>(out AudioSource source))
                {
                    mixer = source.outputAudioMixerGroup;
                }
                else
                {
                    instance.Logger.LogError("NO MIXER");
                }
                if (instance.impactFXEnabled.Value)
                {
                    Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red);
                    if (!cooldownEntryDictionary.Keys.Contains(__instance.sourceWeapon.weaponEntry) && impactFXCooldown > 0)
                    {
                        //instance.Logger.LogDebug(previousEntry.name + "orig, ->" + __instance.sourceWeapon.weaponEntry + " cur, cooldown:" + impactFXCooldown.ToString());
                        if (__instance.sourceWeapon.configuration.auto || __instance.sourceWeapon.configuration.projectilesPerShot > 1)
                        {
                            return;
                        }
                    }
                    bool flag = !(__instance is ExplodingProjectile);
                    if (!flag) flag = (__instance as ExplodingProjectile).explosionConfiguration.damage <= 0 || (__instance as ExplodingProjectile).explosionConfiguration.damageRange <= 0;
                    if (__instance.configuration.damage > 4 && flag && Camera.allCameras.Any(x => Vector3.Distance(hitInfo.point, x.transform.position) < 200 && x.isActiveAndEnabled))
                    {
                        if (!cooldownEntryDictionary.Keys.Contains(__instance.sourceWeapon.weaponEntry))
                        {
                            cooldownEntryDictionary.Add(__instance.sourceWeapon.weaponEntry, 0.2f);
                        }
                        impactFXCooldown = instance.configImpactFXCooldown.Value;
                        instance.StartCoroutine(instance.SpawnImpactFX(hitInfo, __instance));
                    }
                }
            }
        }
        public static float impactFXCooldown = -1;
        public static WeaponManager.WeaponEntry previousEntry = null;

        public static Dictionary<WeaponManager.WeaponEntry, float> cooldownEntryDictionary = new Dictionary<WeaponManager.WeaponEntry, float>();
        void Update()
        {
            if (MoveActorCalled > 0)
            {
                MoveActorCalled -= Time.deltaTime;
            }
            if (impactFXCooldown > 0)
            {
                impactFXCooldown -= Time.deltaTime;
            }
            if (cooldownEntryDictionary.Keys.Count > 0)
            {
                List<WeaponManager.WeaponEntry> removeEntries = new List<WeaponManager.WeaponEntry>();
                List<WeaponManager.WeaponEntry> keys = new List<WeaponManager.WeaponEntry>(cooldownEntryDictionary.Keys);
                foreach (WeaponManager.WeaponEntry entry in keys)
                {
                    cooldownEntryDictionary[entry] = cooldownEntryDictionary[entry] - Time.deltaTime;
                    if (cooldownEntryDictionary[entry] < 0)
                    {
                        removeEntries.Add(entry);
                    }
                }
                if (removeEntries.Count > 0)
                {
                    foreach (WeaponManager.WeaponEntry entry in removeEntries)
                    {
                        cooldownEntryDictionary.Remove(entry);
                    }
                }
            }

        }
        /*
        [HarmonyPatch(typeof(MonoBehaviour), "StartCoroutine", new[] { typeof(IEnumerator)})]
        public class FpsActorControllerPatches
        {
            public static bool Prefix(MonoBehaviour __instance, IEnumerator routine)
            {
                if(__instance.GetType() == typeof(FpsActorController) && instance.arcadeMovement.Value)
                {
                    instance.Logger.LogDebug(routine.ToString());
                    if (routine.ToString() == "FpsActorController+<Kick>d__265")
                    {
                        return true;
                        instance.StartCoroutine(instance.KickMod());
                        
                    }
                }
                return true;
            }
        }
        */
        [HarmonyPatch(typeof(Actor), "SpawnLoadoutWeapons")]
        public class SpawnLoadoutWeapons
        {
            public static void Prefix(Actor __instance)
            {
                if (__instance == ActorManager.instance.player && instance.extraWeapons.Value)
                {
                    __instance.weapons = new Weapon[40];
                    List<WeaponManager.WeaponEntry> weaponList = WeaponManager.instance.allWeapons.ToArray().Where(n => GameManager.instance.gameInfo.team[__instance.team].IsWeaponEntryAvailable(n) == true).ToList();
                    for (int i = 5; i < 40; i++)
                    {
                        WeaponManager.WeaponEntry entry = weaponList[UnityEngine.Random.Range(0, weaponList.Count)];

                        instance.Logger.LogDebug(entry.name);
                        __instance.EquipNewWeaponEntry(entry, i, false);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Actor), "NextWeapon")]
        public class NextWeaponPatch
        {
            public static bool Prefix(Actor __instance)
            {
                if (__instance == ActorManager.instance.player && instance.extraWeapons.Value)
                {
                    if (!__instance.dead && !__instance.fallenOver && !__instance.IsOnLadder() && (!__instance.IsSeated() || __instance.seat.CanUsePersonalWeapons()) && !__instance.immersedInWater)
                    {
                        for (int j = 1; j <= __instance.weapons.Length - 1; j++)
                        {
                            int num2 = ((int)typeof(Actor).GetField("activeWeaponSlot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) + j) % __instance.weapons.Length;
                            if (__instance.weapons[num2] != null && !__instance.weapons[num2].IsToggleable())
                            {
                                __instance.SwitchWeapon(num2);
                                return false;
                            }
                        }
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Actor), "PreviousWeapon")]
        public class PreviousWeaponPatch
        {
            public static bool Prefix(Actor __instance)
            {
                if (__instance == ActorManager.instance.player && instance.extraWeapons.Value)
                {
                    if (!__instance.dead && !__instance.fallenOver && !__instance.IsOnLadder() && (!__instance.IsSeated() || __instance.seat.CanUsePersonalWeapons()) && !__instance.immersedInWater)
                    {
                        for (int j = 1; j <= 4; j++)
                        {
                            int num2 = ((int)typeof(Actor).GetField("activeWeaponSlot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) - j + __instance.weapons.Length) % __instance.weapons.Length;
                            if (__instance.weapons[num2] != null && !__instance.weapons[num2].IsToggleable())
                            {
                                __instance.SwitchWeapon(num2);
                                return false;
                            }
                        }
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(LoadoutUi), "LoadSlotEntry")]
        public class AnyWeaponSlot
        {
            public static bool CanUseWeaponEntry(WeaponManager.WeaponEntry entry)
            {
                return (GameManager.GameParameters().playerHasAllWeapons || GameManager.instance.gameInfo.team[GameManager.PlayerTeam()].IsWeaponEntryAvailable(entry));
            }
            public static bool Prefix(LoadoutUi __instance, ref WeaponManager.WeaponEntry __result, WeaponManager.WeaponSlot entrySlot, string keyName)
            {
                if (!instance.anyWeapon.Value)
                {
                    return true;
                }
                if (!PlayerPrefs.HasKey(keyName))
                {
                    foreach (WeaponManager.WeaponEntry weaponEntry in WeaponManager.instance.allWeapons)
                    {
                        if (CanUseWeaponEntry(weaponEntry))
                        {
                            __result = weaponEntry;
                            return false;
                        }
                    }
                    __result = null;
                    return false;
                }
                int @int = PlayerPrefs.GetInt(keyName);
                if (@int == -1)
                {
                    __result = null;
                    return false;
                }
                WeaponManager.WeaponEntry weaponEntry2 = null;
                foreach (WeaponManager.WeaponEntry weaponEntry3 in WeaponManager.instance.allWeapons)
                {
                    if (CanUseWeaponEntry(weaponEntry3))
                    {
                        if (weaponEntry3.nameHash == @int)
                        {
                            __result = weaponEntry3;
                            return false;
                        }
                        if (weaponEntry2 == null)
                        {
                            weaponEntry2 = weaponEntry3;
                        }
                    }
                }
                __result = weaponEntry2;
                return false;
            }
        }
        [HarmonyPatch(typeof(WeaponManager), "GetWeaponTagDictionary")]
        public class AnyWeaponSlot2
        {
            public static bool Prefix(WeaponManager __instance, ref bool allSlots)
            {
                if (instance.anyWeapon.Value)
                {
                    allSlots = true;
                }
                return true;
            }
        }
        /*
        [HarmonyPatch(typeof(AiActorController), "UpdateMovementSpeed")]
        public class KickDamagePatch
        {
            static public bool grabPrivateBool(string name, AiActorController __instance)
            {
                return (bool)typeof(Actor).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            }
            static public TimedAction grabPrivateTimedAction(string name, AiActorController __instance)
            {
                return (TimedAction)typeof(Actor).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            }
            public static bool Prefix(AiActorController __instance)
            {
                if (__instance.IsFollowingScriptedPath())
                {
                    typeof(AiActorController).GetField("movementSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, __instance.activeScriptedPathSeeker.movementSpeed);
                }
                float num = 0.5f;
                if (grabPrivateBool("hasPath",__instance))
                {
                    num = 3.2f;
                    if (__instance.actor.immersedInWater)
                    {
                        num = 4.5f;
                    }
                    else if (!grabPrivateBool("isAlert",__instance) && grabPrivateBool("forceUnalertMovementSpeed", __instance))
                    {
                        num = 1f;
                    }
                    else if (__instance.IsSprinting())
                    {
                        num = Mathf.Lerp(5.5f, 7f, __instance.actor.GetBonusSprintAmount());
                        if (grabPrivateBool("cachedIsFollowing", __instance))
                        {
                            num += 1f;
                        }
                    }
                    else if (!grabPrivateTimedAction("strafeAction", __instance).TrueDone())
                    {
                        num = 1.5f;
                    }
                    else if (grabPrivateBool("isTraversingCorner",__instance) && grabPrivateBool("isInCqcZone", __instance))
                    {
                        num = 1.5f;
                    }
                    else if (__instance.HasSpottedTarget())
                    {
                        if (!__instance.IsMeleeCharging() && Vector3.Distance(this.target.Position(), this.actor.Position()) < 50f)
                        {
                            num = 2f;
                        }
                        else
                        {
                            num = 3.2f;
                        }
                    }
                    else if (this.cachedIsFollowing)
                    {
                        num = Mathf.Clamp(2f * this.cachedFollowTargetDistance, 1f, 3.2f);
                    }
                    if (this.Crouch())
                    {
                        num = Mathf.Min(num, 2f);
                    }
                    else if (this.Prone())
                    {
                        num = Mathf.Min(num, 1f);
                    }
                    num *= this.actor.speedMultiplier;
                }
                if (this.isSquadLeader)
                {
                    num = Mathf.Min(num, this.squad.GetSpeedRestriction());
                }
                this.movementSpeed = Mathf.MoveTowards(this.movementSpeed, num, 10f * Time.deltaTime);
                return false;
            }
        }


        [HarmonyPatch(typeof(Hurtable), nameof(Hurtable.Damage))]
        public class KickDamagePatch
        {
            public static void Prefix(Hurtable __instance, ref DamageInfo info)
            {
               if(info.balanceDamage == 120 && info.healthDamage == 30)
                {
                    instance.Logger.LogDebug(info);
                    info = new DamageInfo(DamageInfo.DamageSourceType.Melee, info.sourceActor, null)
                    {
                        healthDamage = 100f,
                        balanceDamage = 150f,
                        point = info.point,
                        direction = info.direction,
                        impactForce = info.direction * 4f
                    };
                }
            }
        }

        IEnumerator KickMod()
        {
            FpsActorController __instance = (FpsActorController)ActorManager.instance.player.controller;
            typeof(FpsActorController).GetField("kickCooldownAction", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, new TimedAction(1f, false));
            typeof(FpsActorController).GetField("kickAction", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, new TimedAction(0.7f, false));
            __instance.kickAnimation.Stop();
            __instance.kickAnimation.Play();
            AudioSource kickSound = (AudioSource)typeof(FpsActorController).GetField("kickSound", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            kickSound.Play();

            if (__instance.actor.activeWeapon != null)
            {
                __instance.actor.activeWeapon.animator.SetTrigger("kick");
            }
            PlayerFpParent.instance.KickCamera(Vector3.right * 3f);
            yield return new WaitForSeconds(0.15f);
            PlayerFpParent.instance.KickCamera(Vector3.left * 8f);
            yield return new WaitForSeconds(0.1f);
            RaycastHit raycastHit;
            if (!__instance.actor.fallenOver && !__instance.actor.dead && Physics.SphereCast(new Ray(__instance.fpCamera.transform.position, __instance.fpCamera.transform.forward), 1.2f, out raycastHit, 3.0f, 16848129))
            {
                kickSound.PlayOneShot(__instance.kickHitSound);
                if (raycastHit.collider.gameObject.layer == 24)
                {
                    KickActivator componentInParent = raycastHit.collider.gameObject.GetComponentInParent<KickActivator>();
                    if (componentInParent != null)
                    {
                        componentInParent.Trigger();
                    }
                    TriggerUsable component = raycastHit.collider.gameObject.GetComponent<TriggerUsable>();
                    if (component != null)
                    {
                        component.OnKicked();
                    }
                }
                if (Hitbox.IsHitboxLayer(raycastHit.collider.gameObject.layer))
                {
                    Hitbox component2 = raycastHit.collider.GetComponent<Hitbox>();
                    Actor actor = component2.parent as Actor;
                    if (actor != null && actor.dead)
                    {
                        Rigidbody attachedRigidbody = raycastHit.collider.attachedRigidbody;
                        if (attachedRigidbody != null)
                        {
                            attachedRigidbody.AddForceAtPosition(__instance.fpCamera.transform.forward * 300f, raycastHit.point, ForceMode.Impulse);
                        }
                        if (actor.CanSpawnAmmoReserve())
                        {
                            ActorManager.SpawnAmmoReserveOnActor(actor);
                        }
                    }
                    if (component2.parent != __instance.actor)
                    {
                        DamageInfo info = new DamageInfo(DamageInfo.DamageSourceType.Melee, __instance.actor, null)
                        {
                            healthDamage = 50f,
                            balanceDamage = 120f,
                            point = raycastHit.point,
                            direction = __instance.fpCamera.transform.forward,
                            impactForce = __instance.fpCamera.transform.forward * 600f
                        };
                        component2.parent.Damage(info);
                        PlayerFpParent.instance.KickCamera(Vector3.left * 20f);
                    }
                }
                else
                {
                    Rigidbody attachedRigidbody2 = raycastHit.collider.attachedRigidbody;
                    if (attachedRigidbody2 != null)
                    {
                        attachedRigidbody2.AddForceAtPosition(__instance.fpCamera.transform.forward * 300f, raycastHit.point, ForceMode.Impulse);
                    }
                }
            }
            yield break;
        }
        */
        static Vector3 EasingVector(Vector3 one, Vector3 two, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector3(EasingFunction.EaseInOutSine(one.x, two.x, t), EasingFunction.EaseInOutSine(one.y, two.y, t), EasingFunction.EaseInOutSine(one.z, two.z, t));
        }
    }

}


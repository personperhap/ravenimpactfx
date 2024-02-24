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
using Lua.Proxy;
using Lua.Wrapper;
namespace personplus
{
    [BepInPlugin("com.personperhaps.ravenimpactfx", "ravenimpactfx", "1.3")]
    public class ravenimpactfx : BaseUnityPlugin
    {
        public static ravenimpactfx instance = null;
        public static AudioSource impactFXSource = null;
        public static VolumetricFogOptions fogOption = new VolumetricFogOptions();
        public static VolumetricFog fogObject;
        public static Shader calculateFogShader;
        public static Shader applyBlurShader;
        public static Shader applyFogShader;
        public static ComputeShader create3DLutShader;
        public static Texture2D fogTexture2D;
        public static Texture2D blueNoiseTexture2D;
        void Start()
        {
            Debug.Log("ravenimpactfx: Loading!");
            Harmony harmony = new Harmony("ravenimpactfx");
            harmony.PatchAll();
            StartCoroutine(LoadAssetBundle(Assembly.GetExecutingAssembly().GetManifestResourceStream("personplus.assets.impact")));

            StartCoroutine(LoadAudioAssetBundle(Assembly.GetExecutingAssembly().GetManifestResourceStream("personplus.assets.impactaudio")));

            StartCoroutine(LoadFogBundle(Assembly.GetExecutingAssembly().GetManifestResourceStream("personplus.assets.fog.asd")));
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
                                               18f,
                                               "ground smooth movement");
            slideMovementSmoothing = Config.Bind("Tactical.Toggles",
                                               "slideMovementSmoothing",
                                               18f,
                                               "ground smooth movement while crouching (not the same as actual sliding)");
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
                                               13f,
                                               "aim movement");


            extraWeapons = Config.Bind("Funny.Toggles",
                                               "Extra Weapons",
                                               false,
                                               "Gives a few extra weapon slots per loadout");

            impactFXEnabled = Config.Bind("General.Toggles",
                                               "Impact FX",
                                               true,
                                               "Toggles custom impact VFX");
            impactSFXEnabled = Config.Bind("General.Toggles",
                                               "Impact SFX",
                                               false,
                                               "Toggles custom impact SFX");


            configImpactFXCooldown = Config.Bind("General.Toggles",
                                                "ImpactFX Cooldown",
                                                0.005f,
                                                "Time between impact fx");


            damageScreenShakeReduction = Config.Bind("General.Toggles",
                                                "Damage Screenshake Reduction",
                                                1f,
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

            cameraSpring = Config.Bind("Camera.Recoil",
                                                "Camera Spring",
                                                70f,
                                                "how bouncy the camera spring is");
            cameraDrag = Config.Bind("Camera.Recoil",
                                                "Camera Drag",
                                                7f,
                                                "camera spring drag");
            cameraRecoveryCoefficient = Config.Bind("Camera.Recoil",
                                                "Camera Recovery",
                                                1f,
                                                "how great the character is at controlling recoil");

            cameraSimSpeed = Config.Bind("Camera.Recoil",
                                                "Camera Spring Simulation Speed",
                                                1f,
                                                "simulation speed the spring updates at");
            cameraSnap = Config.Bind("Camera.Recoil",
                                                "Camera Snap",
                                                4f,
                                                "camera general snap multiplier");
            cameraParkinsons = Config.Bind("Camera.Parkinsons",
                                                "Camera Parkinsons Multiplier",
                                                1f,
                                                "how shakey your camera gets");
            cameraParkinsonsAdditiveAmount = Config.Bind("Camera.Parkinsons",
                                                "Camera Parkinsons Additive",
                                                1f,
                                                "multiplier for how much each transform delta in an animation adds onto the total.");

            cameraSwing = Config.Bind("Camera.Recoil",
                                                "Camera Swing",
                                                4f,
                                                "funky springs that make the camera bounce (might be annoying)");

            slide = Config.Bind("Tactical.AndBeyond",
                                                "slide",
                                                true,
                                                "slideeeeee");

            vaulting = Config.Bind("Tactical.AndBeyond",
                                                "vaulting",
                                                false,
                                                "get to places you aren't supposed to be");

            wallRunning = Config.Bind("Tactical.AndBeyond",
                                                "wall running",
                                                false,
                                                "huh");
        }

        public ConfigEntry<bool> slide;

        public ConfigEntry<bool> vaulting;

        public ConfigEntry<bool> wallRunning;

        public ConfigEntry<float> weaponSnapDurationMultiplier;
        public ConfigEntry<float> weaponSnapMagnitudeMultiplier;
        public ConfigEntry<float> weaponSnapFrequencyMultiplier;

        public ConfigEntry<float> weaponPositionSpring;
        public ConfigEntry<float> weaponPositionDrag;


        public ConfigEntry<float> cameraRecoveryCoefficient;

        public ConfigEntry<float> weaponRotationSpring;
        public ConfigEntry<float> weaponRotationDrag;

        public ConfigEntry<float> cameraSpring;
        public ConfigEntry<float> cameraDrag;
        public ConfigEntry<float> cameraSnap;

        public ConfigEntry<float> cameraSimSpeed;
        public ConfigEntry<float> cameraParkinsons;
        public ConfigEntry<float> cameraParkinsonsAdditiveAmount;

        public ConfigEntry<float> cameraSwing;

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

        public ConfigEntry<bool> impactSFXEnabled;

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
        IEnumerator LoadFogBundle(Stream path)
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
            var assetLoadRequest = assetBundle.LoadAllAssetsAsync();
            yield return assetLoadRequest;
            if (assetLoadRequest.allAssets == null)
            {
                Logger.LogError("uh oh loading assets failed");
                yield break;
            }
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>(assetLoadRequest.allAssets);
            foreach (UnityEngine.Object asset in objects)
            {
                Logger.LogInfo("Fog Found " + asset.name);
                if (asset.name == "CameraAndFog")
                {
                    fogObject = (asset as GameObject).GetComponent<VolumetricFog>();
                }
                if (asset.name == "Hidden/ApplyBlur")
                {
                    applyBlurShader = (asset as Shader);
                }
                if (asset.name == "Hidden/ApplyFog")
                {
                    applyFogShader = (asset as Shader);
                }
                if (asset.name == "ComputeShader #2")
                {
                    create3DLutShader = (asset as ComputeShader);
                }
                if (asset.name == "noise1")
                {
                    fogTexture2D = (asset as Texture2D);
                }
                if (asset.name == "BlueNoise64Tiled")
                {
                    blueNoiseTexture2D = (asset as Texture2D);
                }
                if (asset.name == "FogOptions")
                {
                    fogOption = (asset as VolumetricFogOptions);
                }
                if (asset.name == "Hidden/CalculateFogDensity")
                {
                    calculateFogShader = (asset as Shader);
                }
            }
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
        [HarmonyPatch(typeof(FpsActorController), "DeployParachute")]
        public class AutoJumpPatch2
        {
            public static void Postfix(FpsActorController __instance, bool __result)
            {
                if (instance.autoJump.Value && !SmoothMovementPatch2.wallRunning && SmoothMovementPatch2.wallJumpCooldown.Done() && SmoothMovementPatch2.leaveGroundCooldown.Done())
                {
                    __result = SteelInput.GetButtonDown(SteelInput.KeyBinds.Jump) && SteelInput.GetButton(SteelInput.KeyBinds.Use);
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

            }
        }
        //[HarmonyPatch(typeof(PostProcessingManager), "RegisterWorldCamera")]
        //public class coolfog
        //{
        //    public static Vector3 GetMeanVector(Vector3[] positions)
        //    {
        //        if (positions.Length == 0)
        //            return Vector3.zero;
        //        float x = 0f;
        //        float y = 0f;
        //        float z = 0f;
        //        foreach (Vector3 pos in positions)
        //        {
        //            x += pos.x;
        //            y += pos.y;
        //            z += pos.z;
        //        }
        //        return new Vector3(x / positions.Length, y / positions.Length, z / positions.Length);
        //    }
        //    public static void Postfix(PostProcessingManager __instance, Camera camera)
        //    {
        //        if(FindObjectOfType<Benchmark>() == null)
        //        {
        //            GameObject gb = new GameObject();
        //            Benchmark bm = gb.AddComponent<Benchmark>();
        //        }

        //        VolumetricFog fog = camera.gameObject.AddComponent<VolumetricFog>();
        //        fog.sunLight = Light.GetLights(LightType.Directional, 1).FirstOrDefault();
        //        fog.applyBlurShader = applyBlurShader;
        //        fog.applyFogShader = applyFogShader;
        //        fog.blueNoiseTexture2D = blueNoiseTexture2D;
        //        fog.create3DLutShader = create3DLutShader;
        //        fog.calculateFogShader = calculateFogShader;
        //        fog.fogOptions = fogOption;
        //        fog.fogOptions.fpsTarget = Enum.FPSTarget.Max60;
        //        fog.fogOptions.addSceneColor = true;
        //        fog.fogOptions.fogInLightColor = RenderSettings.ambientSkyColor;
        //        fog.fogOptions.fogInShadowColor = RenderSettings.ambientGroundColor;
        //        fog.fogOptions.useLightColorForFog = true;
        //        fog.fogOptions.fogWorldPosition = GetMeanVector(ActorManager.instance.spawnPoints.Select(x => x.transform.position).ToArray());
        //        fog.fogOptions.optimizeSettingsFps = true;
        //        fog.fogOptions.rayMarchSteps = 200;
        //        fog.fogOptions.lightIntensity = fog.sunLight.intensity;
        //        fog.fogTexture2D = fogTexture2D;
        //        fog.calculateFogShader = calculateFogShader;

        //        fog.fogLightCasters = Light.GetLights(LightType.Spot, 1).ToList();

        //        QualitySettings.pixelLightCount = 5;
        //        QualitySettings.realtimeReflectionProbes = true;
        //        QualitySettings.shadows = ShadowQuality.All;
        //        QualitySettings.shadowDistance = camera.farClipPlane;
        //        QualitySettings.shadowResolution = ShadowResolution.Low;
        //        QualitySettings.streamingMipmapsMemoryBudget = 2048;
        //        QualitySettings.streamingMipmapsMaxLevelReduction = 2;

        //        fog.Regenerate3DTexture();
        //        camera.depthTextureMode = camera.depthTextureMode | DepthTextureMode.DepthNormals;
        //    }
        //}
        [HarmonyPatch(typeof(PlayerFpParent), "FixedUpdate")]
        public class FovPatch
        {
            static float beforeFovRatio = 1.5f;


            static public Vector3 springBack = Vector3.zero;
            static public SecondOrderDynamics springbackDynamics = new SecondOrderDynamics(0.6f, 0.2f, 0.1f, Vector3.zero);

            static public float heavyness = 1;

            static public Vector3 springBackRotation = Vector3.zero;
            static public SecondOrderDynamics springbackDynamicsRotation = new SecondOrderDynamics(1.2f, 0.2f, 0f, Vector3.zero);


            static public Vector3 weaponNoisePosition = Vector3.zero;
            static public Vector3 passiveWeaponNoisePosition = Vector3.zero;

            static public SecondOrderDynamics weaponNoiseDynamics = new SecondOrderDynamics(1.1f, 0.3f, 0.1f, Vector3.zero);
            static public SecondOrderDynamics passiveWeaponNoiseDynamics = new SecondOrderDynamics(1.0f, 0.7f, 0.1f, Vector3.zero);

            static public SecondOrderDynamics weaponBounceDynamics = new SecondOrderDynamics(1.1f, 0.4f, 0.1f, Vector3.zero);

            static public Vector3 hipOffset = new Vector3(0, -0.1f, 0);

            static public SecondOrderDynamics hipfireOffset = new SecondOrderDynamics(0.6f, 0.2f, 0.1f, Vector3.zero);

            static public SpringPlus bounceSpring = new SpringPlus(100, 3, -Vector3.one * 5f, Vector3.one * 5f, 16, 1f, 0.5f);
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

                        cameraRoot.fieldOfView = EasingFunction.EaseInOutQuint(normalFov, zoomFov, lerpHeight);

                    }
                }
                __instance.fpCamera.fieldOfView += springbackDynamics.y.magnitude * 40;
                springBackRotation = Vector3.MoveTowards(springBackRotation, Vector3.zero, Time.unscaledDeltaTime * 30f);

                passiveWeaponNoisePosition += Vector3.one * Time.unscaledDeltaTime * 2f + bounceSpring.velocity * 0.5f;
                FovPatch.weaponNoisePosition += Vector3.one * Time.unscaledDeltaTime * 0.1f + bounceSpring.velocity * 0.5f;
                Vector3 velocity = FpsActorController.instance.Velocity();
                if (velocity.magnitude > 0.01f)
                {
                    FovPatch.weaponNoisePosition += velocity * 0.1f;
                }
                Vector3 hipTarget = hipOffset;
                Vector3 weaponNoise = Vector3.zero;
                weaponNoise += weaponBounceDynamics.Update(Time.unscaledDeltaTime, CoolTiltingPatch.headDipCoil.velocity * 0.05f);
                weaponNoise += weaponNoiseDynamics.Update(Time.unscaledDeltaTime * 1f, new Vector3(Mathf.Lerp(1, -1, Mathf.PerlinNoise(weaponNoisePosition.x, weaponNoisePosition.y)), Mathf.Lerp(1, -1, Mathf.PerlinNoise(-weaponNoisePosition.x, -weaponNoisePosition.y)), Mathf.Lerp(1, -1, Mathf.PerlinNoise(-weaponNoisePosition.z, -weaponNoisePosition.y + 1)))) * 9f;
                weaponNoise += passiveWeaponNoiseDynamics.Update(Time.unscaledDeltaTime, new Vector3(Mathf.Lerp(1, -1, Mathf.PerlinNoise(-passiveWeaponNoisePosition.x, passiveWeaponNoisePosition.y)), Mathf.Lerp(1, -1, Mathf.PerlinNoise(-passiveWeaponNoisePosition.x, passiveWeaponNoisePosition.y + 2)), Mathf.Lerp(1, -1, Mathf.PerlinNoise(-passiveWeaponNoisePosition.z + 3, -passiveWeaponNoisePosition.y -2)))) * 2f;
                if (FpsActorController.instance.Aiming())
                {
                    weaponNoise *= 0.5f;
                    weaponNoise.y *= 0.5f;
                    hipTarget = Vector3.zero;
                }
                if(weaponNoise.y > 0)
                {
                    weaponNoise.y *= 0.1f;
                }
                Debug.Log(weaponNoise);


                bounceSpring.Update();
                __instance.shoulderParent.localEulerAngles += Vector3.back * CoolTiltingPatch.slideTilt * -20f + weaponNoise * 0.5f + Vector3.right * Vector3.Lerp(springBack, Vector3.zero, 0.5f).magnitude;
                __instance.shoulderParent.localPosition += Vector3.Lerp(springbackDynamics.Update(Time.unscaledDeltaTime * 4.5f * heavyness, Vector3.Lerp(springBack, Vector3.zero, 0.5f)) * 1f, Vector3.zero, 0.6f) * 1.4f + Vector3.Lerp(FovPatch.springbackDynamics.y * 1.8f, Vector3.zero, 0.8f) * 0.7f * Time.timeScale;
                __instance.shoulderParent.position += __instance.shoulderParent.TransformDirection(weaponNoise * 0.01f) + hipfireOffset.Update(Time.unscaledDeltaTime * 2.5f, hipTarget);
                springBack = Vector3.MoveTowards(springBack, Vector3.zero, Time.unscaledDeltaTime * 4);
            }
        }
        public static float GetSnap(PlayerFpParent __instance)
        {
            TimedAction weaponSnapAction = (TimedAction)typeof(PlayerFpParent).GetField("weaponSnapAction", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            float weaponSnapFrequency = (float)typeof(PlayerFpParent).GetField("weaponSnapFrequency", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            float weaponSnapMagnitude = (float)typeof(PlayerFpParent).GetField("weaponSnapMagnitude", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            return Mathf.Sin(weaponSnapAction.Ratio() * (0.1f + 1f - weaponSnapAction.Ratio()) * weaponSnapFrequency / 3) * weaponSnapMagnitude / 2;
        }
        [HarmonyPatch(typeof(Weapon), "Equip")]
        public class EquipFix
        {
            public static void Postfix(Weapon __instance)
            {
                if (__instance.UserIsPlayer() && __instance.arms != null)
                {
                    CoolTiltingPatch.bones = __instance.arms.bones.ToDictionary(x => x, x => new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero));
                    CoolTiltingPatch.ravenCoil.velocity = Vector3.zero;
                    CoolTiltingPatch.ravenCoil.position = Vector3.zero;

                    CoolTiltingPatch.shakeCoil.velocity = Vector3.zero;
                    CoolTiltingPatch.shakeCoil.position = Vector3.zero;
                }
            }
        }
        [HarmonyPatch(typeof(WPlayerCamera), nameof(WPlayerCamera.fpCameraLocalPosition), MethodType.Getter)]
        public class PositionPatchGet
        {
            public static bool Prefix(ref Vector3 __result)
            {
                __result = CoolTiltingPatch.ravenscriptFpPositionOffset;
                return false;
            }
        }
        [HarmonyPatch(typeof(WPlayerCamera), nameof(WPlayerCamera.fpCameraLocalPosition), MethodType.Setter)]
        public class PositionPatchSet
        {
            public static bool Prefix(Vector3 value)
            {
                CoolTiltingPatch.ravenscriptFpPositionOffset = value;
                return false;
            }
        }

        [HarmonyPatch(typeof(WPlayerCamera), nameof(WPlayerCamera.fpCameraLocalRotation), MethodType.Getter)]
        public class RotationPatchGet
        {
            public static bool Prefix(ref Quaternion __result)
            {
                __result = CoolTiltingPatch.ravenscriptFpRotationOffset;
                return false;
            }
        }
        [HarmonyPatch(typeof(WPlayerCamera), nameof(WPlayerCamera.fpCameraLocalRotation), MethodType.Setter)]
        public class RotationPatchSet
        {
            public static bool Prefix(Quaternion value)
            {
                CoolTiltingPatch.ravenscriptFpRotationOffset = value;
                return false;
            }
        }
        [HarmonyPatch(typeof(PlayerFpParent), "LateUpdate")]
        public class CoolTiltingPatch
        {
            static public float swayAmount = 0;

            static public SpringPlus ravenCoil = new SpringPlus(instance.cameraSpring.Value, instance.cameraDrag.Value, -Vector3.one * 20f, Vector3.one * 20f, 16, instance.cameraSimSpeed.Value, 0.5f);

            static public SpringPlus headDipCoil = new SpringPlus(instance.cameraSpring.Value * 2, instance.cameraDrag.Value, -Vector3.one * 20f, Vector3.one * 20f, 16, instance.cameraSimSpeed.Value * 2, 0.5f);

            static public SecondOrderDynamics ravenCoilSpringyness = new SecondOrderDynamics(1.3f, 1.0f, 0.1f, Vector3.zero);

            static public SpringPlus shakeCoil = new SpringPlus(70, 7, -Vector3.one * 20f, Vector3.one * 20f, 16, 1f, 0.5f);

            static public Vector3 averageDelta = Vector3.zero;

            static public Dictionary<Transform, Tuple<Vector3, Vector3>> bones = new Dictionary<Transform, Tuple<Vector3, Vector3>>();

            static public float lastSnap = 0;

            static public Vector3 slideNoise;

            static public Vector3 lerpNoise;

            static public float slideTilt = 0;

            public static float max = 50;

            static public float wallTilt = 0;

            static public Vector3 offsetPos = Vector3.zero;

            static public Vector3 origPos = Vector3.zero;

            static public TimedActionPlus vaultAction = new TimedActionPlus(0.4f);


            static public Vector3 weaponOffset = Vector3.zero;

            static public Vector3 weaponRotationOffset = Vector3.zero;

            static public Vector3 cameraRotationOffset = Vector3.zero;

            static public Vector3 cameraRecoilTarget = Vector3.zero;

            static public SecondOrderDynamics camRotation = new SecondOrderDynamics(0.5f, 0.7f, 0f, Vector3.zero);

            static public SecondOrderDynamics camRecoil = new SecondOrderDynamics(1.5f, 0.5f, 0.1f, Vector3.zero);

            static public SecondOrderDynamics weaponRotation = new SecondOrderDynamics(1.3f, 0.5f, 0.05f, Vector3.zero);

            static public SecondOrderDynamics weaponRecoil = new SecondOrderDynamics(1.8f, 0.4f, 0.1f, Vector3.zero);

            static public Vector3 recoilTarget = Vector3.zero;

            static public float cameraTilt = 0;

            static List<Tuple<Vector3, Quaternion>> armBones = new List<Tuple<Vector3, Quaternion>>();

            static public Vector3 noisePosition = Vector3.zero;

            static public SecondOrderDynamics viewBob = new SecondOrderDynamics(0.8f, 1.0f, 0.1f, Vector3.zero);

            static public SecondOrderDynamics viewBobPosition = new SecondOrderDynamics(0.5f, 1.0f, 0.1f, Vector3.zero);

            static public SecondOrderDynamics walkBob = new SecondOrderDynamics(0.5f, 1.0f, 0.1f, Vector3.zero);

            static public float recoilControl = 1;



            static public Vector3 ravenscriptFpPositionOffset = Vector3.zero;

            static public Quaternion ravenscriptFpRotationOffset = Quaternion.identity;


            


            public static void Postfix(PlayerFpParent __instance)
            {
                Vector3 targetAverage = Vector3.zero;
                if (FpsActorController.instance.actor != null && !GameManager.IsSpectating())
                {
                    if (FpsActorController.instance.actor.activeWeapon != null)
                    {
                        if (FpsActorController.instance.actor.activeWeapon.arms != null)
                        {
                            Dictionary<Transform, Tuple<Vector3, Vector3>> replaceBones = new Dictionary<Transform, Tuple<Vector3, Vector3>>();

                            List<Tuple<Vector3, Quaternion>> newArmBones = new List<Tuple<Vector3, Quaternion>>();
                            foreach (Transform bone in FpsActorController.instance.actor.activeWeapon.arms.bones)
                            {
                                newArmBones.Add(new Tuple<Vector3, Quaternion>(bone.localPosition, bone.localRotation));
                            }
                            armBones = newArmBones;
                            foreach (Transform transform in bones.Keys)
                            {
                                if (transform.localPosition == null)
                                {
                                    break;
                                }
                                if (bones.ContainsKey(transform))
                                {
                                    Vector3 inversePoint = transform.localPosition;
                                    replaceBones.Add(transform, new Tuple<Vector3, Vector3>(bones[transform].Item1, bones[transform].Item2));
                                    targetAverage += Vector3.ClampMagnitude((bones[transform].Item1 - transform.localRotation.eulerAngles) / 5, max) * instance.cameraParkinsonsAdditiveAmount.Value;
                                    targetAverage += Vector3.ClampMagnitude((transform.localPosition - bones[transform].Item2), max) * instance.cameraParkinsonsAdditiveAmount.Value;
                                }
                            }

                            Dictionary<Transform, Tuple<Vector3, Vector3>> dict = FpsActorController.instance.actor.activeWeapon.arms.bones.ToDictionary(x => x, x => new Tuple<Vector3, Vector3>(x.localRotation.eulerAngles, x.localPosition));

                            //foreach (Transform child in FpsActorController.instance.actor.activeWeapon.transform.GetComponentInChildren<Transform>())
                            //{
                            //    if (!dict.ContainsKey(child))
                            //    {
                            //        dict.Add(child, new Tuple<Vector3, Vector3>(child.localRotation.eulerAngles, child.localPosition));
                            //    }
                            //}
                            CoolTiltingPatch.bones = dict;
                        }
                    }

                    if (averageDelta.sqrMagnitude > 0.1f)
                    {
                        shakeCoil.AddVelocity(averageDelta);
                    }
                    else
                    {
                        averageDelta = Vector3.zero;
                    }
                    if (FpsActorController.instance.actor.IsAiming())
                    {
                        averageDelta /= 10;
                    }
                    float scaleOnDistanceToZero = targetAverage.magnitude < averageDelta.magnitude ? (Mathf.Pow(2, (Vector3.Distance(averageDelta, Vector3.zero) * -0.4f) - 6f) * 400 + 1) : 1;
                    float scaleOnTargetDifference = targetAverage.magnitude > 0.2 ? (Mathf.Log10(Mathf.Abs((averageDelta - targetAverage).magnitude + 1f)) + 2.3f) : 1;
                    float returnSpeed = RealDelta() * scaleOnTargetDifference * scaleOnDistanceToZero / 4;
                    averageDelta = Vector3.Lerp(averageDelta, targetAverage, returnSpeed);

                    //averageDelta = new Vector3(averageDelta.x, averageDelta.y, 0);
                    ravenCoil.Update();
                    shakeCoil.Update();
                    headDipCoil.Update();

                    swayAmount = Mathf.Lerp(swayAmount, SteelInput.GetAxis(SteelInput.KeyBinds.Horizontal), RealDelta() * 2);
                    __instance.weaponParent.transform.localEulerAngles = __instance.weaponParent.transform.localEulerAngles + Vector3.back * -swayAmount * 6;
                    lastSnap = Mathf.Lerp(lastSnap, GetSnap(__instance), RealDelta() * 10);
                    slideTilt = Mathf.Lerp(slideTilt, SmoothMovementPatch2.slidingAction.Done() ? 0 : 1, RealDelta() * 3);


                    wallTilt = Mathf.Lerp(wallTilt, SmoothMovementPatch2.wallTilt, RealDelta() * 3);

                    Vector2 mouseInput = new Vector2(SteelInput.GetAxis(SteelInput.KeyBinds.AimX), SteelInput.GetAxis(SteelInput.KeyBinds.AimY));


                    cameraTilt = Mathf.Lerp(cameraTilt, mouseInput.x, RealDelta() * 1.4f);


                    weaponRotationOffset = weaponRotationOffset + new Vector3(mouseInput.y, mouseInput.x, 0) / 4;


                    float clampWeapon = 3;
                    weaponRotationOffset = Vector3.Lerp(weaponRotationOffset, new Vector3(Mathf.Clamp(weaponRotationOffset.x, -clampWeapon, clampWeapon), Mathf.Clamp(weaponRotationOffset.y, -clampWeapon, clampWeapon), Mathf.Clamp(weaponRotationOffset.z, -clampWeapon, clampWeapon)), Time.deltaTime * 10);



                    cameraRotationOffset = cameraRotationOffset + new Vector3(mouseInput.y, mouseInput.x, 0) / 3;


                    float clampCam = 5;
                    cameraRotationOffset = Vector3.Lerp(cameraRotationOffset, new Vector3(Mathf.Clamp(cameraRotationOffset.x, -clampCam, clampCam), Mathf.Clamp(cameraRotationOffset.y, -clampCam, clampCam), Mathf.Clamp(cameraRotationOffset.z, -clampCam, clampCam)), Time.deltaTime * 10);

                    Vector3 rotOffset = weaponRotation.Update(Time.unscaledDeltaTime * 5f, weaponRotationOffset);

                    Vector3 camOffset = camRotation.Update(Time.unscaledDeltaTime * 5f, cameraRotationOffset);
                    Vector3 weaponRecoilUpdate = weaponRecoil.Update(Time.unscaledDeltaTime * 8f, recoilTarget);
                    FpsActorController.instance.controller.m_MouseLook.ApplyScriptedRotation(new Vector2(weaponRecoilUpdate.x, weaponRecoilUpdate.y));
                    recoilTarget = Vector3.MoveTowards(recoilTarget, Vector3.zero, Time.unscaledDeltaTime * 20);

                    cameraRecoilTarget = Vector3.MoveTowards(cameraRecoilTarget, Vector3.zero, Time.unscaledDeltaTime * 6);

                    recoilControl = Mathf.MoveTowards(recoilControl, 1, Time.unscaledDeltaTime * 4);

                    Vector3 currentBob = Vector3.zero;
                    Vector3 currentBobPosition = Vector3.zero;


                    if (FpsActorController.instance.actor.activeWeapon != null)
                    {
                        if (FpsActorController.instance.actor.IsAiming())
                        {
                            weaponRotationOffset = Vector3.zero;
                            currentBobPosition = Vector3.zero;
                            currentBob = Vector3.zero;
                            viewBob.y = Vector3.zero;
                            camOffset = Vector3.zero;
                            cameraRotationOffset = Vector3.zero;
                        }
                        else
                        {
                            Vector3 velocity = FpsActorController.instance.Velocity();
                            float speed = velocity.magnitude;

                            if (FpsActorController.instance.OnGround())
                            {
                                speed *= 0.3f;
                            }

                            if (speed > 0.01f)
                            {
                                noisePosition += velocity * 0.0015f;


                                currentBob = viewBob.Update(Time.unscaledDeltaTime * (speed + 0.1f) * 4f, 1f * new Vector3(Mathf.Lerp(-1, 1, Mathf.PerlinNoise(noisePosition.x, noisePosition.y)), Mathf.Lerp(-1, 1, Mathf.PerlinNoise(-noisePosition.x, noisePosition.z)), Mathf.Lerp(-1, 1, Mathf.PerlinNoise(-noisePosition.z, -noisePosition.y))));
                                currentBobPosition = viewBobPosition.Update(Time.unscaledDeltaTime * (speed + 0.1f) / 4f, 1f * new Vector3(Mathf.Lerp(-1, 1, Mathf.PerlinNoise(noisePosition.x, noisePosition.y)), Mathf.Lerp(-1, 1, Mathf.PerlinNoise(-noisePosition.x, noisePosition.z)), Mathf.Lerp(-1, 1, Mathf.PerlinNoise(-noisePosition.z, -noisePosition.y))));
                            }
                            else
                            {
                                currentBob = viewBob.Update(Time.unscaledDeltaTime * 2f, Vector3.zero);
                                currentBobPosition = viewBobPosition.Update(Time.unscaledDeltaTime / 4f, Vector3.zero);
                            }
                        }
                    }
                    
                    

                    __instance.fpCameraParent.localEulerAngles = __instance.fpCameraParent.localEulerAngles + Vector3.back * -swayAmount * 2 + __instance.GetSpringLocalEuler(true) / 2 * instance.cameraSwing.Value + new Vector3(lastSnap * -6f * instance.cameraSnap.Value, 0f, 0f) + ravenCoilSpringyness.Update(Time.deltaTime * 3f, ravenCoil.position) + shakeCoil.position * 0.8f * instance.cameraParkinsons.Value + Vector3.back * slideTilt * -5f + wallTilt * Vector3.back * -20;

                    __instance.fpCameraParent.localEulerAngles = __instance.fpCameraParent.localEulerAngles + Vector3.back * cameraTilt * -10 + camOffset / 2 + currentBob * 2 + camRecoil.Update(Time.unscaledDeltaTime * 4.5f, cameraRecoilTarget) * 4;
                    __instance.fpCameraParent.localPosition += currentBobPosition / 20f + Vector3.Lerp(FovPatch.springbackDynamics.y * 1.1f, Vector3.zero, 0.8f) / 2;

                    __instance.fpCameraParent.localPosition += ravenscriptFpPositionOffset;

                    __instance.fpCameraParent.localRotation *= ravenscriptFpRotationOffset;

                    __instance.fpCameraParent.localPosition += headDipCoil.position;

                    float t = FpsActorController.instance.GetStepPhase();
                    Vector3 targetBob = new Vector3(-0.05f * Mathf.Pow(Mathf.Abs(Mathf.Sin(3.1415f * t * 3 + 0.5f * 3.1415f) - 0.9f), 2), -0.05f * Mathf.Pow(Mathf.Abs(Mathf.Cos(3.1415f * t * 2 * 3) - 0.9f), 2), 0);
                    __instance.fpCameraParent.localPosition += walkBob.Update(Time.unscaledDeltaTime, targetBob) * 0.8f;



                    __instance.weaponParent.transform.localEulerAngles += Vector3.back * slideTilt * -20f + Vector3.back * cameraTilt * 10 + -rotOffset + FovPatch.springbackDynamicsRotation.Update(Time.unscaledDeltaTime * 6f * FovPatch.heavyness, FovPatch.springBackRotation) * 9;

                   // __instance.weaponParent.transform.localPosition += Vector3.Lerp(FovPatch.springbackDynamics.y * 1.8f, Vector3.zero, 0.8f) * 0.7f * Time.timeScale;


                    FirstPersonController fpc = FpsActorController.instance.GetComponent<FirstPersonController>();
                    CharacterController cc = FpsActorController.instance.GetComponent<CharacterController>();

                    if (instance.vaulting.Value)
                    {
                        if (vaultAction.Done())
                        {
                            if (Physics.SphereCast(__instance.fpCameraParent.position - Vector3.up * 0.6f, 0.3f, fpc.Velocity().ToGround(), out var firstHit, cc.bounds.size.magnitude / 4f, -12945153))
                            {
                                if (Physics.SphereCast(firstHit.point + (__instance.fpCameraParent.forward * 0.3f) + (Vector3.up * 1f * cc.bounds.size.y / 2), 0.1f, Vector3.down, out var secondHit, cc.bounds.size.y * 5, -12945153))
                                {
                                    var offset = cc.height / 2 - cc.radius;
                                    var localPoint0 = cc.center - Vector3.up * offset;
                                    var localPoint1 = cc.center + Vector3.up * offset;
                                    if (Physics.OverlapCapsule(localPoint0 + secondHit.point, localPoint1 + secondHit.point, cc.radius, -12945153).Length == 0)
                                    {
                                        Vector3 storedPos = PlayerFpParent.instance.fpCameraParent.position;
                                        FpsActorController.instance.controller.transform.position = secondHit.point;
                                        FpsActorController.instance.actor.animator.transform.localPosition = Vector3.zero;

                                        origPos = PlayerFpParent.instance.fpCameraParent.InverseTransformPoint(storedPos);
                                        vaultAction.Start();
                                        PlayerFpParent.instance.KickCamera(Vector3.left * 8f);
                                    }
                                }
                            }
                        }
                        else
                        {
                            offsetPos = Vector3.Lerp(origPos, Vector3.zero, EasingFunction.EaseOutSine(0, 1, vaultAction.Ratio()));
                        }
                    }
                    __instance.fpCameraParent.localPosition += offsetPos;
                }
            }
            public static Vector3 LerpWithoutClamp(Vector3 A, Vector3 B, float t)
            {
                return A + (B - A) * t;
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
            static bool rand = false;
            public static void Postfix(Weapon __instance)
            {
                if (__instance.UserIsPlayer() || !__instance.IsMountedWeapon() && !__instance.UserIsAI())
                {


                    float t = __instance.configuration.auto ? 1.8f : 0.6f;
                    float t2 = __instance.configuration.auto ? 1.0f : 0.8f;

                    float offset = 1;

                    t2 = t2 * (__instance.slot == 1 ? 1.1f : 1);



                    Vector3 vector = __instance.configuration.kickback * Vector3.back + UnityEngine.Random.insideUnitSphere * __instance.configuration.randomKick * 1.4f * __instance.configuration.snapDuration * t;
                    float reduction = 1f;
                    if (__instance.user.stance == Actor.Stance.Prone)
                    {
                        vector *= __instance.configuration.kickbackProneMultiplier;
                        reduction = 0.5f;
                    }
                    if (__instance.user.stance == Actor.Stance.Crouch)
                    {
                        vector *= __instance.configuration.kickbackProneMultiplier;
                        reduction = 0.9f;
                    }
                    if (__instance.user.IsAiming())
                    {
                        reduction *= 0.75f;
                    }
                    if (__instance is ThrowableWeapon)
                    {
                        reduction *= 0.1f;
                        offset = 0.1f;
                    }


                    FovPatch.springBackRotation = Vector3.back * Mathf.Lerp(vector.magnitude / 2 + 1, 0, 0.5f) * (UnityEngine.Random.value >= 0.5f ? 1 : -1) * reduction * t2;
                    FovPatch.heavyness = Mathf.Lerp(1 / (__instance.configuration.cooldown + 0.4f), 1, 0.5f) * t2;



                    CoolTiltingPatch.recoilControl += vector.magnitude * 1.2f * offset;
                    FovPatch.springbackDynamics = new SecondOrderDynamics(0.6f, __instance.configuration.snapDuration + 0.1f, 0.1f, FovPatch.springbackDynamics.y);
                    FovPatch.springBack = Vector3.ClampMagnitude(vector * 1.1f, 2.5f) / 11 + UnityEngine.Random.insideUnitSphere * __instance.configuration.randomKick / 4 + Vector3.back * Mathf.Lerp(Mathf.Clamp(vector.magnitude, 2f, 5f) / 2 + 1, 0, 0.5f) / 7 * reduction * t2;
                    CoolTiltingPatch.recoilTarget += new Vector3(((UnityEngine.Random.insideUnitSphere * 5 * (__instance.configuration.snapMagnitude + 0.1f))).x, Mathf.Lerp(-(Mathf.Lerp(vector.z, 0.4f, 0.5f) + 0.1f) * 8 * (__instance.configuration.snapMagnitude + 0.1f), 5f, 0.5f), 0f) / 10 * reduction * CoolTiltingPatch.recoilControl * offset;
                    vector = new Vector3(vector.z, vector.x, -vector.x);
                    CoolTiltingPatch.weaponRotationOffset += new Vector3(vector.x * -0.01f * UnityEngine.Random.insideUnitSphere.x * __instance.configuration.kickback, (vector.y * UnityEngine.Random.insideUnitSphere.y) * __instance.configuration.kickback / 2, (vector.z * UnityEngine.Random.insideUnitSphere.z) * 3) * 42 * reduction * Mathf.Lerp(CoolTiltingPatch.recoilControl, 1, 0.5f) * offset;
                    CoolTiltingPatch.ravenCoil.AddVelocity(vector * 50 * Mathf.Lerp(CoolTiltingPatch.recoilControl, 1, 0.8f));

                    CoolTiltingPatch.ravenCoil.distanceCoefficient = __instance.configuration.cooldown / 8 * instance.cameraRecoveryCoefficient.Value;
                    CoolTiltingPatch.camRecoil.Update(Time.fixedDeltaTime * 5, Vector3.zero);
                    CoolTiltingPatch.camRecoil.y = Vector3.Lerp(Vector3.zero, CoolTiltingPatch.camRecoil.y, 0.1f);
                    CoolTiltingPatch.cameraRecoilTarget = Vector3.Lerp(CoolTiltingPatch.cameraRecoilTarget, Vector3.back * Mathf.Lerp(vector.magnitude / 2 + 1, 0, 0.4f) * (rand ? 1 : -1) * reduction * t2 * CoolTiltingPatch.recoilControl, 0.5f);
                    FovPatch.bounceSpring.AddVelocity(vector);
                    rand = !rand;
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
                SmoothMovementPatch2.wallJumpCooldown.StartLifetime(0.4f);
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
                Vector3 impact = __instance.actor.Velocity();
                impact = new Vector3(0, impact.y, 0);
                CoolTiltingPatch.headDipCoil.AddVelocity(Vector3.ClampMagnitude(impact, 7));
                return false;
            }
        }
        [HarmonyPatch(typeof(FirstPersonController), "PlayFootStepAudio")]
        public class StepDipPatch
        {
            public static void Prefix(FirstPersonController __instance)
            {
                if (__instance.OnGround() && !__instance.inWater && !FpsActorController.instance.actor.IsSeated())
                {
                    CoolTiltingPatch.headDipCoil.spring = instance.cameraSpring.Value * 2 * UnityEngine.Random.Range(0.5f, 1.5f);
                    float bob = UnityEngine.Random.Range(-1.6f, -0.8f);
                    if(FpsActorController.instance.actor.stance != Actor.Stance.Stand)
                    {
                        bob *= 0.4f;
                    }
                    if (!FpsActorController.instance.IsSprinting())
                    {
                        bob *= 0.6f;
                        CoolTiltingPatch.headDipCoil.spring = instance.cameraSpring.Value * UnityEngine.Random.Range(0.5f, 1.5f);
                    }
                    CoolTiltingPatch.headDipCoil.AddVelocity(new Vector3(0, bob, 0) + UnityEngine.Random.insideUnitSphere * 0.1f);
                    
                    FovPatch.bounceSpring.AddVelocity(new Vector3(0, bob, 0));
                }
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

            public static TimedActionPlus slidingAction = new TimedActionPlus(2, false);

            public static TimedActionPlus wallrunningAction = new TimedActionPlus(5, false);

            public static TimedActionPlus wallJumpCooldown = new TimedActionPlus(1f, false);

            public static TimedActionPlus leaveGroundCooldown = new TimedActionPlus(1f, false);

            public static Dictionary<Actor, TimedActionPlus> knockOverCooldown = new Dictionary<Actor, TimedActionPlus>();

            static bool lastAction;

            public static float wallTilt = 0;

            public static Vector3 slidingVector;

            public static bool wallRunning = false;

            public static bool lastWallRunning = false;

            static public Vector3 extraVelocity = Vector3.zero;



            public static float origGravity = 1.2f;

            public static bool beforeGrounded;

            public static float vaultSpeedMultiplier = 0.8f;

            public float extraSlidePower = 0;
            public static void Prefix(CharacterController __instance, ref Vector3 motion)
            {
                if (instance.tacticalMovement.Value && __instance.gameObject.name == "Player Fps Actor(Clone)" && !ActorManager.instance.player.IsSeated() && MoveActorCalled <= 0 && FpsActorController.instance != null)
                {


                    FirstPersonController fpc = FpsActorController.instance.GetComponent<FirstPersonController>();
                    if (!__instance.isGrounded)
                    {
                        if (beforeGrounded == true)
                        {
                            leaveGroundCooldown.StartLifetime(0.4f);
                        }
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
                    bool action = ActorManager.instance.player.controller.IsSprinting();
                    if (lastAction == true && ActorManager.instance.player.controller.Crouch() && slidingAction.Done() && __instance.isGrounded && ActorManager.instance.player.controller.Velocity().ToGround().magnitude > 0.2f && instance.slide.Value)
                    {
                        slidingAction.Start();
                        slidingVector = new Vector3(__instance.velocity.x, 0, __instance.velocity.z) * ActorManager.instance.player.speedMultiplier / 40;
                        PlayerFpParent.instance.KickCamera(new Vector3(UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0, 1)) * 5);
                    }
                    lastAction = action;
                    List<Actor> removeActors = new List<Actor>();
                    foreach (Actor actor in knockOverCooldown.Keys)
                    {
                        if (knockOverCooldown[actor].Done())
                        {
                            removeActors.Add(actor);
                        }
                    }
                    foreach (Actor actor in removeActors)
                    {
                        if (knockOverCooldown.ContainsKey(actor))
                        {
                            knockOverCooldown.Remove(actor);
                        }
                    }
                    if (!slidingAction.Done())
                    {
                        if (FpsActorController.instance.actor.activeWeapon != null)
                        {
                            FpsActorController.instance.actor.activeWeapon.walkBobMultiplier = 0;
                            FpsActorController.instance.actor.activeWeapon.sprintBobMultiplier = 0;
                        }
                        if (!ActorManager.instance.player.controller.OnGround())
                        {
                            slidingAction.end += Time.deltaTime;
                        }
                        //RaycastHit raycastHit;
                        //bool slideSlop = Physics.SphereCast(new Ray(fpc.transform.position + new Vector3(0f, __instance.radius + 0.4f, 0f), Vector3.down), __instance.radius, out raycastHit, 0.6f, -12945153);
                        //if (slideSlop)
                        //{
                        //    var dot = Vector3.Dot(raycastHit.normal, motion);
                        //    var floorAngle = Vector3.Angle(raycastHit.normal, Vector3.up);
                        //    if (floorAngle > 10)
                        //    { // set in inspector or something
                        //        Vector3 a = raycastHit.normal * 2f * Time.deltaTime;
                        //        slidingVector += a;
                        //    }
                        //}

                        //Collider[] cols = Physics.OverlapSphere(__instance.transform.position, 0.6f, 16848129);
                        //Vector3 forward = motion.normalized;

                        //foreach (Collider col in cols)
                        //{
                        //    if (Hitbox.IsHitboxLayer(col.gameObject.layer))
                        //    {
                        //        Hitbox component2 = col.GetComponent<Hitbox>();
                        //        Actor actor = component2.parent as Actor;
                        //        if (!knockOverCooldown.ContainsKey(actor))
                        //        {
                        //            if (actor != null && actor.dead)
                        //            {
                        //                Rigidbody attachedRigidbody = col.attachedRigidbody;
                        //                if (attachedRigidbody != null)
                        //                {
                        //                    attachedRigidbody.AddForceAtPosition(forward * 300f + Vector3.up * 50f, __instance.transform.position, ForceMode.Impulse);
                        //                }
                        //                if (actor.CanSpawnAmmoReserve())
                        //                {
                        //                    ActorManager.SpawnAmmoReserveOnActor(actor);
                        //                }
                        //                knockOverCooldown.Add(actor, new TimedActionPlus(1f));
                        //            }
                        //            if (actor != FpsActorController.instance.actor)
                        //            {
                        //                if (cols.Length > 0)
                        //                {
                        //                    FpsActorController.instance.kickAnimation.GetComponent<AudioSource>().PlayOneShot(FpsActorController.instance.kickHitSound);
                        //                }
                        //                DamageInfo damageInfo = new DamageInfo(DamageInfo.DamageSourceType.Melee, FpsActorController.instance.actor, null)
                        //                {
                        //                    healthDamage = 10f,
                        //                    balanceDamage = 120f,
                        //                    point = col.ClosestPointOnBounds(__instance.transform.position),
                        //                    direction = forward,
                        //                    impactForce = forward * 100f + Vector3.up * 300f,
                        //                };
                        //                DamageInfo info = damageInfo;
                        //                component2.parent.Damage(info);
                        //                if (!knockOverCooldown.ContainsKey(actor))
                        //                {
                        //                    knockOverCooldown.Add(actor, new TimedActionPlus(1f));
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                        motion = Vector3.Lerp(new Vector3(slidingVector.x, motion.y, slidingVector.z), motion, Mathf.Lerp(0, 1, slidingAction.Ratio())) * Time.timeScale;
                        if (!ActorManager.instance.player.controller.Crouch() || (ActorManager.instance.player.controller.Velocity().ToGround().magnitude < 0.2f && slidingAction.Ratio() > 0.5f))
                        {
                            slidingAction.Stop();
                            slidingVector = Vector3.zero;
                        }

                    }
                    else
                    {
                        if (FpsActorController.instance.actor.activeWeapon != null)
                        {
                            Weapon entryWeapon = FpsActorController.instance.actor.activeWeapon.weaponEntry.prefab.GetComponent<Weapon>();
                            FpsActorController.instance.actor.activeWeapon.walkBobMultiplier = entryWeapon.walkBobMultiplier;
                            FpsActorController.instance.actor.activeWeapon.sprintBobMultiplier = entryWeapon.sprintBobMultiplier;
                        }
                    }
                    motion += extraVelocity;

                    wallTilt = 0;
                    wallRunning = false;
                    if (!__instance.isGrounded && instance.wallRunning.Value && !FpsActorController.instance.Prone())
                    {

                        RaycastHit raycastHitLeft;
                        RaycastHit raycastHitRight;

                        Ray rayleft = new Ray(fpc.transform.position, -fpc.cameraParent.right);
                        Ray rayright = new Ray(fpc.transform.position, fpc.cameraParent.right);

                        bool hitleft = Physics.Raycast(rayleft, out raycastHitLeft, __instance.bounds.size.magnitude / 2 + 0.01f, -12945153);

                        bool hitright = Physics.Raycast(rayright, out raycastHitRight, __instance.bounds.size.magnitude / 2 + 0.01f, -12945153);

                        bool hitdown = Physics.Raycast(new Ray(fpc.transform.position, Vector3.down), out raycastHitLeft, __instance.bounds.size.y / 2 + 0.01f, -12945153);

                        if ((hitleft || hitright) && !hitdown)
                        {
                            RaycastHit raycastHit;
                            if (hitleft && hitright)
                            {
                                raycastHit = raycastHitLeft.distance < raycastHitRight.distance ? raycastHitLeft : raycastHitRight;
                                wallTilt = raycastHitLeft.distance < raycastHitRight.distance ? -1 : 1;
                            }
                            else
                            {
                                raycastHit = hitleft ? raycastHitLeft : raycastHitRight;
                                wallTilt = hitleft ? -1 : 1;
                            }

                            if (Mathf.Abs(Vector3.Dot(raycastHit.normal, Vector3.up)) < 0.1f)
                            {
                                Vector3 vector = Vector3.Cross(raycastHit.normal, Vector3.up);
                                if ((FpsActorController.instance.FacingDirection() - vector).magnitude > (FpsActorController.instance.FacingDirection() - -vector).magnitude)
                                    vector = -vector;
                                motion = new Vector3(motion.x, motion.y, motion.z) + vector * ActorManager.instance.player.speedMultiplier / 150;
                                wallRunning = true;

                                FpsActorController.instance.actor.balance = FpsActorController.instance.actor.maxBalance;

                                if (SteelInput.GetButton(SteelInput.KeyBinds.Jump) && wallJumpCooldown.Done() && Time.time - (float)typeof(FirstPersonController).GetField("jumpTimestamp", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fpc) < Mathf.Max(Time.deltaTime, 0.15f) && leaveGroundCooldown.Done())
                                {
                                    wallJumpCooldown.StartLifetime(1f);
                                    fpc.ResetVelocity();
                                    typeof(FirstPersonController).GetField("m_MoveVelocity", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fpc, new Vector3(0, 5, 0) + raycastHit.normal);
                                    extraVelocity += raycastHit.normal * 0.12f;
                                    motion = Vector3.zero;
                                    PlayerFpParent.instance.KickCamera(new Vector3(2, wallTilt * -2, 0));
                                    fpc.m_AudioSource.PlayOneShot(FootstepAudio.GetOutdoorClip());
                                }
                            }

                        }
                    }

                    extraVelocity = Vector3.MoveTowards(extraVelocity, Vector3.zero, Time.deltaTime * 2);
                    if (wallRunning)
                    {
                        typeof(FirstPersonController).GetField("m_GravityMultiplier", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fpc, origGravity / 4);
                    }
                    else
                    {
                        typeof(FirstPersonController).GetField("m_GravityMultiplier", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fpc, origGravity);
                    }
                    beforeGrounded = __instance.isGrounded;
                    playerSpeed = ActorManager.instance.player.speedMultiplier;
                    motion = CoolTiltingPatch.vaultAction.Done() ? motion : motion * vaultSpeedMultiplier;
                }

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
            if (instance.impactSFXEnabled.Value)
            {
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
            }
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
                    if (!cooldownEntryDictionary.Keys.Contains(__instance.sourceWeapon.weaponEntry) && impactFXCooldown > 0)
                    {
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
        //public class MovementData
        //{
        //    public Vector3 storedVelocity = Vector3.zero;

        //    public Vector3 storedMovement = Vector3.zero;

        //    public Vector3 storedMovement2 = Vector3.zero;

        //    public float storedSpeed = 1;
        //}
        //[HarmonyPatch(typeof(AiActorController), nameof(AiActorController.Velocity))]
        //public class SmoothAiMovement
        //{
        //    public static Dictionary<AiActorController, MovementData> actorDictionary = new Dictionary<AiActorController, MovementData>();
        //    public static void Postfix(AiActorController __instance, ref Vector3 __result)
        //    {
        //        Vector3 newVel = __result;
        //        if (actorDictionary.ContainsKey(__instance))
        //        {
        //            newVel = actorDictionary[__instance].storedVelocity;
        //            actorDictionary[__instance].storedVelocity = Vector3.Lerp(actorDictionary[__instance].storedVelocity, __result, ravenimpactfx.RealDelta() * 3);
        //        }
        //        else
        //        {
        //            actorDictionary.Add(__instance, new MovementData());
        //        }
        //        __result = newVel;
        //    }

        //}
        //[HarmonyPatch(typeof(AiActorController), "UpdateMovementSpeed")]
        //public class SmoothAiSpeed
        //{
        //    public static void Postfix(AiActorController __instance)
        //    {
        //        float newSpeed = ((float)typeof(AiActorController).GetField("movementSpeed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance));
        //        float oldSpeed = newSpeed;
        //        if (SmoothAiMovement.actorDictionary.ContainsKey(__instance))
        //        {
        //            newSpeed = SmoothAiMovement.actorDictionary[__instance].storedSpeed;
        //            SmoothAiMovement.actorDictionary[__instance].storedSpeed = Mathf.Lerp(SmoothAiMovement.actorDictionary[__instance].storedSpeed, oldSpeed, ravenimpactfx.RealDelta() * 7);
        //        }
        //        typeof(AiActorController).GetField("movementSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, newSpeed);
        //    }
        //}
        ////[HarmonyPatch(typeof(Actor), "UpdateMovement")]
        ////public class SmoothAiSpeed2
        ////{
        ////    public static void Postfix(Actor __instance)
        ////    {
        ////        if (__instance.aiControlled)
        ////        {
        ////            Vector3 newMove = ((Vector3)typeof(Actor).GetField("movementPosition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance));
        ////            Vector3 oldMove = newMove;
        ////            AiActorController controller = __instance.controller as AiActorController;
        ////            if (SmoothAiMovement.actorDictionary.ContainsKey(controller))
        ////            {
        ////                if(SmoothAiMovement.actorDictionary[controller].storedMovement2 == Vector3.zero)
        ////                {
        ////                    SmoothAiMovement.actorDictionary[controller].storedMovement2 = oldMove;
        ////                }
        ////                newMove = SmoothAiMovement.actorDictionary[controller].storedMovement2;
        ////                SmoothAiMovement.actorDictionary[controller].storedMovement2 = Vector3.Lerp(SmoothAiMovement.actorDictionary[controller].storedMovement2, oldMove, ravenimpactfx.RealDelta() * 100);
        ////            }
        ////            typeof(Actor).GetField("movementPosition", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, newMove);
        ////        }
        ////    }
        ////}
        //[HarmonyPatch(typeof(AiActorController), "Awake")]
        //public class MoreModifiersHehe
        //{
        //    public static void Postfix(AiActorController __instance)
        //    {
        //        Pathfinding.SimpleSmoothModifier smoothModifier = __instance.gameObject.AddComponent<Pathfinding.SimpleSmoothModifier>();
        //        smoothModifier.offset = 2;
        //        smoothModifier.strength = 4;
        //        smoothModifier.iterations = 4;
        //        smoothModifier.smoothType = Pathfinding.SimpleSmoothModifier.SmoothType.Bezier;
        //        __instance.GetComponent<Pathfinding.Seeker>().RegisterModifier(smoothModifier);
        //    }
        //}

        //}
        //[HarmonyPatch(typeof(Rigidbody), "MovePosition")]
        //public class SmoothAiMove
        //{
        //    public static void Postfix(Rigidbody __instance, ref Vector3 position)
        //    {
        //        AiActorController controller;
        //        if (__instance.TryGetComponent<AiActorController>(out controller))
        //        {
        //            Vector3 newPos = position;
        //            if (SmoothAiMovement.actorDictionary.ContainsKey(controller))
        //            {
        //                newPos = SmoothAiMovement.actorDictionary[controller].storedMovement;
        //                Vector3 storedMove = SmoothAiMovement.actorDictionary[controller].storedMovement;
        //                SmoothAiMovement.actorDictionary[controller].storedMovement = Vector3.Lerp(storedMove, position, ravenimpactfx.RealDelta() * 5f);
        //            }
        //            position = newPos;
        //        }
        //    }
        //}

        //    [HarmonyPatch(typeof(Weapon), "SetupTeammateDangerRange")]
        //public class HeheGrenade2
        //{
        //    public static void Postfix(Weapon __instance)
        //    {
        //        __instance.teammateDangerRange = 0;
        //    }
        //}
        public static float OctavePerlin(double x, double y, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;  // Used for normalizing result to 0.0 - 1.0
            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise((float)(x * frequency), (float)(y * frequency)) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return Mathf.Lerp((float)(total / maxValue), -1, 1);
        }
        public static class Perlin
        {
            #region Noise functions

            public static float Noise(float x)
            {
                var X = Mathf.FloorToInt(x) & 0xff;
                x -= Mathf.Floor(x);
                var u = Fade(x);
                return Lerp(u, Grad(perm[X], x), Grad(perm[X + 1], x - 1)) * 2;
            }

            public static float Noise(float x, float y)
            {
                var X = Mathf.FloorToInt(x) & 0xff;
                var Y = Mathf.FloorToInt(y) & 0xff;
                x -= Mathf.Floor(x);
                y -= Mathf.Floor(y);
                var u = Fade(x);
                var v = Fade(y);
                var A = (perm[X] + Y) & 0xff;
                var B = (perm[X + 1] + Y) & 0xff;
                return Lerp(v, Lerp(u, Grad(perm[A], x, y), Grad(perm[B], x - 1, y)),
                               Lerp(u, Grad(perm[A + 1], x, y - 1), Grad(perm[B + 1], x - 1, y - 1)));
            }

            public static float Noise(Vector2 coord)
            {
                return Noise(coord.x, coord.y);
            }

            public static float Noise(float x, float y, float z)
            {
                var X = Mathf.FloorToInt(x) & 0xff;
                var Y = Mathf.FloorToInt(y) & 0xff;
                var Z = Mathf.FloorToInt(z) & 0xff;
                x -= Mathf.Floor(x);
                y -= Mathf.Floor(y);
                z -= Mathf.Floor(z);
                var u = Fade(x);
                var v = Fade(y);
                var w = Fade(z);
                var A = (perm[X] + Y) & 0xff;
                var B = (perm[X + 1] + Y) & 0xff;
                var AA = (perm[A] + Z) & 0xff;
                var BA = (perm[B] + Z) & 0xff;
                var AB = (perm[A + 1] + Z) & 0xff;
                var BB = (perm[B + 1] + Z) & 0xff;
                return Lerp(w, Lerp(v, Lerp(u, Grad(perm[AA], x, y, z), Grad(perm[BA], x - 1, y, z)),
                                       Lerp(u, Grad(perm[AB], x, y - 1, z), Grad(perm[BB], x - 1, y - 1, z))),
                               Lerp(v, Lerp(u, Grad(perm[AA + 1], x, y, z - 1), Grad(perm[BA + 1], x - 1, y, z - 1)),
                                       Lerp(u, Grad(perm[AB + 1], x, y - 1, z - 1), Grad(perm[BB + 1], x - 1, y - 1, z - 1))));
            }

            public static float Noise(Vector3 coord)
            {
                return Noise(coord.x, coord.y, coord.z);
            }

            #endregion

            #region fBm functions

            public static float Fbm(float x, int octave)
            {
                var f = 0.0f;
                var w = 0.5f;
                for (var i = 0; i < octave; i++)
                {
                    f += w * Noise(x);
                    x *= 2.0f;
                    w *= 0.5f;
                }
                return f;
            }

            public static float Fbm(Vector2 coord, int octave)
            {
                var f = 0.0f;
                var w = 0.5f;
                for (var i = 0; i < octave; i++)
                {
                    f += w * Noise(coord);
                    coord *= 2.0f;
                    w *= 0.5f;
                }
                return f;
            }

            public static float Fbm(float x, float y, int octave)
            {
                return Fbm(new Vector2(x, y), octave);
            }

            public static float Fbm(Vector3 coord, int octave)
            {
                var f = 0.0f;
                var w = 0.5f;
                for (var i = 0; i < octave; i++)
                {
                    f += w * Noise(coord);
                    coord *= 2.0f;
                    w *= 0.5f;
                }
                return f;
            }

            public static float Fbm(float x, float y, float z, int octave)
            {
                return Fbm(new Vector3(x, y, z), octave);
            }

            #endregion

            #region Private functions

            static float Fade(float t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            static float Lerp(float t, float a, float b)
            {
                return a + t * (b - a);
            }

            static float Grad(int hash, float x)
            {
                return (hash & 1) == 0 ? x : -x;
            }

            static float Grad(int hash, float x, float y)
            {
                return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
            }

            static float Grad(int hash, float x, float y, float z)
            {
                var h = hash & 15;
                var u = h < 8 ? x : y;
                var v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
                return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
            }

            static int[] perm = {
        151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
        151
    };

            #endregion
        }
        public class SecondOrderDynamics
        {
            private Vector3 xp;
            public Vector3 y, yd;
            public float k1, k2, k3;

            private float PI = 3.14159f;

            private float T_crit;

            public SecondOrderDynamics(float f, float z, float r, Vector3 x0)
            {
                k1 = z / (PI * f);
                k2 = 1 / ((2 * PI * f) * (2 * PI * f));
                k3 = r * z / (2 * PI * f);

                T_crit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);


                xp = x0;
                y = x0;
                yd = Vector3.zero;
            }

            public Vector3 Update(float T, Vector3 x)
            {
                Vector3 xd;
                xd = (x - xp) / T;
                xp = x;
                int iterations = Mathf.CeilToInt(T / T_crit);
                T = T / iterations;
                for (int i = 0; i < iterations; i++)
                {
                    y = y + T * yd;
                    yd = yd + T * (x + k3 * xd - y - k1 * yd) / k2;
                }
                return y;
            }
        }
    }

}


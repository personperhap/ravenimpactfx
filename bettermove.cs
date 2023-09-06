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
namespace ravenimpactfx
{
    [BepInPlugin("com.personperhaps.ravenimpactfx", "ravenimpactfx", "1.0")]
    public class ravenimpactfx : BaseUnityPlugin
    {
        public static ravenimpactfx instance = null;
        public static AudioSource impactFXSource = null;
        void Start()
        {
            Debug.Log("ravenimpactfx: Loading!");
            Harmony harmony = new Harmony("ravenimpactfx");
            harmony.PatchAll();
            StartCoroutine(LoadAssetBundle(Assembly.GetExecutingAssembly().GetManifestResourceStream("ravenimpactfx.assets.impact")));
            StartCoroutine(LoadAudioAssetBundle(Assembly.GetExecutingAssembly().GetManifestResourceStream("ravenimpactfx.assets.impactaudio")));
            instance = this;
            impactFXSource = new GameObject().AddComponent<AudioSource>();
        }
        static GameObject impactFX;
        static GameObject largerImpactFX;
        static List<AudioClip> impactSounds = new List<AudioClip>();

        static List<GameObject> impactFXActivePool = new List<GameObject>();
        static List<GameObject> impactFXInactivePool = new List<GameObject>();
        void Update()
        {
            foreach(GameObject fx in impactFXActivePool)
            {
                if(fx.activeInHierarchy == false)
                {
                    impactFXInactivePool.Add(fx);
                    impactFXActivePool.Remove(fx);
                }
            }
        }

        static GameObject InstantiateFromPool(GameObject obj, Vector3 point, Quaternion rotation)
        {
            GameObject returnObj;
            if(impactFXInactivePool.Count <= 0)
            {
                returnObj = GameObject.Instantiate(obj, point, rotation);
                impactFXActivePool.Add(returnObj);
            }
            else
            {
                returnObj = impactFXInactivePool[0];
                impactFXInactivePool.RemoveAt(0);
                impactFXActivePool.Add(returnObj);
                returnObj.SetActive(true);
                returnObj.transform.position = point;
                returnObj.transform.rotation = rotation;
            }
            return returnObj;
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
        [HarmonyPatch(typeof(FpsActorController), "Update")]
        public class SmoothCameraPatch
        {
            public static void Prefix(out float __state, FpsActorController __instance)
            {
                __state = __instance.fpParent.lean;
            }
            public static void Postfix(float __state, FpsActorController __instance)
            {
                __instance.fpParent.lean = Mathf.Lerp(__state, __instance.Lean() * 1.1f, Time.fixedDeltaTime * 5);
            }
        }

        [HarmonyPatch(typeof(Projectile), "SpawnDecal")]
        public class SpawnDecalPatch
        {
            public static void Prefix(RaycastHit hitInfo, Projectile __instance)
            {
                ravenimpactfx.instance.Logger.LogDebug("Projectile Hit! Typeof: " + __instance.GetType().ToString());
                Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red);
                
                if(__instance.TryGetComponent<Projectile>(out Projectile proj))
                {
                    if (proj.configuration.damage > 5)
                    {
                        ravenimpactfx.instance.Logger.LogDebug("Tryget: " + proj.name);
                        if(impactFX == null)
                        {
                            return;
                        }
                        GameObject fx = InstantiateFromPool(impactFX, hitInfo.point, Quaternion.Euler(hitInfo.normal));
                        fx.transform.up = hitInfo.normal;
                        fx.GetComponent<ParticleSystem>().Play();
                        if(impactFXSource == null)
                        {
                            impactFXSource = new GameObject().AddComponent<AudioSource>();
                        }
                        impactFXSource.outputAudioMixerGroup = GameManager.instance.fpMixerGroup;
                        impactFXSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                        impactFXSource.spatialBlend = 1f;
                        impactFXSource.minDistance = UnityEngine.Random.Range(3f, 2f);
                        impactFXSource.maxDistance = UnityEngine.Random.Range(5f, 10f);
                        impactFXSource.PlayOneShot(impactSounds[UnityEngine.Random.Range(0, impactSounds.Count)]);
                    }
                }
            }
        }


    }
}

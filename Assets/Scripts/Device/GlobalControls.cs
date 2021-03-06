﻿using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls that should be active on all screens. Pretty much a hack to allow people to reset. Now it's more useful.
/// </summary>
public class GlobalControls : MonoBehaviour {
    public static int frame = -1;
    public static PlayerOverworld po;
    public static Misc misc;
    public static UndertaleInput input = new KeyboardInput();
    public static LuaInputBinding luaInput = new LuaInputBinding(input);
    public static AudioClip Music;
    public static Texture2D texBeforeEncounter;
    public static string realName;
    public static string lastScene = "test2";
    public static int uduu; //A secret for everyone :)
    public static int fleeIndex = 0;
    public static bool modDev = false;
    public static bool lastSceneUnitale = false;
    public static bool lastTitle = false;
    public static bool ppcollision = false;
    public static bool allowplayerdef = false;
    public static bool crate = false;
    public static bool retroMode = false;
    public static bool stopScreenShake = false;
    private bool screenShaking = false;
    public static Vector2 beginPosition;
    //public static bool samariosNightmare = false;
    public static string[] nonOWScenes = new string[] { "Battle", "Error", "EncounterSelect", "ModSelect", "GameOver", "TitleScreen", "Disclaimer", "EnterName", "TransitionOverworld", "Intro" };
    public static string[] canTransOW = new string[] { "Battle", "Error", "GameOver" };
    //Wow what's this
    public static Dictionary<int, Dictionary<string, int>> MapEventPages = new Dictionary<int, Dictionary<string, int>>();

    /*void Start() {
        if ((Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) && windows == null)
            windows = new Windows();
        else if (window == null
            misc = new Misc();
    }*/

    void Awake() { SceneManager.sceneLoaded += LoadScene; }

    /// <summary>
    /// Control checking, and way more.
    /// </summary>
	void Update () {
        frame ++;
        if (SceneManager.GetActiveScene().name == "EncounterSelect") lastSceneUnitale = true;
        else                                                         lastSceneUnitale = false;
        if (UserDebugger.instance && Input.GetKeyDown(KeyCode.F9)) {
            if (UserDebugger.instance.gameObject.activeSelf)
                GameObject.Find("Text").transform.SetParent(UserDebugger.instance.gameObject.transform);
            UserDebugger.instance.gameObject.SetActive(!UserDebugger.instance.gameObject.activeSelf);
            Camera.main.GetComponent<FPSDisplay>().enabled = !Camera.main.GetComponent<FPSDisplay>().enabled;
        } else if (SceneManager.GetActiveScene().name == "Battle" && Input.GetKeyDown(KeyCode.H))
            GameObject.Find("Main Camera").GetComponent<ProjectileHitboxRenderer>().enabled =!GameObject.Find("Main Camera").GetComponent<ProjectileHitboxRenderer>().enabled;
        else if (Input.GetKeyDown(KeyCode.Escape) && canTransOW.Contains(SceneManager.GetActiveScene().name)) {
            if (SceneManager.GetActiveScene().name == "Battle" && LuaEnemyEncounter.script.GetVar("unescape").Boolean)
                return;
            UIController.EndBattle();
            //StaticInits.Reset();
        } else if (input.Menu == UndertaleInput.ButtonState.PRESSED && !nonOWScenes.Contains(SceneManager.GetActiveScene().name) && !PlayerOverworld.menuRunning[3] && EventManager.instance.script == null)
            StartCoroutine(PlayerOverworld.LaunchMenu());
        if (Input.GetKeyDown(KeyCode.F4))
            Screen.fullScreen =!Screen.fullScreen;
        //else if (Input.GetKeyDown(KeyCode.L))
        //    MyFirstComponentClass.SpriteAnalyser();
        if (SceneManager.GetActiveScene().name == "Battle")
            switch (fleeIndex) {
                case 0:
                    if (Input.GetKeyDown(KeyCode.F)) fleeIndex++; break;
                case 1:
                    if (Input.GetKeyDown(KeyCode.L)) fleeIndex++;
                    else if (Input.anyKeyDown)       fleeIndex = 0;
                    break;
                case 2:
                    if (Input.GetKeyDown(KeyCode.E)) fleeIndex++;
                    else if (Input.anyKeyDown)       fleeIndex = 0;
                    break;
                case 3:
                    if (Input.GetKeyDown(KeyCode.E)) fleeIndex++;
                    else if (Input.anyKeyDown)       fleeIndex = 0;
                    break;
                case 4:
                    if (Input.GetKeyDown(KeyCode.S)) { fleeIndex = -1; UIController.instance.SuperFlee(); }
                    else if (Input.anyKeyDown)       fleeIndex = 0;
                    break;
            }
        if (Screen.currentResolution.height != 480 || Screen.currentResolution.width != 640) {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.SetResolution(640, 480, false, 0);
        }
        stopScreenShake = false;
    }

    public int GetMapEventPage(string key1, int key2) {
        int value = -5924710;

        if (!MapEventPages[SceneManager.GetActiveScene().buildIndex].TryGetValue(key1, out value)) {
            UnitaleUtil.writeInLogAndDebugger("[WARN]The dictionary doesn't have any data about this map.");
            return -5924710;
        }
        return value;
    }
    
    void LoadScene(Scene scene, LoadSceneMode mode) {
        if (LuaScriptBinder.GetAlMighty(null, "CrateYourFrisk") != null)  crate = LuaScriptBinder.GetAlMighty(null, "CrateYourFrisk").Boolean;
        else                                                              crate = false;
    }

    private IEnumerator IShakeScreen(object[] args) {
        float frames, intensity;
        bool fade;

        try { frames = (float)args[0]; } catch { throw new CYFException("The argument \"seconds\" must be a number."); }
        try { intensity = (float)args[1]; } catch { throw new CYFException("The argument \"intensity\" must be a number."); }
        try { fade = (bool)args[2]; } catch { throw new CYFException("The argument \"fade\" must be a boolean."); }

        Transform tf = Camera.main.transform;
        Vector2 shift = new Vector2(0, 0), totalShift = new Vector2(0, 0);
        float frameCount = 0, intensityBasis = intensity;
        while (frameCount < frames) {
            if (stopScreenShake) {
                tf.position = new Vector3(tf.position.x - totalShift.x, tf.position.y - totalShift.y, tf.position.z);
                screenShaking = false;
                yield break;
            }
            if (fade)
                intensity = intensityBasis * (1 - (frameCount / frames));
            shift = new Vector2((Random.value - 0.5f) * 2 * intensity, (Random.value - 0.5f) * 2 * intensity);

            if (UnitaleUtil.isOverworld())
                PlayerOverworld.instance.cameraShift = new Vector2(PlayerOverworld.instance.cameraShift.x + shift.x - totalShift.x, PlayerOverworld.instance.cameraShift.y + shift.y - totalShift.y);
            else
                tf.position = new Vector3(tf.position.x + shift.x - totalShift.x, tf.position.y + shift.y - totalShift.y, tf.position.z);
            //print(totalShift + " + " + shift + " = " + (totalShift + shift));
            totalShift = shift;
            frameCount++;
            yield return 0;
        }
        screenShaking = false;
        tf.position = new Vector3(tf.position.x - totalShift.x, tf.position.y - totalShift.y, tf.position.z);
    }

    public void ShakeScreen(float duration, float intensity, bool isIntensityDecreasing) {
        if (!screenShaking) {
            screenShaking = true;
            StartCoroutine("IShakeScreen", new object[] { duration, intensity, isIntensityDecreasing });
        }
    }

    void OnApplicationQuit() { /*UnitaleUtil.closeFile();*/ }
}

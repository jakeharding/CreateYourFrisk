﻿using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StaticInits {
    //private static string CurrMODFOLDER;
    //private static string CurrENCOUNTER;
    public static string MODFOLDER;
    public static string ENCOUNTER;
    public static string EDITOR_MODFOLDER = "Title";
    public static string EDITOR_ENCOUNTER = "";
    private static bool firstInit = false;

    public static bool Initialized { get; set; }
    
    public delegate void LoadedAction();
    public static event LoadedAction Loaded;

    static void OnEnable() {  UIController.SendToStaticInits += StaticInits.SendLoaded; }
    static void OnDisable() { UIController.SendToStaticInits -= StaticInits.SendLoaded; }

    public static void Start() {
        if (!firstInit) {
            firstInit = true;
            SpriteRegistry.Start();
            AudioClipRegistry.Start();
            SpriteFontRegistry.Start();
        }
        if (MODFOLDER == null || MODFOLDER == "")
            MODFOLDER = EDITOR_MODFOLDER;
        if (ENCOUNTER == null || ENCOUNTER == "")
            ENCOUNTER = EDITOR_ENCOUNTER;
        //if (CurrMODFOLDER != MODFOLDER || CurrENCOUNTER != ENCOUNTER)
        initAll();
        Initialized = true;
    }

    public static void initAll() {
        if (!Initialized && (SceneManager.GetActiveScene().name != "Battle" || GlobalControls.lastSceneUnitale)) {
            //UnitaleUtil.createFile();
            if (GlobalControls.lastSceneUnitale)
                GlobalControls.lastSceneUnitale = false;
            Stopwatch sw = new Stopwatch(); //benchmarking terrible loading times
            sw.Start();
            ScriptRegistry.init();
            sw.Stop();
            UnityEngine.Debug.Log("Script registry loading time: " + sw.ElapsedMilliseconds + "ms");
            sw.Reset();

            sw.Start();
            SpriteRegistry.init();
            sw.Stop();
            UnityEngine.Debug.Log("Sprite registry loading time: " + sw.ElapsedMilliseconds + "ms");
            sw.Reset();

            sw.Start();
            AudioClipRegistry.init();
            sw.Stop();
            UnityEngine.Debug.Log("Audio clip registry loading time: " + sw.ElapsedMilliseconds + "ms");
            sw.Reset();

            sw.Start();
            SpriteFontRegistry.init();
            sw.Stop();
            UnityEngine.Debug.Log("Sprite font registry loading time: " + sw.ElapsedMilliseconds + "ms");
            sw.Reset();
        } else 
            Initialized = true;
        LateUpdater.init(); // must be last; lateupdater's initialization is for classes that depend on the above registries
        MusicManager.src = Camera.main.GetComponent<AudioSource>();
        SendLoaded();
        //CurrENCOUNTER = ENCOUNTER;
        //CurrMODFOLDER = MODFOLDER;
    }

    public static void SendLoaded() {
        if (Loaded != null)
            Loaded();
    }

    /*public static void Reset() {
        Initialized = false;
        LuaScriptBinder.Clear();
        PlayerCharacter.instance.Reset();
    }*/
}
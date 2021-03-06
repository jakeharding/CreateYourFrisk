﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonSharp.Interpreter;

public class LuaGeneralOW {
    private TextManager textmgr;
    public ScriptWrapper appliedScript;

    public delegate void LoadedAction(string name, object args);
    [MoonSharpHidden]
    public static event LoadedAction StCoroutine;

    [MoonSharpHidden]
    public LuaGeneralOW(TextManager textmgr) { this.textmgr = textmgr; }

    [CYFEventFunction] public void HiddenReloadAppliedScript() { EventManager.instance.CheckCurrentEvent(); }

    /// <summary>
    /// Displays a text.
    /// </summary>
    /// <param name="texts"></param>
    /// <param name="formatted"></param>
    /// <param name="mugshots"></param>
    [CYFEventFunction]
    public void SetDialog(DynValue texts, bool formatted = true, DynValue mugshots = null) {
        TextMessage[] textmsgs = new TextMessage[texts.Table.Length];
        for (int i = 0; i < texts.GetLength().Number; i++)
            textmsgs[i] = new TextMessage(texts.Table.Get(i + 1).String, formatted, false, mugshots.ToString() != "void" ? mugshots.Table.Get(i + 1).String : null);
        textmgr.setTextQueue(textmsgs);
        textmgr.transform.parent.parent.SetAsLastSibling();
    }
    
    /// <summary>
    /// Makes a choice, like when you have to choose the blue or the red pill
    /// </summary>
    /// <param name="question"></param>
    /// <param name="varIndex"></param>
    [CYFEventFunction]
    public void SetChoice(DynValue choices, string question = null) {
        bool threeLines = false;
        TextMessage textMsgChoice = new TextMessage("", false, false, true);
        textMsgChoice.addToText("[mugshot:null]");
        string[] finalText = new string[3];

        //Do not put more than 3 lines and 2 choices
        //If the 3rd parameter is a string, it has to be a question
        if (question != null) {
            textMsgChoice.addToText(question + "\n");

            //int lengthAfter = question.Split('\n').Length;
            //if (question.Split('\n').Length > lengthAfter) lengthAfter = question.Split('\n').Length;

            /*if (lengthAfter > 2)*/ //textMsgChoice.addToText("\n");
            //else                 textMsgChoice.addToText("\n\n");
        }
        for (int i = 0; i < choices.Table.Length; i++) {
            //If there's no text, just don't print it
            if (i == 2 && question != null)
                break;
            if (choices.Table.Get(i + 1).String == null)
                continue;

            string[] preText = choices.Table.Get(i + 1).String.Split('\n'), text = new string[3];
            if (preText.Length == 3)
                threeLines = true;
            for (int j = 0; j < 3; j++) {
                if (j < preText.Length) text[j] = preText[j];
                else text[j] = "";
            }

            for (int k = 0; k < 3; k++) {
                if (text[k] != "")
                    if (k == 0) text[k] = "* " + text[k];
                    else text[k] = "  " + text[k];

                finalText[k] += text[k] + '\t';
                if (k == text.Length - 1)
                    break;
            }
        }

        //Add the text to the text to print then the SetChoice function with its parameters
        if (!threeLines && question != null)
            textMsgChoice.addToText("\n");
        textMsgChoice.addToText(finalText[0] + "\n" + finalText[1] + "\n" + finalText[2]);
        textmgr.setText(textMsgChoice);
        textmgr.transform.parent.parent.SetAsLastSibling();

        StCoroutine("ISetChoice", new object[] { question != null, threeLines });
    }

    [CYFEventFunction]
    public void Wait(int frames) { StCoroutine("IWait", frames); }

    /// <summary>
    /// Function that ends when the player press the button "Confirm"
    /// </summary>
    [CYFEventFunction]
    public void WaitForInput() { StCoroutine("IWaitForInput", null); }

    /// <summary>
    /// Launch the GameOver screen
    /// </summary>
    [CYFEventFunction]
    public void GameOver(DynValue deathText = null, string deathMusic = null) {
        PlayerCharacter.instance.HP = PlayerCharacter.instance.MaxHP;
        Transform rt = GameObject.Find("Player").GetComponent<Transform>();
        rt.position = new Vector3(rt.position.x, rt.position.y, -1000);
        string[] deathTable = null;

        if (deathText != null) {
            deathTable = new string[deathText.Table.Length];
            for (int i = 0; i < deathText.Table.Length; i++)
                deathTable[i] = deathText.Table[i].ToString();
        }

        GlobalControls.Music = UnitaleUtil.GetCurrentOverworldAudio().clip;
        PlayerOverworld.instance.enabled = false;

        UnitaleUtil.writeInLogAndDebugger(GameObject.FindObjectOfType<GameOverBehavior>().name);

        GameObject.FindObjectOfType<GameOverBehavior>().StartDeath(deathTable, deathMusic);
        appliedScript.Call("CYFEventNextCommand");
    }

    /// <summary>
    /// Plays and adjust the volume of a chosen bgm.
    /// </summary>
    /// <param name="bgm">The name of the chosen BGM to play.</param>
    /// <param name="volume">The volume of the BGM. Clamped from 0 to 1.</param>
    [CYFEventFunction]
    public void PlayBGM(string bgm, float volume) {
        volume = Mathf.Clamp01(volume);
        if (AudioClipRegistry.GetMusic(bgm) == null)
            throw new CYFException("General.PlayBGM: The given BGM doesn't exist. Please check if you haven't mispelled it.");
        AudioSource audio = UnitaleUtil.GetCurrentOverworldAudio();
        audio.clip = AudioClipRegistry.GetMusic(bgm);
        audio.volume = volume;
        audio.Play();
        appliedScript.Call("CYFEventNextCommand");
    }

    /// <summary>
    /// Stops the current BGM.
    /// </summary>
    /// <param name="fadeFrames"></param>
    [CYFEventFunction]
    public void StopBGM(int fadeFrames = 0) {
        if (EventManager.instance.bgmCoroutine)
            throw new CYFException("General.StopBGM: The music is already fading.");
        else if (!GameObject.Find("Main Camera OW").GetComponent<AudioSource>().isPlaying)
            throw new CYFException("General.StopBGM: There is no current BGM.");
        else if (fadeFrames < 0)
            throw new CYFException("General.StopBGM: The fade time has to be positive or equal to 0.");
        StCoroutine("fadeBGM", fadeFrames);
        appliedScript.Call("CYFEventNextCommand");
    }

    /// <summary>
    /// Plays a selected sound at a given volume.
    /// </summary>
    /// <param name="sound"></param>
    /// <param name="volume"></param>
    [CYFEventFunction]
    public void PlaySound(string sound, float volume = 0.65f) {
        volume = Mathf.Clamp01(volume);
        if (AudioClipRegistry.GetSound(sound) == null)
            throw new CYFException("General.PlaySound: The given BGM doesn't exist. Please check if you haven't mispelled it.");
        UnitaleUtil.PlaySound("PlaySound", AudioClipRegistry.GetSound(sound), volume);
        //GameObject.Find("Player").GetComponent<AudioSource>().PlayOneShot(AudioClipRegistry.GetSound(sound), volume);
        appliedScript.Call("CYFEventNextCommand");
    }

    /// <summary>
    /// Saves the game. Pretty obvious, heh.
    /// </summary>
    [CYFEventFunction]
    public void Save() { StCoroutine("ISave", null); }

    [CYFEventFunction]
    //NOT WORKING
    public void TitleScreen() {
        NewMusicManager.DestroyChannel("StaticKeptAudio");
        GameObject.Destroy(GameObject.Find("Player"));
        GameObject.Destroy(GameObject.Find("Canvas OW"));
        StaticInits.MODFOLDER = "Title";
        StaticInits.Initialized = false;
        StaticInits.initAll();
        GameObject.Destroy(GameObject.Find("Main Camera OW"));
        SceneManager.LoadScene("TitleScreen");
    }

    /// <summary>
    /// Sets an encounter of the current mod folder, with a given encounter name
    /// The boolean is used to tell if the encounter anim will be short
    /// </summary>
    /// <param name="encounterName"></param>
    /// <param name="quickAnim"></param>
    [CYFEventFunction]
    public void SetBattle(string encounterName, bool quickAnim = false, bool ForceNoFlee = false) { PlayerOverworld.instance.SetEncounterAnim(encounterName, quickAnim, ForceNoFlee); }
}

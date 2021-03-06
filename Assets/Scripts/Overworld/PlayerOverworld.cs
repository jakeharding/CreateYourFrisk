﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;

public class PlayerOverworld : MonoBehaviour {
    public static PlayerOverworld instance;
    public int blockingLayer;               //Layer on which collision will be checked
    public int EventLayer;                  //Layer of the events, colliding too
    public int forcedMove = 0;              //Direction of a forced move
    //public int rolled = 0;
    public float speed = 3;
    public bool firstTime = false;          //Boolean used to not launch another event a the end of the previous event
    public static bool playerNoMove = false;      //Is the player not able to move?
    public static bool[] menuRunning = new bool[] { false, false, false, false };
    public Vector2 lastMove;                //The Player's last input
    public Vector2 cameraShift = new Vector2();
    public Transform PlayerPos;             //The Transform component attached to this object
    public Image utHeart;
    public static AudioSource audioKept;
    public LuaSpriteController sprctrl;
    public TextManager textmgr;             //The map's text manager
    public Vector2 hackSizeBoxCollider;

    private int battleWalkCount;            //Will be used to check the battle appearance
    private float TimeIndicator = 0f;       //A time indicator used for the soul's movement during the pre-Encounter anim
    private bool inBattleAnim = false;
    private Rigidbody2D rb2D;               //The Rigidbody2D component attached to this object
    private AudioSource uiAudio;            //AudioSource used for the pre-Encounter sounds
    private CYFAnimator animator;

    //private bool lockedCamera = false;    //Used to stop the camera's position refresh

    //Start overrides the Start function of MovingObject
    public void Start() {
        GameObject Player = GameObject.Find("Player");
        uiAudio = Player.GetComponent<AudioSource>();
        utHeart = GameObject.Find("utHeart").GetComponent<Image>();
        utHeart.color = new Color(utHeart.color.r, utHeart.color.g, utHeart.color.b, 0);
        
        //StartCoroutine(LaunchMusic());

        //Get a component reference to the Player's transform
        PlayerPos = Player.transform;

        //If the player's position already exists, move the player to it
        if (LuaScriptBinder.Get(null, "PlayerPosX") != null && LuaScriptBinder.Get(null, "PlayerPosY") != null) {
            Vector2 temp = new Vector3((float)LuaScriptBinder.Get(null, "PlayerPosX").Number, (float)LuaScriptBinder.Get(null, "PlayerPosY").Number);
            PlayerPos.position = temp;
        } else
            PlayerPos.position = GlobalControls.beginPosition;

        //Get a component reference to the Player's animator component
        //animator = Player.GetComponent<Animator>();
        sprctrl = new LuaSpriteController(Player.GetComponent<SpriteRenderer>());
        sprctrl.loopmode = "LOOP";
        animator = Player.GetComponent<CYFAnimator>();

        //Get a component reference to this object's Rigidbody2D
        rb2D = Player.GetComponent<Rigidbody2D>();

        //Get the layer that blocks our object, here BlockingLayer and EventLayer
        blockingLayer = LayerMask.GetMask("BlockingLayer");
        EventLayer = LayerMask.GetMask("EventLayer");

        textmgr = GameObject.Find("TextManager OW").GetComponent<TextManager>();

        //How many times the player has to move before encounter an enemy
        battleWalkCount = Math.randomRange(300, 1000);

        //Let's set the last movement to Down.
        lastMove = new Vector2(0, 1);

        EventManager.instance = GameObject.Find("Main Camera OW").GetComponent<EventManager>();

        if (!NewMusicManager.Exists("StaticKeptAudio")) {
            audioKept = NewMusicManager.CreateChannelAndGetAudioSource("StaticKeptAudio");
            audioKept.loop = true;
            GameObject.DontDestroyOnLoad(audioKept);
        }

        instance = this;

        StartCoroutine(textCoroutine());
    }

    void OnEnable() {
        SceneManager.sceneLoaded += LoadScene;
        Fading.FinishFade += FinishFade;
    }


    void OnDisable() {
        SceneManager.sceneLoaded -= LoadScene;
        Fading.FinishFade -= FinishFade;
    }

    private void LoadScene(Scene scene, LoadSceneMode mode) { playerNoMove = true; } //Scene loaded
    private void FinishFade() { playerNoMove = false; }

    private void NextText() {
        if (!textmgr.allLinesComplete() && (textmgr.canAutoSkipAll() || textmgr.lineComplete()))
            textmgr.nextLine();
        else if ((textmgr.allLinesComplete() || textmgr.canAutoSkipAll()) && textmgr.lineCount() != 0) {
            textmgr.transform.parent.parent.SetAsFirstSibling();
            textmgr.setTextQueue(null);
            textmgr.destroyText();
            textmgr.setTextFrameAlpha(0);
            if (EventManager.instance.script != null)
                EventManager.instance.script.Call("CYFEventNextCommand");
            else
                playerNoMove = false; //End text no event
        }
    }

    IEnumerator textCoroutine() {
        while (true) {
            //UnitaleUtil.writeInLogAndDebugger("inText = " + inText + ", textmgr.lineCount = " + textmgr.lineCount());
            if (GameObject.Find("textframe_border_outer").GetComponent<Image>().color.a != 0) {
                //UnitaleUtil.writeInLogAndDebugger("blockskip = " + textmgr.blockSkip + ", skipNowIfBlocked = " + textmgr.skipNowIfBlocked);
                yield return 0;
                try {
                    if (textmgr.canAutoSkipAll())
                        NextText();
                    if (GlobalControls.input.Cancel == UndertaleInput.ButtonState.PRESSED && !textmgr.blockSkip && !textmgr.lineComplete() && textmgr.canSkip())
                        textmgr.skipLine();
                    else if (GlobalControls.input.Confirm == UndertaleInput.ButtonState.PRESSED && !textmgr.blockSkip)
                        NextText();
                } catch { }
            } else
                yield return 0;
        }
    }

    private void Update() {
        if (GameOverBehavior.gameOverContainer.activeSelf)
            return;
        
        if ((Vector2)PlayerPos.position == new Vector2(0, 0))
            PlayerPos.position = GlobalControls.beginPosition;
        //Used to increment TimeIndicator for our pre-Encounter anim
        if (TimeIndicator > 0 && TimeIndicator < 1) {
            TimeIndicator += Time.deltaTime;

            if (TimeIndicator > 1)
                TimeIndicator = 1;

            Vector2 positionCamera = Camera.main.transform.position;
            Vector2 end = new Vector2(PlayerPos.position.x - (positionCamera.x - 320 + 48), PlayerPos.position.y - (positionCamera.y - 240 + 25));
            Image utHeart = GameObject.Find("utHeart").GetComponent<Image>();

            //Here we move the heart to the place it'll be on the beginning of the battle
            if (utHeart.transform.position != new Vector3(positionCamera.x - 320 + 48, positionCamera.y - 240 + 25, -1f)) {
                Vector3 positionTemp = new Vector3(PlayerPos.position.x - (end.x * TimeIndicator), PlayerPos.position.y - (end.y * TimeIndicator), -11);
                utHeart.transform.position = positionTemp;
            }
        }

        int horizontal = 0;     //Used to store the horizontal move direction
        int vertical = 0;       //Used to store the vertical move direction        
        int currentDirection = 0;
        //If you locked the player, do nothing
        if (!playerNoMove) {
            horizontal = (int)(Input.GetAxisRaw("Horizontal"));
            vertical = (int)(Input.GetAxisRaw("Vertical"));
            //Just some animations switches ;)
            if (animator.movementDirection == 0) {
                if (GlobalControls.input.Up == UndertaleInput.ButtonState.HELD) currentDirection = 8;
                else if (GlobalControls.input.Down == UndertaleInput.ButtonState.HELD) currentDirection = 2;
                else if (GlobalControls.input.Right == UndertaleInput.ButtonState.HELD) currentDirection = 6;
                else if (GlobalControls.input.Left == UndertaleInput.ButtonState.HELD) currentDirection = 4;
            }
            if (GlobalControls.input.Up == UndertaleInput.ButtonState.PRESSED) currentDirection = 8;
            else if (GlobalControls.input.Right == UndertaleInput.ButtonState.PRESSED) currentDirection = 6;
            else if (GlobalControls.input.Left == UndertaleInput.ButtonState.PRESSED) currentDirection = 4;
            else if (GlobalControls.input.Down == UndertaleInput.ButtonState.PRESSED) currentDirection = 2;
            if ((animator.beginAnim.Contains("Up") && GlobalControls.input.Up == UndertaleInput.ButtonState.RELEASED) || 
                (animator.beginAnim.Contains("Right") && GlobalControls.input.Right == UndertaleInput.ButtonState.RELEASED) ||
                (animator.beginAnim.Contains("Left") && GlobalControls.input.Left == UndertaleInput.ButtonState.RELEASED) ||
                (animator.beginAnim.Contains("Down") && GlobalControls.input.Down == UndertaleInput.ButtonState.RELEASED)) {
                if (horizontal < 0)      currentDirection = 4;
                else if (horizontal > 0) currentDirection = 6;
                else if (vertical > 0)   currentDirection = 8;
                else if (vertical < 0)   currentDirection = 2;
            }

            //Special keys

            /*
            if (PlayerPos.position.x < 40) {
                string mapName;
                if (UnitaleUtil.MapCorrespondanceList.ContainsKey(SceneManager.GetActiveScene().name))  mapName = UnitaleUtil.MapCorrespondanceList[SceneManager.GetActiveScene().name];
                else                                                                                    mapName = SceneManager.GetActiveScene().name;
                LuaScriptBinder.Set(null, "PlayerMap", DynValue.NewString(mapName));
                LuaScriptBinder.Set(null, "PlayerPosX", DynValue.NewNumber(320));
                LuaScriptBinder.Set(null, "PlayerPosY", DynValue.NewNumber(160));

                //GameObject.Find("Main Camera OW").tag = "Untagged";
                GlobalControls.Music = UnitaleUtil.GetCurrentOverworldAudio().clip;
                EventManager.SetEventStates();
                SceneManager.LoadScene("ModSelect");
            }*/

            /*if (Input.GetKeyDown("p")) {
                SetDialog(new string[] { "[letters:3]DUN[w:4][letters:4] DUN[w:5][letters:6] DUN!",
                                     "Did you see?[w:10][mugshot:rtlukark_determined:skipover] Yeah, it worked!",
                                     "No[w:2].[w:2].[w:2].[w:2]?[w:10][mugshot:rtlukark_determined:skipover] I'll do it again.",
                                     "Here's [letters:15]the first test and it [letters:14]looks like it works!",
                                     "See? [w:10]It worked! [w:10]I'll give you the text of the first sentence...",
                                     "(letters:3) DUN (w:4)(letters:4) DUN (w:5)(letters:6) DUN!",
                                     "Hope you liked it!" },
                          true, new string[] { "rtlukark_angry", "rtlukark_normal", "rtlukark_waitwhat", "rtlukark_angry", "rtlukark_determined", "rtlukark_perv", "rtlukark_determined" });
            } else*/
            if (Input.GetKeyDown("m"))
                GameObject.Find("Main Camera OW").GetComponent<AudioSource>().time = Random.value * GameObject.Find("Main Camera OW").GetComponent<AudioSource>().clip.length;
            else if (Input.GetKeyDown("h") && SceneManager.GetActiveScene().name == "test2") {
                /*if (GameObject.Find("Event1").GetComponent<EventOW>().actualPage == 4)
                    rolled++;
                if (GameObject.Find("Event1").GetComponent<EventOW>().actualPage == 666) {
                    LuaEventOW.SetPage2("Event1", 1); 
                    rolled++;
                } else*/
                LuaEventOW.SetPage2("Event1", (GameObject.Find("Event1").GetComponent<EventOW>().actualPage + 1) % 4);
                /*if (GameObject.Find("Event1").GetComponent<EventOW>().actualPage == 1 && rolled % 4 == 3)  LuaEventOW.SetPage2("Event1", 666);
                else*/
                if (GameObject.Find("Event1").GetComponent<EventOW>().actualPage == 0) LuaEventOW.SetPage2("Event1", 4);
                /*if (GameObject.Find("Event1").GetComponent<EventOW>().actualPage == 666)
                    SetDialog(new string[] { "[color:ff0000]Event page = " + GameObject.Find("Event1").GetComponent<EventOW>().actualPage + " :)" }, true, new string[] { "rtlukark_determimed" });
                else*/
                SetDialog(new string[] { "Event page = " + GameObject.Find("Event1").GetComponent<EventOW>().actualPage + "." }, true, new string[] { "rtlukark_determined" });
            } else if (Input.GetKeyDown("t") && GameObject.Find("textframe_border_outer").GetComponent<Image>().color.a == 0)
                SetDialog(new string[] { "Your game is saved at\n" + Application.persistentDataPath + "/save.gd" }, true, null);
            else if (Input.GetKeyDown("b"))
                SetEncounterAnim();
        }        

        if (currentDirection != 0) animator.movementDirection = currentDirection;

        //Check is the movement is possible
        if ((Vector2)PlayerPos.position != new Vector2(0, 0))
            AttemptMove(horizontal, vertical);
        
        if (menuRunning[2])
            if (GlobalControls.input.Menu == UndertaleInput.ButtonState.PRESSED)
                CloseMenu(true);
    }

    private IEnumerator LaunchMusic() {
        yield return 0;
        yield return Application.isLoadingLevel;
        
        AudioSource audio = UnitaleUtil.GetCurrentOverworldAudio();
        MapInfos mi = GameObject.FindObjectOfType<MapInfos>();

        try {
            //Starts the music if there's no music
            if (audio.clip == null) {
                if (GlobalControls.Music != null) {
                    audio.clip = GlobalControls.Music;
                    audio.Play();
                } else if (mi.music != "none") {
                    audio.clip = AudioClipRegistry.GetMusic(mi.music);
                    audio.Play();
                } else
                    audio.Stop();
                //Starts the music if the music we added is different of the one that is playing right now
            } else if (!FileLoader.getRelativePathWithoutExtension(audio.clip.name).Replace("Audio\\", string.Empty).Equals(mi.music))
                audio.Play();
        } catch { UnitaleUtil.displayLuaError("Overworld System", "Start of PlayerOverworld: music error!\nThis bug may have happened because the current mod folder doesn't exist, or because the map's music doesn't exist."); }
    }

    //Moves the player
    public void Move(float xDir, float yDir, GameObject go) {
        Transform transform = go.transform;
        Rigidbody2D rb2Dgo = go.GetComponent<Rigidbody2D>();

        //Creates the movement of our object
        Vector2 end;
        if (go == rb2D.gameObject)  end = (Vector2)transform.position + new Vector2(xDir * speed, yDir * speed);
        else                        end = (Vector2)transform.position + new Vector2(xDir * go.GetComponent<EventOW>().moveSpeed, yDir * go.GetComponent<EventOW>().moveSpeed);

        //Creates the new position of our object, depending on our current position
        Vector2 newPosition = Vector2.MoveTowards(rb2Dgo.position, end, Mathf.Infinity);

        //If the GameObject is the player, check if the camera can follow him or not
        if (go == gameObject && !inBattleAnim && GameObject.Find("Background") != null) {
            RectifyCameraPosition(new Vector2(newPosition.x, newPosition.y + PlayerPos.GetComponent<SpriteRenderer>().sprite.texture.height / 2f));
            GameObject.Find("Canvas OW").transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, -10);
        }

        //Moves the GameObject to the position we created before
        rb2Dgo.MovePosition(newPosition);

        //Decrease battleWalkCount if the player is moving freely
        if ((xDir != 0 || yDir != 0) && !playerNoMove && EventManager.instance.script == null && go == gameObject)
            battleWalkCount--;
    }

    //testMove returns true if it is able to move and false if not.
    //testMove takes parameters for x direction, y direction and a RaycastHit2D to check collision
    public bool testMove(float xDir, float yDir, out RaycastHit2D hit, GameObject go) {
        Transform transform = go.transform;
        BoxCollider2D boxCollider = go.GetComponent<BoxCollider2D>();

        //Store start position to move from, based on objects current transform position
        Vector2 start = new Vector2(transform.position.x + transform.localScale.x * boxCollider.offset.x,
                                    transform.position.y + transform.localScale.x * boxCollider.offset.y);

        //Calculate end position based on the direction parameters passed in when calling Move and using our boxCollider
        Vector2 dir = new Vector2(xDir, yDir);

        //Calculate the current size of the object's boxCollider
        Vector2 size = new Vector2(boxCollider.size.x * transform.localScale.x, boxCollider.size.y * transform.localScale.y);

        //Disable the boxCollider so that linecast doesn't hit this object's own collider
        boxCollider.enabled = false;

        //Cast a line from start point to end point checking collision on blockingLayer and then EventLayer
        if (go == rb2D.gameObject) {
            hit = Physics2D.BoxCast(start, size, 0, dir, Mathf.Sqrt(Mathf.Pow(xDir * speed, 2) + Mathf.Pow(yDir * speed, 2)), blockingLayer);
            if (hit.transform == null)
                hit = Physics2D.BoxCast(start, size, 0, dir, Mathf.Sqrt(Mathf.Pow(xDir * speed, 2) + Mathf.Pow(yDir * speed, 2)), EventLayer);
        } else {
            hit = Physics2D.BoxCast(start, size, 0, dir, Mathf.Sqrt(Mathf.Pow(xDir * go.GetComponent<EventOW>().moveSpeed, 2) + Mathf.Pow(yDir * go.GetComponent<EventOW>().moveSpeed, 2)), blockingLayer);
            if (hit.transform == null)
                hit = Physics2D.BoxCast(start, size, 0, dir, Mathf.Sqrt(Mathf.Pow(xDir * go.GetComponent<EventOW>().moveSpeed, 2) + Mathf.Pow(yDir * go.GetComponent<EventOW>().moveSpeed, 2)), EventLayer);
        }

        //Re-enable boxCollider after BoxCast
        boxCollider.enabled = true;

        //Check if anything was hit
        if (hit.transform == null)
            return true;
        //If something was hit, return false, Move was unsuccesful
        else
            return false;
    }

    public bool AttemptMove(float xDir, float yDir, GameObject go = null, bool wallPass = false) {
        if (go == null)
            go = gameObject;

        //If there's an input, register the last input
        if (!(xDir == 0 && yDir == 0) && go == gameObject)
            lastMove = new Vector2(xDir, yDir);

        //Hit will store whatever our linecast hits when Move is called
        RaycastHit2D hit;

        //Set canMove to true if Move was successful, false if failed
        bool canMove = testMove(xDir, yDir, out hit, go);

        //Check if nothing was hit by BoxCast
        //If nothing was hit, move normally
        if (hit.transform == null || wallPass)
            Move(xDir, yDir, go);
        else {
            //If we can go on sides
            if (testMove(xDir, 0, out hit, go))
                Move(xDir, 0, go);
            //if we can go up or down
            else if (testMove(0, yDir, out hit, go))
                Move(0, yDir, go);
            else
                Move(0, 0, go);
        }

        //If we moved enough to set battleWalkCount to 0...
        if (battleWalkCount == 0)
            if (!GameObject.Find("Background").GetComponent<MapInfos>().noRandomEncounter) {
                battleWalkCount = -1;

                //...let's set an encounter!
                SetEncounterAnim();
            } else
                battleWalkCount = Math.randomRange(300, 1000);

        return canMove;
    }

    //Restart reloads the scene when called
    private void Restart() {
        EventManager.SetEventStates();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// The encounter anim, that ends to a battle
    /// </summary>
    /// <param name="encounterName">The name of the encounter. If not set, the encounter will be random.</param>
    public void SetEncounterAnim(string encounterName = null, bool quickAnim = false, bool ForceNoFlee = false) { StartCoroutine(AnimationBeforeBattle(encounterName, quickAnim, ForceNoFlee)); }

    //The function that creates the animation before the encounter
    IEnumerator AnimationBeforeBattle(string encounterName = null, bool quickAnim = false, bool ForceNoFlee = false) {
        playerNoMove = true; //Begin encounter
        inBattleAnim = true;
        //Here are the player's soul and a black sprite, we'll need to make them go up
        Image utHeart = GameObject.Find("utHeart").GetComponent<Image>();
        Image playerMask = GameObject.Find("PlayerEncounter").GetComponent<Image>();
        Image blackFont = GameObject.Find("black").GetComponent<Image>();

        Vector2 positionCamera, end;
        GlobalControls.Music = UnitaleUtil.GetCurrentOverworldAudio().clip;
        playerMask.GetComponent<Image>().sprite = PlayerPos.GetComponent<SpriteRenderer>().sprite;
        Camera.main.GetComponent<AudioSource>().Stop();

        blackFont.transform.SetAsLastSibling();
        playerMask.transform.SetAsLastSibling();
        utHeart.transform.SetAsLastSibling();

        //If you want a quick animation, we just keep the end of the anim
        if (!quickAnim) {
            uiAudio.PlayOneShot(AudioClipRegistry.GetSound("BeginBattle1"));

            //Shows the encounter bubble, the "!" on the player
            SpriteRenderer EncounterBubble = GameObject.Find("EncounterBubble").GetComponent<SpriteRenderer>();
            EncounterBubble.color = new Color(EncounterBubble.color.r, EncounterBubble.color.g, EncounterBubble.color.b, 1f);
            EncounterBubble.transform.position = new Vector3(EncounterBubble.transform.position.x, PlayerPos.position.y + playerMask.sprite.texture.height + EncounterBubble.sprite.texture.height);

            yield return new WaitForSeconds(0.5f);
        }
        //Set the heart's position to the player's position
        utHeart.transform.position = new Vector3(PlayerPos.position.x, PlayerPos.position.y, -5100);
        positionCamera = Camera.main.transform.position;
        end = new Vector2(PlayerPos.position.x - (positionCamera.x - 320 + 48), PlayerPos.position.y - (positionCamera.y - 240 + 25));
        blackFont.transform.position = new Vector3(positionCamera.x, positionCamera.y, blackFont.transform.position.z);
        blackFont.color = new Color(blackFont.color.r, blackFont.color.g, blackFont.color.b, 1f);
        playerMask.transform.position = new Vector3(PlayerPos.position.x, PlayerPos.transform.position.y, -5040);
        playerMask.sprite = PlayerPos.GetComponent<SpriteRenderer>().sprite;
        playerMask.rectTransform.sizeDelta = PlayerPos.GetComponent<RectTransform>().sizeDelta;
        //playerMask.transform.localScale = new Vector3(PlayerPos.lossyScale.x / playerMask.transform.lossyScale.x, PlayerPos.lossyScale.y / playerMask.transform.lossyScale.y, 1);
        Color color = PlayerPos.GetComponent<SpriteRenderer>().color;
        PlayerPos.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 0);
        playerMask.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 1);

        for (int i = 0; i < 2; i++) {
            utHeart.color = new Color(utHeart.color.r, utHeart.color.g, utHeart.color.b, 1f);
            uiAudio.PlayOneShot(AudioClipRegistry.GetSound("BeginBattle2"));
            yield return new WaitForSeconds(0.075f);

            utHeart.color = new Color(utHeart.color.r, utHeart.color.g, utHeart.color.b, 0f);
            yield return new WaitForSeconds(0.075f);
        }

        playerMask.color = new Color(color.r, color.g, color.b, 0);
        utHeart.color = new Color(utHeart.color.r, utHeart.color.g, utHeart.color.b, 1f);
        blackFont.color = new Color(blackFont.color.r, blackFont.color.g, blackFont.color.b, 1f);

        //-----------------------------------------------

        uiAudio.PlayOneShot(AudioClipRegistry.GetSound("BeginBattle3"));

        TimeIndicator += Time.deltaTime;

        if (TimeIndicator > 1)
            TimeIndicator = 1;

        //Here we move the heart to the place it'll be on the beginning of the battle
        if (utHeart.transform.position != new Vector3(positionCamera.x - 320 + 48, positionCamera.y - 240 + 25, -5100)) {
            Vector3 positionTemp = new Vector3(PlayerPos.position.x - (end.x * TimeIndicator), PlayerPos.position.y - (end.y * TimeIndicator), -5100);
            utHeart.transform.position = positionTemp;
        }

        yield return new WaitForSeconds(1f);

        //Set the heart's position
        Vector3 positionTemp3 = new Vector3(positionCamera.x - 320 + 48, positionCamera.y - 240 + 25, -5100f);
        utHeart.transform.position = positionTemp3;

        //Launch the battle
        StartCoroutine(SetEncounter(encounterName, ForceNoFlee));
    }

    //The function that is used to launch a battle
    private IEnumerator SetEncounter(string encounterName = null, bool ForceNoFlee = false) {
        //Saves our last map and the position of our player, before the battle
        string mapName;
        if (UnitaleUtil.MapCorrespondanceList.ContainsKey(SceneManager.GetActiveScene().name))  mapName = UnitaleUtil.MapCorrespondanceList[SceneManager.GetActiveScene().name];
        else                                                                                    mapName = SceneManager.GetActiveScene().name;
        LuaScriptBinder.Set(null, "PlayerMap", DynValue.NewString(mapName));
        LuaScriptBinder.Set(null, "PlayerPosX", DynValue.NewNumber(rb2D.position.x));
        LuaScriptBinder.Set(null, "PlayerPosY", DynValue.NewNumber(rb2D.position.y));

        //Sets the mod's folder and the encounter file's name to know what file we have to load
        string ModFolder = StaticInits.MODFOLDER, Encounter;
        LuaScriptBinder.Set(null, "ModFolder", DynValue.NewString(ModFolder));
        if (ForceNoFlee)
            LuaScriptBinder.Set(null, "ForceNoFlee", DynValue.NewBoolean(ForceNoFlee));

        DirectoryInfo di = new DirectoryInfo(Path.Combine(FileLoader.DataRoot, "Mods/" + StaticInits.MODFOLDER + "/Lua/Encounters"));
        FileInfo[] encounterFiles = di.GetFiles();

        if (encounterName == null) {
            ArrayList encounterNames = new ArrayList();
            foreach (FileInfo encounterFile in encounterFiles) {
                if (!encounterFile.Name.EndsWith(".lua") || encounterFile.Name[0] == '#')
                    continue;
                encounterNames.Add(Path.GetFileNameWithoutExtension(encounterFile.Name));
            }
            if (encounterNames.Count == 0) {
                UnitaleUtil.displayLuaError("Overworld System", "There's no valid encounter to launch.\nYou need to have at least 1 encounter\nthat doesn't have a '#' for first character!");
                yield break;
            } else {
                if (encounterNames.Count == 1)
                    Encounter = Path.GetFileNameWithoutExtension(encounterNames[0].ToString());
                else
                    Encounter = Path.GetFileNameWithoutExtension(encounterNames[Math.randomRange(0, encounterNames.Count)].ToString());
            }
        } else
            Encounter = Path.GetFileNameWithoutExtension(encounterName);

        //Let's set the folder and file we want to load.
        StaticInits.MODFOLDER = ModFolder;
        StaticInits.ENCOUNTER = Encounter;

        //We save the state of the events.
        EventManager.SetEventStates();

        LuaScriptBinder.ClearBattleVar();
        
        yield return new WaitForEndOfFrame();
        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        GlobalControls.texBeforeEncounter = tex;

        //Now, we load our battle.
        GameObject.Find("Main Camera OW").tag = "Untagged";
        SceneManager.LoadScene("Battle");
    }

    /// <summary>
    /// Permits to set a dialogue (old method).
    /// </summary>
    /// <param name="textTable">The text that'll be printed</param>
    /// <param name="rearranged">Will the text be rearranged ? (\r replacements etc)</param>
    /// <param name="mugshots">The mugshots' name that'll be used in the dialogue</param>
    public void SetDialog(string[] textTable, bool rearranged, string[] mugshots = null) {
        if (textTable[0] == string.Empty) {
            UnitaleUtil.writeInLogAndDebugger("Old SetDialog: There is no text to print!");
            return;
        }

        TextMessage[] textmsg = new TextMessage[textTable.Length];

        if (mugshots != null)
            for (int i = 0; i < textTable.Length; i++)
                textmsg[i] = new TextMessage(textTable[i], rearranged, false, mugshots[i]);
        else
            for (int i = 0; i < textTable.Length; i++)
                textmsg[i] = new TextMessage(textTable[i], rearranged, false);
        playerNoMove = true; //Old SetDialog
        EventManager.instance.passPressOnce = true;

        textmgr.setTextFrameAlpha(1);
        textmgr.blockSkip = false;

        //textmgr.setTextQueue(textmsg, mugshots);
        textmgr.setTextQueue(textmsg);
        textmgr.transform.parent.parent.SetAsLastSibling();
    }

    /// <summary>
    /// Rectifies the position of the camera, to be sure that the background will always be on tha camera.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public void RectifyCameraPosition(Vector3 pos) {
        Vector3 dimBG = new Vector3(640, 480, -10000);
        try { dimBG = GameObject.Find("Background").GetComponent<RectTransform>().sizeDelta * GameObject.Find("Background").GetComponent<RectTransform>().localScale.x; } 
        catch { UnitaleUtil.writeInLogAndDebugger("RectifyCameraPosition: The 'Background' GameObject is missing."); }

        if (pos.x < 320)                 pos.x = 320;
        else if (pos.x > dimBG.x - 320)  pos.x = Mathf.RoundToInt(dimBG.x - 320);
        if (pos.y < 240)                 pos.y = 240;
        else if (pos.y > dimBG.y - 240)  pos.y = Mathf.RoundToInt(dimBG.y - 240);

        pos += (Vector3)cameraShift;
        pos.z = -10000;
        Camera.main.transform.position = pos;

        foreach (Transform t in UnitaleUtil.GetFirstChildren(null))
            if (t.gameObject.name.Contains("Parallax")) {
                Vector3 dimPlx = t.GetComponent<RectTransform>().sizeDelta * t.localScale.x;
                t.position = new Vector3(Mathf.Round(dimPlx.x) > 640 ? (dimPlx.x / 2 + (dimBG.x - dimPlx.x) * ((pos.x - 320) / (dimBG.x - 640))) : t.position.x,
                                         Mathf.Round(dimPlx.y) > 480 ? (dimPlx.y / 2 + (dimBG.y - dimPlx.y) * ((pos.y - 240) / (dimBG.y - 480))) : t.position.y, t.position.z);
            }
    }

    public static IEnumerator LaunchMenu() {
        playerNoMove = true; menuRunning[2] = true; //Launch menu
        GameObject.Find("MenuContainer").transform.SetAsLastSibling();
        TextManager[] txtmgrs = GameObject.Find("MenuContainer").GetComponentsInChildren<TextManager>();
        instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menumove"));
        /* 0-6   : Menu
           7-17  : Item
           18-27 : Stat */
        foreach (TextManager txt in txtmgrs)
            txt.setHorizontalSpacing(2);

        GameObject.Find("TextManager OW").GetComponent<TextManager>().setText(new TextMessage("[noskipatall]", false, false));
        GameObject.Find("menustat_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 1);
        GameObject.Find("menuchoice_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 1);
        GameObject.Find("menustat_interior").GetComponent<Image>().color = new Color(0, 0, 0, 1);
        GameObject.Find("menuchoice_interior").GetComponent<Image>().color = new Color(0, 0, 0, 1);

        txtmgrs[0].setText(new TextMessage("[noskipatall]" + PlayerCharacter.instance.Name, false, true));
        if (GlobalControls.crate) {
            txtmgrs[1].setText(new TextMessage("[noskipatall][font:menu]LV " + PlayerCharacter.instance.LV, false, true));
            txtmgrs[2].setText(new TextMessage("[noskipatall][font:menu]PH " + (int)PlayerCharacter.instance.HP + "/" + PlayerCharacter.instance.MaxHP, false, true));
            txtmgrs[3].setText(new TextMessage("[noskipatall][font:menu]G  " + PlayerCharacter.instance.Gold, false, true));
            txtmgrs[4].setText(new TextMessage("[noskipatall]TEM", false, true));
            txtmgrs[5].setText(new TextMessage("[noskipatall]TAST", false, true));
            txtmgrs[6].setText(new TextMessage("[noskipatall]LECL", false, true));
        } else {
            txtmgrs[1].setText(new TextMessage("[noskipatall][font:menu]LV " + PlayerCharacter.instance.LV, false, true));
            txtmgrs[2].setText(new TextMessage("[noskipatall][font:menu]HP " + (int)PlayerCharacter.instance.HP + "/" + PlayerCharacter.instance.MaxHP, false, true));
            txtmgrs[3].setText(new TextMessage("[noskipatall][font:menu]G  " + PlayerCharacter.instance.Gold, false, true));
            txtmgrs[4].setText(new TextMessage("[noskipatall]ITEM", false, true));
            txtmgrs[5].setText(new TextMessage("[noskipatall]STAT", false, true));
            txtmgrs[6].setText(new TextMessage("[noskipatall]CELL", false, true));
        }
        GameObject.Find("Mugshot").GetComponent<Image>().color = new Color(1, 1, 1, 0);
        GameObject.Find("textframe_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
        GameObject.Find("textframe_interior").GetComponent<Image>().color = new Color(0, 0, 0, 0);
        Color c = GameObject.Find("utHeartMenu").GetComponent<Image>().color;
        GameObject.Find("utHeartMenu").GetComponent<Image>().color = new Color(c.r, c.g, c.b, 1);
        GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-255, 35, GameObject.Find("utHeartMenu").transform.position.z);
        int choice = 2;
        yield return 0;
        while (!menuRunning[3]) {
            if (!menuRunning[0]) {
                if (GlobalControls.input.Up == UndertaleInput.ButtonState.PRESSED) {
                    choice = (choice + 1) % 3;
                    GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-255, 35 - ((2 - choice % 3) * 36), GameObject.Find("utHeartMenu").transform.position.z);
                } else if (GlobalControls.input.Down == UndertaleInput.ButtonState.PRESSED) {
                    choice = (choice + 2) % 3;
                    GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-255, 35 - ((2 - choice % 3) * 36), GameObject.Find("utHeartMenu").transform.position.z);
                } else if (GlobalControls.input.Confirm == UndertaleInput.ButtonState.PRESSED) {
                    instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menuconfirm"));
                    menuRunning[0] = true;
                    if (choice == 2) { //ITEM
                        int invCount = Inventory.container.Count;
                        if (invCount == 0) {
                            menuRunning[0] = false;
                            //yield break;
                        } else {
                            for (int i = 0; i != invCount; i++)
                                txtmgrs[i + 7].setText(new TextMessage("[noskipatall]" + Inventory.container[i].Name, false, true));
                            if (GlobalControls.crate) {
                                txtmgrs[15].setText(new TextMessage("[noskipatall]SUE", false, true));
                                txtmgrs[16].setText(new TextMessage("[noskipatall]FINO", false, true));
                                txtmgrs[17].setText(new TextMessage("[noskipatall]DORP", false, true));
                            } else {
                                txtmgrs[15].setText(new TextMessage("[noskipatall]USE", false, true));
                                txtmgrs[16].setText(new TextMessage("[noskipatall]INFO", false, true));
                                txtmgrs[17].setText(new TextMessage("[noskipatall]DROP", false, true));
                            }
                            GameObject.Find("Mugshot").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                            GameObject.Find("textframe_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                            GameObject.Find("textframe_interior").GetComponent<Image>().color = new Color(0, 0, 0, 0);
                            GameObject.Find("item_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 1);
                            GameObject.Find("item_interior").GetComponent<Image>().color = new Color(0, 0, 0, 1);
                            GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-48, 143, GameObject.Find("utHeartMenu").transform.position.z);
                            int index = 0;
                            yield return 0;
                            while (menuRunning[0] &&!menuRunning[1] &&!menuRunning[3]) {
                                if (GlobalControls.input.Down == UndertaleInput.ButtonState.PRESSED) {
                                    index = (index + 1) % invCount;
                                    instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menumove"));
                                    GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-48, 143 - 32 * index, GameObject.Find("utHeartMenu").transform.position.z);
                                } else if (GlobalControls.input.Up == UndertaleInput.ButtonState.PRESSED) {
                                    index = (index + invCount - 1) % invCount;
                                    instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menumove"));
                                    GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-48, 143 - 32 * index, GameObject.Find("utHeartMenu").transform.position.z);
                                } else if (GlobalControls.input.Cancel == UndertaleInput.ButtonState.PRESSED) {
                                    menuRunning[0] = false;
                                    for (int i = 7; i <= 17; i++) txtmgrs[i].destroyText();
                                    GameObject.Find("Mugshot").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                                    GameObject.Find("textframe_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                                    GameObject.Find("textframe_interior").GetComponent<Image>().color = new Color(0, 0, 0, 0);
                                    GameObject.Find("item_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                                    GameObject.Find("item_interior").GetComponent<Image>().color = new Color(0, 0, 0, 0);
                                    GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-255, 35 - ((2 - choice % 3) * 36), GameObject.Find("utHeartMenu").transform.position.z);
                                } else if (GlobalControls.input.Confirm == UndertaleInput.ButtonState.PRESSED) {
                                    menuRunning[1] = true;
                                    instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menuconfirm"));
                                    int index2 = 0;
                                    GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-48, -137, GameObject.Find("utHeartMenu").transform.position.z); // -53,42,156
                                    yield return 0;
                                    while (menuRunning[1] &&!menuRunning[3]) {
                                        if (GlobalControls.input.Left == UndertaleInput.ButtonState.PRESSED) {
                                            instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menumove"));
                                            index2 = (index2 + 2) % 3;
                                            switch (index2) {
                                                case 0: GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-48, -137, GameObject.Find("utHeartMenu").transform.position.z); break;
                                                case 1: GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(47, -137, GameObject.Find("utHeartMenu").transform.position.z); break;
                                                case 2: GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(161, -137, GameObject.Find("utHeartMenu").transform.position.z); break;
                                            }
                                        } else if (GlobalControls.input.Right == UndertaleInput.ButtonState.PRESSED) {
                                            instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menumove"));
                                            index2 = (index2 + 1) % 3;
                                            switch (index2) {
                                                case 0: GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-48, -137, GameObject.Find("utHeartMenu").transform.position.z); break;
                                                case 1: GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(47, -137, GameObject.Find("utHeartMenu").transform.position.z); break;
                                                case 2: GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(161, -137, GameObject.Find("utHeartMenu").transform.position.z); break;
                                            }
                                        } else if (GlobalControls.input.Cancel == UndertaleInput.ButtonState.PRESSED) {
                                            GameObject.Find("utHeartMenu").transform.localPosition = new Vector3(-48, 143 - 32 * index, GameObject.Find("utHeartMenu").transform.position.z);
                                            menuRunning[1] = false;
                                        } else if (GlobalControls.input.Confirm == UndertaleInput.ButtonState.PRESSED) {
                                            instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menuconfirm"));
                                            yield return CloseMenu();
                                            switch (index2) {
                                                case 0: Inventory.UseItem(index); break;
                                                case 1:
                                                    string str;
                                                    Inventory.NametoDesc.TryGetValue(Inventory.container[index].Name, out str);
                                                    instance.textmgr.setText(new TextMessage("\"" + Inventory.container[index].Name + "\"\n" + str, true, false));
                                                    instance.textmgr.transform.parent.parent.SetAsLastSibling();
                                                    break;
                                                case 2:
                                                    if (GlobalControls.crate)
                                                        instance.textmgr.setText(new TextMessage("U DORPED TEH " + Inventory.container[index].Name + "!!!!!", true, false));
                                                    else
                                                        instance.textmgr.setText(new TextMessage("You dropped the " + Inventory.container[index].Name + ".", true, false));
                                                    instance.textmgr.transform.parent.parent.SetAsLastSibling();
                                                    Inventory.RemoveItem(index);
                                                    break;
                                            }
                                        }
                                        yield return 0;
                                    }
                                }
                                yield return 0;
                            }
                        }
                    } else if (choice == 1) { // STAT
                        txtmgrs[18].setText(new TextMessage("[noskipatall]\"" + PlayerCharacter.instance.Name + "\"", false, true));
                        txtmgrs[19].setText(new TextMessage("[noskipatall]LV " + PlayerCharacter.instance.LV, false, true));
                        txtmgrs[20].setText(new TextMessage("[noskipatall]HP " + PlayerCharacter.instance.HP + "/" + PlayerCharacter.instance.MaxHP, false, true));
                        if (GlobalControls.crate) {
                            txtmgrs[21].setText(new TextMessage("[noskipatall]TA " + (PlayerCharacter.instance.ATK + PlayerCharacter.instance.WeaponATK) + " (" + PlayerCharacter.instance.WeaponATK + ")", false, true));
                            txtmgrs[22].setText(new TextMessage("[noskipatall]DF " + (PlayerCharacter.instance.DEF + PlayerCharacter.instance.ArmorDEF) + " (" + PlayerCharacter.instance.ArmorDEF + ")", false, true));
                            txtmgrs[23].setText(new TextMessage("[noskipatall]EPX: " + PlayerCharacter.instance.EXP, false, true));
                            txtmgrs[24].setText(new TextMessage("[noskipatall]NETX: " + PlayerCharacter.instance.GetNext(), false, true));
                            txtmgrs[25].setText(new TextMessage("[noskipatall]WAEPON: " + PlayerCharacter.instance.Weapon, false, true));
                            txtmgrs[26].setText(new TextMessage("[noskipatall]AROMR: " + PlayerCharacter.instance.Armor, false, true));
                            txtmgrs[27].setText(new TextMessage("[noskipatall]GLOD: " + PlayerCharacter.instance.Gold, false, true));
                        } else {
                            txtmgrs[21].setText(new TextMessage("[noskipatall]AT " + (PlayerCharacter.instance.ATK + PlayerCharacter.instance.WeaponATK) + " (" + PlayerCharacter.instance.WeaponATK + ")", false, true));
                            txtmgrs[22].setText(new TextMessage("[noskipatall]DF " + (PlayerCharacter.instance.DEF + PlayerCharacter.instance.ArmorDEF) + " (" + PlayerCharacter.instance.ArmorDEF + ")", false, true));
                            txtmgrs[23].setText(new TextMessage("[noskipatall]EXP: " + PlayerCharacter.instance.EXP, false, true));
                            txtmgrs[24].setText(new TextMessage("[noskipatall]NEXT: " + PlayerCharacter.instance.GetNext(), false, true));
                            txtmgrs[25].setText(new TextMessage("[noskipatall]WEAPON: " + PlayerCharacter.instance.Weapon, false, true));
                            txtmgrs[26].setText(new TextMessage("[noskipatall]ARMOR: " + PlayerCharacter.instance.Armor, false, true));
                            txtmgrs[27].setText(new TextMessage("[noskipatall]GOLD: " + PlayerCharacter.instance.Gold, false, true));
                        }
                        GameObject.Find("Mugshot").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                        GameObject.Find("textframe_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                        GameObject.Find("textframe_interior").GetComponent<Image>().color = new Color(0, 0, 0, 0);
                        GameObject.Find("stat_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 1);
                        GameObject.Find("stat_interior").GetComponent<Image>().color = new Color(0, 0, 0, 1);
                        yield return 0;
                        while (menuRunning[0] &&!menuRunning[3]) {
                            if (GlobalControls.input.Cancel == UndertaleInput.ButtonState.PRESSED || GlobalControls.input.Confirm == UndertaleInput.ButtonState.PRESSED) {
                                instance.uiAudio.PlayOneShot(AudioClipRegistry.GetSound("menuconfirm"));
                                menuRunning[0] = false;
                                for (int i = 18; i <= 27; i++) txtmgrs[i].destroyText();
                                GameObject.Find("Mugshot").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                                GameObject.Find("textframe_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                                GameObject.Find("textframe_interior").GetComponent<Image>().color = new Color(0, 0, 0, 0);
                                GameObject.Find("stat_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                                GameObject.Find("stat_interior").GetComponent<Image>().color = new Color(0, 0, 0, 0);
                            }
                            yield return 0;
                        }
                    } else { //CELL
                        yield return CloseMenu();
                        if (GlobalControls.crate)
                            instance.textmgr.setText(new TextMessage("NO CELPLHONE ALOLWDE!!!", true, false));
                        else
                            instance.textmgr.setText(new TextMessage("But you don't have a cellphone...[w:10]yet.", true, false));
                        instance.textmgr.transform.parent.parent.SetAsLastSibling();
                    }
                } else if (GlobalControls.input.Cancel == UndertaleInput.ButtonState.PRESSED)
                    yield return CloseMenu(true);
            }
            yield return 0;
        }
        while (playerNoMove) 
            yield return 0;
        menuRunning[3] = false;
    }

    private static bool CloseMenu(bool endOfinText = false) {
        foreach (Transform tf in GameObject.Find("MenuContainer").GetComponentsInChildren<Transform>()) {
            if (tf.GetComponent<Image>())       tf.gameObject.GetComponent<Image>().color = new Color(tf.gameObject.GetComponent<Image>().color.a,
                                                                                                      tf.gameObject.GetComponent<Image>().color.b,
                                                                                                      tf.gameObject.GetComponent<Image>().color.g, 0);
            if (tf.GetComponent<TextManager>()) tf.gameObject.GetComponent<TextManager>().destroyText();
        }
        menuRunning = new bool[] { false, false, false, true };
        GameObject.Find("Mugshot").GetComponent<Image>().color = new Color(1, 1, 1, 0);
        GameObject.Find("textframe_border_outer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
        GameObject.Find("textframe_interior").GetComponent<Image>().color = new Color(0, 0, 0, 0);
        GameObject.Find("TextManager OW").GetComponent<TextManager>().skipNowIfBlocked = true;
        if (endOfinText)
            playerNoMove = false; //Close menu
        return true;
    }
}


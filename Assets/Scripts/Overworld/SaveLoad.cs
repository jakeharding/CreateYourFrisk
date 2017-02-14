﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

/// <summary>
/// A static class that is used to load and save a gamestate.
/// </summary>
public static class SaveLoad {
    public static GameState currentGame = null;                   //The current save
    public static GameState savedGame = null;                     //The real save
    public static AlMightyGameState almightycurrentGame = null;   //The almighty save
    
    public static void Start() {
        try {
            if (File.Exists(Application.persistentDataPath + "/save.gd")) {
                UnitaleUtil.writeInLog("We found a save at this location : " + Application.persistentDataPath + "/save.gd");
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(Application.persistentDataPath + "/save.gd", FileMode.Open);
                savedGame = (GameState)bf.Deserialize(file);
                file.Close();
            } else {
                UnitaleUtil.writeInLog("There's no save at all.");
            }
        } catch {
            UnitaleUtil.displayLuaError(StaticInits.ENCOUNTER, "Have you saved on one of a previous CYF version ? The save isn't retrocompatible.\n\n"
           + "To not have this error anymore, you'll have to delete the save file. Here it is : \n"
           + Application.persistentDataPath + "/save.gd\n"
           + "Tell me if you have some more problems, and thanks for following my fork ! ^^\n\n"
           + "PS : Don't try to press ESCAPE, or bad things can happen ;)");
        }
    }

    public static void Save() {
        currentGame = new GameState();
        currentGame.SaveGameVariables();
        BinaryFormatter bf = new BinaryFormatter();
        //Application.persistentDataPath is a string, so if you wanted you can put that into unitaleutil.writeinlog if you want to know where save games are located
        FileStream file;
        file = File.Create(Application.persistentDataPath + "/save.gd");
        bf.Serialize(file, currentGame);
        savedGame = currentGame;
        UnitaleUtil.writeInLog("Save created at this location : " + Application.persistentDataPath + "/save.gd");
        file.Close();
    }

    public static bool Load() {
        if (File.Exists(Application.persistentDataPath + "/save.gd")) {
            UnitaleUtil.writeInLog("We found a save at this location : " + Application.persistentDataPath + "/save.gd");
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/save.gd", FileMode.Open);
            currentGame = (GameState)bf.Deserialize(file);
            currentGame.LoadGameVariables();
            file.Close();
            return true;
        } else {
            UnitaleUtil.writeInLog("There's no save to load.");
            return false;
        }
    }

    public static void SaveAlMighty() {
        almightycurrentGame = new AlMightyGameState();
        almightycurrentGame.UpdateVariables();
        File.Delete(Application.persistentDataPath + "/AlMightySave.gd");
        BinaryFormatter bf = new BinaryFormatter();
        //Application.persistentDataPath is a string, so if you wanted you can put that into unitaleutil.writeinlog if you want to know where save games are located
        FileStream file;
        file = File.Create(Application.persistentDataPath + "/AlMightySave.gd");
        bf.Serialize(file, almightycurrentGame);
        UnitaleUtil.writeInLog("AlMighty Save created at this location : " + Application.persistentDataPath + "/AlMightySave.gd");
        file.Close();
    }

    public static bool LoadAlMighty() {
        if (File.Exists(Application.persistentDataPath + "/AlMightySave.gd")) {
            UnitaleUtil.writeInLog("We found an almighty save at this location : " + Application.persistentDataPath + "/AlMightySave.gd");
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/AlMightySave.gd", FileMode.Open);
            almightycurrentGame = (AlMightyGameState)bf.Deserialize(file);
            almightycurrentGame.LoadVariables();
            file.Close();
            return true;
        } else {
            UnitaleUtil.writeInLog("There's no almighty save to load.");
            return false;
        }
    }
}
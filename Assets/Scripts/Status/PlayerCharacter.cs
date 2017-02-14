﻿using UnityEngine;
using System.ComponentModel;

[System.Serializable]public class PlayerCharacter {
    public static PlayerCharacter instance;
    public int LV = 1;
    public float HP {
        get { return _HP; }
        set { _HP = UnitaleUtil.CropDecimal(value); }
    }
    public float _HP = 20;
    private int realbasishp = 20;
    public int BasisMaxHP {
        get { return realbasishp; }
        set {
            realbasishp = value;
            MaxHP = BasisMaxHP + MaxHPShift;
        }
    }
    private int realmhps = 0;
    public int MaxHPShift {
        get { return realmhps; }
        set {
            realmhps = value;
            MaxHP = BasisMaxHP + MaxHPShift;
        }
    }
    public int MaxHP = 20;
    public string Name = ControlPanel.instance.BasisName;
    public int WeaponATK = 0;
    public int ArmorDEF = 0;
    public int ATK = 10;      // internally, ATK is what Undertale's menu shows + 10
    public int DEF = 10;      // not unused anymore!
    public int EXP = 0;
    public int Gold = 0;
    public string Weapon = "Stick";
    public string Armor = "Bandage";
    private int[] LevelUpTable = new int[] { 10, 30, 70, 120, 200, 300, 500, 800, 1200, 1700, 2500, 3500, 5000, 7000, 10000, 15000, 25000, 50000, 99999 };

    /*private string[] names = new string[]{
        "Chara",  "Dai",    "Dog",   "MTT",    "Papyru",
        "Bones",  "Gira",   "Ophie", "Rhenao", "Neos",
        "Tokeur", "Mehry",  "Yuri",  "Lukark", "Void",
        "Daku",   "Shu",    "Palb",  "Aerun",  "Ram",
        "UUU",    "Schnei", "Hoot",  "Kali",   "Sanga",
        "Joeyw",  "Aye",    "d0p1",  "B-Box",  "Eddy",
        "Touza",  "Redder", "Eliya", "Qwerty", "Elogio",
        "Sanso"
    };*/

    public PlayerCharacter() {
        instance = this;
    }

    public void Reset() {
        Name = ControlPanel.instance.BasisName;
        SetLevel(1);
        EXP = 0;
        Gold = 0;
        Weapon = "Stick";
        Armor = "Bandage";
        WeaponATK = 0;
        ArmorDEF = 0;
        HP = MaxHP;
        MusicManager.SetSoundDictionary("RESETDICTIONARY", "");
    }

    public int GetNext() {
        for (int i = 0; i < 20; i++)
            if (EXP < LevelUpTable[i]) {
                return LevelUpTable[i] - EXP;
            }
        return 0;
    }

    public bool AddBattleResults(int exp, int gold) {
        EXP = EXP + exp;
        if (EXP > ControlPanel.instance.EXPLimit)
            EXP = ControlPanel.instance.EXPLimit;
        Gold = Gold + gold;
        if (Gold > ControlPanel.instance.GoldLimit)
            Gold = ControlPanel.instance.GoldLimit;
        bool levelup = checkLevel();
        return levelup;
    }

    public bool checkLevel() {
        if (LV > 20) return false; //In case that you want to go further than LV 20, the level won't be resetted.

        for (int i = 0; i > -1; i++)
            if (EXP < LevelUpTable[i]) {
                if (LV != i + 1) {
                    //UnitaleUtil.writeInLog(i);
                    float currentHP = HP;
                    if (i + 1 < 20) {
                        MaxHP = 16 + 4 * (i + 1);
                        //HP = currentHP + 4 * (i + 1 - LV);
                    } else {
                        MaxHP = 16 + 4 * (i + 1) + 3;
                        //HP = currentHP + 4 * (i + 1 - LV) + 3;
                    }
                    if (LV > i + 1)
                        if (HP > 1.5 * MaxHP)
                            HP = (int)(1.5f * MaxHP);
                        else
                            HP = currentHP;
                    ATK = 8 + 2 * (i + 1);
                    LV = (i + 1);
                    return true;
                }
                return false;
            }
        return false;
    }

    public void SetLevel(int level) {
        if (level < 1) level = 1;
        else if (level > ControlPanel.instance.LevelLimit) level = ControlPanel.instance.LevelLimit;

        BasisMaxHP = 16 + 4 * level;
        ATK = 8 + 2 * level;
        DEF = 10 + (int)Mathf.Floor((level - 1) / 4);
        LV = level;

        if (LV >= 20)
            BasisMaxHP += 3;
        MaxHP = BasisMaxHP + MaxHPShift;
        if (MaxHP > ControlPanel.instance.HPLimit) {
            MaxHPShift = ControlPanel.instance.HPLimit - BasisMaxHP;
            MaxHP = BasisMaxHP + MaxHPShift;
        }
        if (HP > Mathf.FloorToInt(MaxHP * 1.5f))
            HP = Mathf.FloorToInt(MaxHP * 1.5f);
    }
}
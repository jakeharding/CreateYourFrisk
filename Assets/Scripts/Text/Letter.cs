﻿using System;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Letter : MonoBehaviour {
    public Vector2 basisPos = new Vector2();
    public Image img;
    //public Color currentColor = Color.white;
    public TextEffectLetter effect = null;
    public bool started = false;
    private bool goodInit = false;

    private void Start() {
        img = GetComponent<Image>();
        if (SceneManager.GetActiveScene().name == "Battle")
            if (ArenaManager.instance.firstTurn)
                LateUpdater.lateActions.Add(LateStart);
            else
                LateStart();
        else
            LateStart();
    }

    private void LateStart() {
        started = true;
        try {
            basisPos = transform.position;
            goodInit = true;
        } catch { }
    }

    private void Update() {
        if (effect != null && started) {
            if (!goodInit) {
                basisPos = transform.position;
                goodInit = true;
            }
            effect.updateEffects();
        }
    }
}
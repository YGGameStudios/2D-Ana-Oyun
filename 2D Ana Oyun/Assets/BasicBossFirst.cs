using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBossFirst : BasicBaseEnemy
{
    public Transform target;
    public TargetedShootSkill targetedShootSkill;
    public bool autoFire = true;
    public float initialDelay = 0.5f;

    public LeapChargeAoeSkill leapSkill;
    public KeyCode leapKey = KeyCode.E; // manual test
    public bool autoLeap = true;
    public float leapInterval = 8f;

    private float _startTime;
    private float _nextAiLeapTime;
    void Start()
    {
        _startTime = Time.time;
        if (targetedShootSkill == null)
        {
            targetedShootSkill = GetComponent<TargetedShootSkill>();
        }
        if (leapSkill == null)
        {
            leapSkill = GetComponent<LeapChargeAoeSkill>();
        }
    }

    void Update()
    {
        if (!autoFire || targetedShootSkill == null) return;
        if (Time.time - _startTime < initialDelay) return;
        if (target == null)
        {
            GameObject maybe = GameObject.FindWithTag("Player");
            if (maybe != null) target = maybe.transform;
        }
        if (target != null)
        {
            if (!targetedShootSkill.TryUseOn(target))
            {
                targetedShootSkill.TryUse();
            }
        }

        // Leap skill trigger: key press or automated interval (controlled here)
        if (leapSkill != null)
        {
            if (Input.GetKeyDown(leapKey))
            {
                if (target == null)
                {
                    GameObject maybe2 = GameObject.FindWithTag("Player");
                    if (maybe2 != null) target = maybe2.transform;
                }
                if (target != null) leapSkill.TryUseOn(target);
            }
            else if (autoLeap && Time.time >= _nextAiLeapTime)
            {
                if (target == null)
                {
                    GameObject maybe2 = GameObject.FindWithTag("Player");
                    if (maybe2 != null) target = maybe2.transform;
                }
                if (target != null && leapSkill.TryUseOn(target))
                {
                    _nextAiLeapTime = Time.time + leapInterval;
                }
            }
        }
    }
    
}
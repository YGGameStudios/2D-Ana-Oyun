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

    // Wall minion as a Skill (no hard reference to specific type)
    public Skill wallMinionSkill;
    public KeyCode wallMinionKey = KeyCode.Q; // manual trigger
    public bool autoWallMinion = true;
    public float wallMinionInterval = 10f;
    private float _nextWallMinionTime;

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
        if (wallMinionSkill == null)
        {
            wallMinionSkill = GetComponent("WallMinionSkill") as Skill;
        }
    }

    void Update()
    {
        if (Time.time - _startTime < initialDelay) return;
        if (target == null)
        {
            GameObject maybe = GameObject.FindWithTag("Player");
            if (maybe != null) target = maybe.transform;
        }
        if (autoFire && targetedShootSkill != null && target != null)
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

        // Wall minion skill: key or AI interval
        if (wallMinionSkill != null)
        {
            if (Input.GetKeyDown(wallMinionKey))
            {
                if (!wallMinionSkill.TryUseOn(target))
                {
                    wallMinionSkill.TryUse();
                }
            }
            else if (autoWallMinion && Time.time >= _nextWallMinionTime)
            {
                bool used = false;
                if (target != null) used = wallMinionSkill.TryUseOn(target);
                if (!used) used = wallMinionSkill.TryUse();
                if (used)
                {
                    _nextWallMinionTime = Time.time + wallMinionInterval;
                }
            }
        }
    }
    
}
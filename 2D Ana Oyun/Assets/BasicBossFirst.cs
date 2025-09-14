using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBossFirst : BasicBaseEnemy
{
    
    public Transform target;
    public Transform startPoint; // boss start/return anchor
    public TargetedShootSkill targetedShootSkill;
    public bool autoFire = true;
    public float initialDelay = 0.5f;

    public LeapChargeAoeSkill leapSkill;
    public bool autoLeap = true;
    public float leapInterval = 8f;

    // Wall minion as a Skill (no hard reference to specific type)
    public Skill wallMinionSkill;
    public bool autoWallMinion = true;
    public float wallMinionInterval = 10f;
    private float _nextWallMinionTime;

    // Barrage skill
    public LeapAdvanceBarrageSkill barrageSkill;
    public KeyCode barrageKey = KeyCode.R;
    public bool autoBarrage = true;
    public float barrageInterval = 12f;
    private float _nextBarrageTime;

    private float _startTime;
    private float _nextAiLeapTime;

    // Phase control (based on health)
    public BaseEnemy healthSource; // (optional) assign if health lives on a different object
    private BaseEnemy _baseEnemy; // resolved source
    private float _initialHealth; // fallback for BasicBaseEnemy.health
    private enum BossPhase { Phase1, Phase2, Phase3 }
    private BossPhase _phase = BossPhase.Phase1;
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
        if (barrageSkill == null)
        {
            barrageSkill = GetComponent<LeapAdvanceBarrageSkill>();
        }
        // If wall minion skill exists, pass startPoint as return override (via skill inspector is preferred)
        if (startPoint == null)
        {
            // default to current position at start
            GameObject anchor = new GameObject("BossStartPointAnchor");
            anchor.transform.position = transform.position;
            anchor.transform.rotation = Quaternion.identity;
            anchor.transform.SetParent(transform.parent);
            startPoint = anchor.transform;
        }
        // Wire LeapChargeAoeSkill to return to startPoint
        if (leapSkill != null && startPoint != null)
        {
            leapSkill.returnPoint = startPoint;
        }

        // Phase setup
        _baseEnemy = healthSource != null ? healthSource : GetComponentInParent<BaseEnemy>();
        if (_baseEnemy != null)
        {
            _initialHealth = Mathf.Max(1f, _baseEnemy.maxHealth);
        }
        else
        {
            _initialHealth = Mathf.Max(1f, (float)health);
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
        // Determine phase by health percent
        float hpPct = GetHealthPercent();
        // Use strict bounds so 70% belongs to Phase2 as requested (100–70: P1, 70–40: P2, <40: P3)
        hpPct = Mathf.Clamp01(hpPct);
        if (hpPct > 0.7f) _phase = BossPhase.Phase1;
        else if (hpPct > 0.4f) _phase = BossPhase.Phase2;
        else _phase = BossPhase.Phase3;

        // Phase 1 and 2 include basic shooting
        if ((_phase == BossPhase.Phase1 || _phase == BossPhase.Phase2) && autoFire && targetedShootSkill != null && target != null)
        {
            if (!targetedShootSkill.TryUseOn(target))
            {
                targetedShootSkill.TryUse();
            }
        }

        // Leap skill trigger: automated interval
        if ((_phase == BossPhase.Phase1 || _phase == BossPhase.Phase2) && leapSkill != null)
        {
            if (autoLeap && Time.time >= _nextAiLeapTime)
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

        // Wall minion skill: AI interval
        if (_phase == BossPhase.Phase2 && wallMinionSkill != null)
        {
            if (autoWallMinion && Time.time >= _nextWallMinionTime)
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

        // Barrage skill: manual key or auto interval
        if (_phase == BossPhase.Phase3 && barrageSkill != null)
        {
            if (Input.GetKeyDown(barrageKey))
            {
                if (target == null)
                {
                    GameObject maybe3 = GameObject.FindWithTag("Player");
                    if (maybe3 != null) target = maybe3.transform;
                }
                if (!barrageSkill.TryUseOn(target))
                {
                    barrageSkill.TryUse();
                }
            }
            else if (autoBarrage && Time.time >= _nextBarrageTime)
            {
                bool used2 = false;
                if (target != null) used2 = barrageSkill.TryUseOn(target);
                if (!used2) used2 = barrageSkill.TryUse();
                if (used2)
                {
                    _nextBarrageTime = Time.time + barrageInterval;
                }
            }
        }
    }

    private float GetHealthPercent()
    {
        if (_baseEnemy != null)
        {
            if (_baseEnemy.maxHealth <= 0f) return 1f;
            return Mathf.Clamp01(_baseEnemy.currentHealth / _baseEnemy.maxHealth);
        }
        // Fallback to BasicBaseEnemy.health vs initial snapshot
        return Mathf.Clamp01(((float)health) / Mathf.Max(1f, _initialHealth));
    }
    
}
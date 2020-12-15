﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewConfigAgentToto", menuName = "AgentConfigs/ConfigAgentToto", order = 1)]
public class ConfigAgentToto : ScriptableObject
{
    [Header("Drive")]
    [Range(-1.0f, 1.0f)]
    public float noveltyDrive = 1.0f;
    [Range(-1.0f, 1.0f)]
    public float heightUpDrive = 0.8f;
    [Range(-1.0f, 1.0f)]
    public float heightDownDrive = -0.2f;
    [Range(-1.0f, 1.0f)]
    public float safetyGainDrive = 0.5f;
    [Header("Fitness")]
    [Range(-1.0f, 1.0f)]
    public float noveltyReward = 1.0f;
    [Range(-1.0f, 1.0f)]
    public float heightUpReward = 0.8f;
    [Range(-1.0f, 1.0f)]
    public float heightDownReward = -0.2f;
    [Range(-1.0f, 1.0f)]
    public float safetyReward = 0.5f;
}

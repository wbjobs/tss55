using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntWar.Utils
{
    public static class BattleLogger
    {
        private static readonly Queue<string> _logQueue = new Queue<string>();
        private static readonly object _lock = new object();
        public static int MaxLogCount = 100;

        public static event Action<string> OnLogAdded;

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] {message}";

            lock (_lock)
            {
                _logQueue.Enqueue(logEntry);
                if (_logQueue.Count > MaxLogCount)
                {
                    _logQueue.Dequeue();
                }
            }

            Debug.Log(logEntry);
            OnLogAdded?.Invoke(logEntry);
        }

        public static void LogCombat(string attackerTeam, string attackerType, string defenderTeam, string defenderType, float damage)
        {
            string message = $"⚔️ 战斗: [{attackerTeam}]{attackerType} 攻击 [{defenderTeam}]{defenderType}，造成 {damage:F1} 伤害";
            Log(message);
        }

        public static void LogKill(string killerTeam, string killerType, string victimTeam, string victimType)
        {
            string message = $"💀 击杀: [{killerTeam}]{killerType} 击杀了 [{victimTeam}]{victimType}";
            Log(message);
        }

        public static void LogFoodGathered(string team, float amount)
        {
            string message = $"🍞 采集: [{team}]工蚁 采集了 {amount:F1} 食物并返回巢穴";
            Log(message);
        }

        public static void LogFoodDelivered(string team, float amount, float totalStored)
        {
            string message = $"🏠 存储: [{team}]巢穴 收到 {amount:F1} 食物，总存储: {totalStored:F1}";
            Log(message);
        }

        public static void LogStrategy(string team, string strategy)
        {
            string message = $"📋 策略: [{team}] 发布新策略: {strategy}";
            Log(message);
        }

        public static void LogSpawn(string team, string antType)
        {
            string message = $"🐜 孵化: [{team}]巢穴 孵化了一只{antType}";
            Log(message);
        }

        public static void LogFruitTreeGrown(float2 position, float yieldAmount)
        {
            string message = $"🌳 果树: 新果树成熟，位置 ({position.x:F1}, {position.y:F1})，产量 {yieldAmount:F1}";
            Log(message);
        }

        public static void LogFruitTreeRegrow(float2 position)
        {
            string message = $"🌱 果树: 果树重新结果，位置 ({position.x:F1}, {position.y:F1})";
            Log(message);
        }

        public static void LogCarrionSpawned(float2 position, float amount, string source)
        {
            string message = $"🦴 腐肉: 在 ({position.x:F1}, {position.y:F1}) 生成 {amount:F1} 食物（来源：{source}）";
            Log(message);
        }

        public static void LogCarrionDecayed(float2 position)
        {
            string message = $"💨 腐肉: 在 ({position.x:F1}, {position.y:F1}) 已腐烂消失";
            Log(message);
        }

        public static string[] GetAllLogs()
        {
            lock (_lock)
            {
                return _logQueue.ToArray();
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _logQueue.Clear();
            }
        }
    }
}

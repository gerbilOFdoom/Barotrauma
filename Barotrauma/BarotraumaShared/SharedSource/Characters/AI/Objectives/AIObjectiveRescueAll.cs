﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Barotrauma
{
    class AIObjectiveRescueAll : AIObjectiveLoop<Character>
    {
        public override string DebugTag => "rescue all";
        public override bool ForceRun => true;
        public override bool InverseTargetEvaluation => true;

        private const float vitalityThreshold = 80;
        private const float vitalityThresholdForOrders = 100;
        public static float GetVitalityThreshold(AIObjectiveManager manager, Character character, Character target)
        {
            if (manager == null)
            {
                return vitalityThreshold;
            }
            else
            {
                return character == target || manager.CurrentOrder is AIObjectiveRescueAll ? vitalityThresholdForOrders : vitalityThreshold;
            }
        }
        
        public AIObjectiveRescueAll(Character character, AIObjectiveManager objectiveManager, float priorityModifier = 1) 
            : base(character, objectiveManager, priorityModifier) { }

        protected override bool Filter(Character target) => IsValidTarget(target, character);

        protected override IEnumerable<Character> GetList() => Character.CharacterList;

        protected override float TargetEvaluation()
        {
            int otherRescuers = HumanAIController.CountCrew(c => c != HumanAIController && c.ObjectiveManager.IsCurrentObjective<AIObjectiveRescueAll>(), onlyBots: true);
            int targetCount = Targets.Count;
            bool anyRescuers = otherRescuers > 0;
            float ratio = anyRescuers ? targetCount / (float)otherRescuers : 1;
            if (objectiveManager.CurrentOrder == this)
            {
                return Targets.Min(t => GetVitalityFactor(t)) / ratio;
            }
            else
            {
                float multiplier = 1;
                if (anyRescuers)
                {
                    float mySkill = character.GetSkillLevel("medical");
                    int betterRescuers = HumanAIController.CountCrew(c => c != HumanAIController && c.Character.Info.Job.GetSkillLevel("medical") >= mySkill, onlyBots: true);
                    if (targetCount / (float)betterRescuers <= 1)
                    {
                        // Enough rescuers
                        return 100;
                    }
                    else
                    {
                        bool foundOtherMedics = HumanAIController.IsTrueForAnyCrewMember(c => c != HumanAIController && c.Character.Info.Job.Prefab.Identifier == "medicaldoctor");
                        if (foundOtherMedics)
                        {
                            if (character.Info.Job.Prefab.Identifier != "medicaldoctor")
                            {
                                // Double the vitality factor -> less likely to take action
                                multiplier = 2;
                            }
                        }
                    }
                }
                return Targets.Min(t => GetVitalityFactor(t)) / ratio * multiplier;
            }
        }

        public static float GetVitalityFactor(Character character)
        {
            float vitality = character.HealthPercentage - character.Bleeding - character.Bloodloss + Math.Min(character.Oxygen, 0);
            return Math.Clamp(vitality, 0, 100);
        }

        protected override AIObjective ObjectiveConstructor(Character target)
            => new AIObjectiveRescue(character, target, objectiveManager, PriorityModifier);

        protected override void OnObjectiveCompleted(AIObjective objective, Character target)
            => HumanAIController.RemoveTargets<AIObjectiveRescueAll, Character>(character, target);

        public static bool IsValidTarget(Character target, Character character)
        {
            if (target == null || target.IsDead || target.Removed) { return false; }
            if (!HumanAIController.IsFriendly(character, target)) { return false; }
            if (character.AIController is HumanAIController humanAI)
            {
                if (GetVitalityFactor(target) >= GetVitalityThreshold(humanAI.ObjectiveManager, character, target)) { return false; }
                if (!humanAI.ObjectiveManager.IsCurrentOrder<AIObjectiveRescueAll>())
                {
                    // Ignore unsafe hulls, unless ordered
                    if (humanAI.UnsafeHulls.Contains(target.CurrentHull))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (GetVitalityFactor(target) >= vitalityThreshold) { return false; }
            }
            if (target.Submarine == null || character.Submarine == null) { return false; }
            if (target.Submarine.TeamID != character.Submarine.TeamID) { return false; }
            if (target.CurrentHull == null) { return false; }
            if (character.Submarine != null && !character.Submarine.IsEntityFoundOnThisSub(target.CurrentHull, true)) { return false; }
            if (!target.IsPlayer && HumanAIController.IsActive(target) && target.AIController is HumanAIController targetAI)
            {
                // Ignore all concious targets that are currently fighting, fleeing or treating characters
                if (targetAI.ObjectiveManager.HasActiveObjective<AIObjectiveCombat>() || 
                    targetAI.ObjectiveManager.HasActiveObjective<AIObjectiveFindSafety>() ||
                    targetAI.ObjectiveManager.HasActiveObjective<AIObjectiveRescue>())
                {
                    return false;
                }
            }
            // Don't go into rooms that have enemies
            if (Character.CharacterList.Any(c => c.CurrentHull == target.CurrentHull && !HumanAIController.IsFriendly(character, c) && HumanAIController.IsActive(c))) { return false; }
            return true;
        }
    }
}

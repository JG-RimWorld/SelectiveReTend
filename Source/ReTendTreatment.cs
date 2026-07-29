using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SelectiveReTend;

public static class ReTendTreatment
{
    public static bool PerformAttempt(
        Pawn doctor,
        Pawn patient,
        Hediff primaryHediff,
        Medicine medicine)
    {
        ReTendCandidate primary = ReTendPlanner.GetCandidate(primaryHediff);
        if (primary == null)
        {
            return false;
        }

        MedicineSelector.GetQualityInputs(
            doctor,
            patient,
            medicine?.def,
            out float baseQuality,
            out float maximumQuality);

        bool primaryImproved = false;
        float primaryRoll = primary.CurrentQuality;
        int batchPosition = 0;

        foreach (Hediff hediff in ReTendPlanner.GetBatch(primary, medicine != null))
        {
            ReTendCandidate candidate = ReTendPlanner.GetCandidate(hediff);
            if (candidate == null)
            {
                continue;
            }

            float rolledQuality = Mathf.Clamp(
                baseQuality + Rand.Range(-QualityMath.RandomVariance, QualityMath.RandomVariance),
                0f,
                maximumQuality);
            bool improved = rolledQuality > candidate.CurrentQuality;
            if (improved)
            {
                candidate.TendComp.tendQuality = rolledQuality;
                patient.health.Notify_HediffChanged(hediff);
            }

            if (batchPosition == 0)
            {
                primaryRoll = rolledQuality;
                primaryImproved = improved;
            }
            batchPosition++;
        }

        ShowResult(patient, primaryHediff, primaryRoll, primaryImproved);
        PlayMedicineSound(patient, medicine);
        ConsumeMedicine(medicine);
        return primaryImproved;
    }

    private static void ShowResult(
        Pawn patient,
        Hediff hediff,
        float rolledQuality,
        bool improved)
    {
        if (!patient.Spawned)
        {
            return;
        }

        string key = improved ? "SRT_MoteImproved" : "SRT_MoteNotImproved";
        string text = key.Translate(
            hediff.Label.CapitalizeFirst(),
            rolledQuality.ToStringPercent());
        MoteMaker.ThrowText(
            patient.DrawPos,
            patient.Map,
            text,
            improved ? Color.green : Color.red,
            3.65f);
    }

    private static void PlayMedicineSound(Pawn patient, Medicine medicine)
    {
        if (medicine == null || !patient.Spawned)
        {
            return;
        }

        if (medicine.GetStatValue(StatDefOf.MedicalPotency)
            > ThingDefOf.MedicineIndustrial.GetStatValueAbstract(StatDefOf.MedicalPotency))
        {
            SoundDefOf.TechMedicineUsed.PlayOneShot(
                new TargetInfo(patient.Position, patient.Map));
        }
    }

    private static void ConsumeMedicine(Medicine medicine)
    {
        if (medicine == null || medicine.Destroyed)
        {
            return;
        }

        if (medicine.stackCount > 1)
        {
            medicine.stackCount--;
        }
        else
        {
            medicine.Destroy();
        }
    }
}

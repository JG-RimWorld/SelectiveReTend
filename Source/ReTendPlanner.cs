using RimWorld;
using Verse;

namespace SelectiveReTend;

public static class ReTendPlanner
{
    private const float QualityEpsilon = 0.0001f;

    public static bool TryMakePlan(Pawn doctor, Pawn patient, out ReTendPlan plan)
    {
        foreach (ReTendCandidate candidate in GetCandidates(patient))
        {
            MedicineChoice medicine = MedicineSelector.Choose(doctor, patient, candidate);
            MedicineSelector.GetQualityInputs(
                doctor,
                patient,
                medicine.MedicineDef,
                out float baseQuality,
                out float maxQuality);

            int attempts = MedicineSelector.EffectiveAttempts(
                SelectiveReTendMod.Settings.maxAttempts,
                medicine);
            if (attempts <= 0)
            {
                continue;
            }

            float chanceToImprove = QualityMath.ChancePerAttempt(
                baseQuality,
                candidate.CurrentQuality + QualityEpsilon,
                maxQuality);
            if (chanceToImprove <= 0f)
            {
                continue;
            }

            bool unlimitedWithoutMedicine = attempts == int.MaxValue;
            float referenceQuality;
            float totalChance;
            int referenceAttempts;

            if (unlimitedWithoutMedicine)
            {
                float chancePerAttempt = QualityMath.ChancePerAttempt(
                    baseQuality,
                    candidate.TargetQuality,
                    maxQuality);
                if (chancePerAttempt <= 0f)
                {
                    continue;
                }

                referenceQuality = candidate.TargetQuality;
                totalChance = 1f;
                referenceAttempts = int.MaxValue;
            }
            else
            {
                referenceQuality = QualityMath.ReferenceQuality(baseQuality, maxQuality, attempts);
                totalChance = QualityMath.TotalChance(
                    baseQuality,
                    candidate.TargetQuality,
                    maxQuality,
                    attempts);
                referenceAttempts = attempts;
            }

            SelectiveReTendSettings settings = SelectiveReTendMod.Settings;
            if (settings.skipSmallImprovement
                && referenceQuality < candidate.CurrentQuality + settings.minimumImprovement)
            {
                continue;
            }

            plan = new ReTendPlan(
                candidate,
                medicine,
                baseQuality,
                maxQuality,
                referenceQuality,
                totalChance,
                referenceAttempts,
                unlimitedWithoutMedicine);
            return true;
        }

        plan = null;
        return false;
    }

    public static ReTendCandidate GetCandidate(Hediff hediff)
    {
        if (hediff == null || hediff.pawn == null)
        {
            return null;
        }

        return TryCreateCandidate(hediff, out ReTendCandidate candidate)
            ? candidate
            : null;
    }

    public static IEnumerable<Hediff> GetBatch(ReTendCandidate primary, bool usingMedicine)
    {
        yield return primary.Hediff;

        List<ReTendCandidate> candidates = GetCandidates(primary.Hediff.pawn).ToList();
        HediffCompProperties_TendDuration properties =
            primary.Hediff.def.CompProps<HediffCompProperties_TendDuration>();

        if (properties?.tendAllAtOnce == true)
        {
            foreach (ReTendCandidate candidate in candidates)
            {
                if (candidate.Hediff != primary.Hediff
                    && candidate.Hediff.def == primary.Hediff.def)
                {
                    yield return candidate.Hediff;
                }
            }
            yield break;
        }

        if (primary.Category != TreatmentCategory.Injury || !usingMedicine)
        {
            yield break;
        }

        float combinedSeverity = primary.Hediff.Severity;
        foreach (ReTendCandidate candidate in candidates)
        {
            if (candidate.Hediff == primary.Hediff
                || candidate.Category != TreatmentCategory.Injury)
            {
                continue;
            }

            if (combinedSeverity + candidate.Hediff.Severity <= 20f)
            {
                combinedSeverity += candidate.Hediff.Severity;
                yield return candidate.Hediff;
            }
        }
    }

    private static IEnumerable<ReTendCandidate> GetCandidates(Pawn patient)
    {
        List<ReTendCandidate> candidates = new();
        foreach (Hediff hediff in patient.health.hediffSet.hediffs)
        {
            if (TryCreateCandidate(hediff, out ReTendCandidate candidate))
            {
                candidates.Add(candidate);
            }
        }

        return candidates
            .OrderBy(candidate => (int)candidate.Category)
            .ThenBy(candidate => ImmunityMargin(candidate))
            .ThenByDescending(candidate => LethalProgress(candidate))
            .ThenByDescending(candidate => candidate.Hediff.Severity)
            .ThenBy(candidate => candidate.CurrentQuality);
    }

    private static bool TryCreateCandidate(Hediff hediff, out ReTendCandidate candidate)
    {
        candidate = null;
        SelectiveReTendExtension extension = hediff.def.GetModExtension<SelectiveReTendExtension>();
        if (extension?.exclude == true || hediff.IsPermanent())
        {
            return false;
        }

        HediffComp_TendDuration tendComp = hediff.TryGetComp<HediffComp_TendDuration>();
        if (tendComp == null
            || !tendComp.IsTended
            || tendComp.TProps.TendIsPermanent
            || tendComp.TProps.disappearsAtTotalTendQuality >= 0)
        {
            return false;
        }

        TreatmentCategory? category = Classify(hediff, extension);
        if (category == null || !SelectiveReTendMod.Settings.Enabled(category.Value))
        {
            return false;
        }

        float target = SelectiveReTendMod.Settings.TargetFor(category.Value);
        if (tendComp.tendQuality >= target)
        {
            return false;
        }

        candidate = new ReTendCandidate(hediff, tendComp, category.Value, target);
        return true;
    }

    private static TreatmentCategory? Classify(
        Hediff hediff,
        SelectiveReTendExtension extension)
    {
        if (hediff.def.isInfection || extension?.isInfection == true)
        {
            return TreatmentCategory.Infection;
        }

        if (hediff is Hediff_Injury)
        {
            return TreatmentCategory.Injury;
        }

        if (hediff.def.PossibleToDevelopImmunityNaturally())
        {
            return TreatmentCategory.Disease;
        }

        return null;
    }

    private static float ImmunityMargin(ReTendCandidate candidate)
    {
        if (candidate.Category == TreatmentCategory.Injury)
        {
            return 0f;
        }

        HediffComp_Immunizable immunity =
            candidate.Hediff.TryGetComp<HediffComp_Immunizable>();
        return (immunity?.Immunity ?? 0f) - candidate.Hediff.Severity;
    }

    private static float LethalProgress(ReTendCandidate candidate)
    {
        float lethalSeverity = candidate.Hediff.def.lethalSeverity;
        return lethalSeverity > 0f
            ? candidate.Hediff.Severity / lethalSeverity
            : candidate.Hediff.Severity;
    }
}

using RimWorld;
using Verse;
using Verse.AI;

namespace SelectiveReTend;

public sealed class FloatMenuOptionProvider_SelectiveReTend : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;
    protected override bool CanSelfTarget => true;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        Pawn doctor = context.FirstSelectedPawn;
        return doctor != null
            && (!doctor.IsMutant || doctor.mutant.Def.canTend);
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(
        Pawn patient,
        FloatMenuContext context)
    {
        Pawn doctor = context.FirstSelectedPawn;
        if (doctor == null || patient.Dead || !IsValidPatient(doctor, patient))
        {
            yield break;
        }

        if (!ReTendPlanner.TryMakePlan(doctor, patient, out ReTendPlan plan))
        {
            yield break;
        }

        TaggedString cannotPrefix = "SRT_CannotReTend".Translate(patient.LabelShort);
        if (doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
        {
            yield return new FloatMenuOption(
                cannotPrefix + ": "
                + "CannotPrioritizeWorkTypeDisabled".Translate(WorkTypeDefOf.Doctor.gerundLabel),
                null);
            yield break;
        }

        if (patient.playerSettings?.medCare == MedicalCareCategory.NoCare)
        {
            yield return new FloatMenuOption(
                cannotPrefix + ": " + "MedicalCareDisabled".Translate(),
                null);
            yield break;
        }

        if (doctor == patient
            && (doctor.playerSettings == null || !doctor.playerSettings.selfTend))
        {
            yield return new FloatMenuOption(
                cannotPrefix + ": " + "SelfTendDisabled".Translate().CapitalizeFirst(),
                null);
            yield break;
        }

        if (patient.InAggroMentalState
            && !patient.health.hediffSet.HasHediff(HediffDefOf.Scaria))
        {
            yield return new FloatMenuOption(
                cannotPrefix + ": " + "PawnIsInAggroMentalState".Translate(patient).CapitalizeFirst(),
                null);
            yield break;
        }

        if (!doctor.CanReach(patient, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            yield return new FloatMenuOption(
                cannotPrefix + ": " + "NoPath".Translate().CapitalizeFirst(),
                null);
            yield break;
        }

        string label = BuildLabel(patient, plan);
        FloatMenuOption option = new(
            label,
            () => StartJob(doctor, patient),
            MenuOptionPriority.Default,
            null,
            patient);
        yield return FloatMenuUtility.DecoratePrioritizedTask(option, doctor, patient);
    }

    private static void StartJob(Pawn doctor, Pawn patient)
    {
        if (!ReTendPlanner.TryMakePlan(doctor, patient, out ReTendPlan plan))
        {
            Messages.Message(
                "SRT_NoLongerAvailable".Translate(),
                patient,
                MessageTypeDefOf.RejectInput,
                historical: false);
            return;
        }

        LocalTargetInfo medicineTarget = plan.Medicine.InitialThing != null
            ? new LocalTargetInfo(plan.Medicine.InitialThing)
            : LocalTargetInfo.Invalid;
        Job job = JobMaker.MakeJob(
            SelectiveReTendDefOf.JG_SelectiveReTend,
            patient,
            medicineTarget);
        job.count = SelectiveReTendMod.Settings.maxAttempts == 0
            ? -1
            : SelectiveReTendMod.Settings.maxAttempts;
        doctor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }

    private static string BuildLabel(Pawn patient, ReTendPlan plan)
    {
        string category = CategoryLabel(plan.Candidate.Category);
        string medicine = plan.Medicine.UsesMedicine
            ? plan.Medicine.MedicineDef.LabelCap
            : "SRT_WithoutMedicine".Translate();
        string details;

        if (plan.UnlimitedWithoutMedicine)
        {
            details = "SRT_MenuUnlimited".Translate(
                plan.Candidate.CurrentQuality.ToStringPercent("F0"),
                plan.Candidate.TargetQuality.ToStringPercent("F0"),
                medicine);
        }
        else
        {
            string attempts = plan.ReferenceAttempts == 1
                ? "SRT_AttemptSingular".Translate()
                : "SRT_AttemptPlural".Translate(plan.ReferenceAttempts);
            details = "SRT_MenuDetails".Translate(
                plan.Candidate.CurrentQuality.ToStringPercent("F0"),
                plan.Candidate.TargetQuality.ToStringPercent("F0"),
                plan.TotalChanceToTarget.ToStringPercent("F0"),
                attempts,
                medicine);
        }

        return "SRT_MenuOption".Translate(patient.LabelShort, category) + " — " + details;
    }

    private static string CategoryLabel(TreatmentCategory category)
    {
        return category switch
        {
            TreatmentCategory.Infection => "SRT_CategoryInfection".Translate(),
            TreatmentCategory.Disease => "SRT_CategoryDisease".Translate(),
            TreatmentCategory.Injury => "SRT_CategoryInjury".Translate(),
            _ => category.ToString()
        };
    }

    private static bool IsValidPatient(Pawn doctor, Pawn patient)
    {
        if (patient.Downed || patient == doctor)
        {
            return true;
        }

        if (patient.HostileTo(doctor.Faction))
        {
            return false;
        }

        return patient.IsColonist
            || patient.IsQuestLodger()
            || patient.IsPrisonerOfColony
            || patient.IsSlaveOfColony
            || (patient.Faction == Faction.OfPlayer && patient.IsAnimal)
            || (patient.IsColonySubhuman && patient.mutant.Def.entitledToMedicalCare);
    }
}

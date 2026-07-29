using RimWorld;
using Verse;
using Verse.AI;

namespace SelectiveReTend;

public sealed class JobDriver_SelectiveReTend : JobDriver
{
    private const int MaxMedicineReservation = 10;
    private const float QualityEpsilon = 0.0001f;

    private Hediff targetHediff;
    private ThingDef medicineDef;
    private int attemptsRemaining;
    private bool stateInitialized;

    private Pawn Patient => TargetPawnA;

    public override string GetReport()
    {
        return "SRT_JobReport".Translate(Patient.LabelShort);
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref targetHediff, "targetHediff");
        Scribe_Defs.Look(ref medicineDef, "medicineDef");
        Scribe_Values.Look(ref attemptsRemaining, "attemptsRemaining", 0);
        Scribe_Values.Look(ref stateInitialized, "stateInitialized", false);
        base.ExposeData();
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        InitializeState();
        if (targetHediff == null)
        {
            return false;
        }

        if (Patient != pawn
            && !pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed))
        {
            return false;
        }

        Thing medicine = job.targetB.Thing;
        if (medicine != null
            && medicine.Spawned
            && !pawn.Reserve(
                medicine,
                job,
                MaxMedicineReservation,
                DesiredMedicineCount(medicine),
                null,
                errorOnFailed))
        {
            return false;
        }

        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        InitializeState();
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnAggroMentalState(TargetIndex.A);
        this.FailOn(() =>
            Patient == pawn
            && pawn.Faction == Faction.OfPlayer
            && pawn.playerSettings != null
            && !pawn.playerSettings.selfTend);
        this.FailOn(() => Patient.playerSettings?.medCare == MedicalCareCategory.NoCare);
        AddFinishAction(_ => ReturnUnusedMedicineToInventory());

        PathEndMode patientPathEndMode = Patient == pawn
            ? PathEndMode.OnCell
            : Patient.InBed()
                ? PathEndMode.InteractionCell
                : PathEndMode.ClosestTouch;

        Toil goToPatient = Toils_Goto.GotoThing(TargetIndex.A, patientPathEndMode);
        Toil reserveMedicine = MakeReserveMedicineToil();
        Toil goToMedicine = Toils_Goto
            .GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B);
        Toil pickUpMedicine = MakePickupMedicineToil();

        int ticks = (int)(600f / pawn.GetStatValue(StatDefOf.MedicalTendSpeed));
        Toil wait = Toils_General.Wait(ticks);
        wait.FailOnCannotTouch(TargetIndex.A, patientPathEndMode)
            .WithProgressBarToilDelay(TargetIndex.A)
            .PlaySustainerOrSound(SoundDefOf.Interact_Tend);
        wait.activeSkill = () => SkillDefOf.Medicine;
        wait.handlingFacing = true;
        wait.tickIntervalAction = _ =>
        {
            if (pawn != Patient)
            {
                pawn.rotationTracker.FaceTarget(Patient);
            }
        };

        Toil finalize = ToilMaker.MakeToil("FinalizeSelectiveReTend");
        finalize.initAction = () => FinalizeAttempt(wait, reserveMedicine);
        finalize.defaultCompleteMode = ToilCompleteMode.Instant;

        if (medicineDef != null)
        {
            yield return Toils_Jump.JumpIf(goToPatient, MedicineIsHeldByDoctor);
            yield return reserveMedicine;
            yield return goToMedicine;
            yield return pickUpMedicine;
            yield return Toils_Jump.Jump(goToPatient);
        }

        yield return goToPatient;
        yield return wait;
        yield return finalize;
    }

    private void InitializeState()
    {
        if (stateInitialized)
        {
            return;
        }

        attemptsRemaining = job.count <= 0 ? -1 : job.count;
        if (ReTendPlanner.TryMakePlan(pawn, Patient, out ReTendPlan plan))
        {
            targetHediff = plan.Candidate.Hediff;
            medicineDef = plan.Medicine.MedicineDef;
            if (job.targetB.Thing == null && plan.Medicine.InitialThing != null)
            {
                job.SetTarget(TargetIndex.B, plan.Medicine.InitialThing);
            }
        }
        stateInitialized = true;
    }

    private void FinalizeAttempt(Toil wait, Toil reserveMedicine)
    {
        ReTendCandidate candidate = ReTendPlanner.GetCandidate(targetHediff);
        if (candidate == null)
        {
            EndJobWith(JobCondition.Succeeded);
            return;
        }

        Medicine medicine = null;
        if (medicineDef != null)
        {
            medicine = job.targetB.Thing as Medicine;
            if (medicine == null || medicine.Destroyed || medicine.def != medicineDef)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
        }

        ReTendTreatment.PerformAttempt(pawn, Patient, targetHediff, medicine);
        if (attemptsRemaining > 0)
        {
            attemptsRemaining--;
        }

        candidate = ReTendPlanner.GetCandidate(targetHediff);
        if (attemptsRemaining == 0 || candidate == null)
        {
            EndJobWith(JobCondition.Succeeded);
            return;
        }

        MedicineSelector.GetQualityInputs(
            pawn,
            Patient,
            medicineDef,
            out float baseQuality,
            out float maxQuality);
        if (QualityMath.ChancePerAttempt(
                baseQuality,
                candidate.CurrentQuality + QualityEpsilon,
                maxQuality) <= 0f)
        {
            EndJobWith(JobCondition.Succeeded);
            return;
        }

        if (medicineDef == null)
        {
            JumpToToil(wait);
            return;
        }

        Thing remainingMedicine = job.targetB.Thing;
        if (remainingMedicine != null
            && !remainingMedicine.Destroyed
            && MedicineIsHeldByDoctor())
        {
            JumpToToil(wait);
            return;
        }

        Thing replacement = MedicineSelector.FindMedicineOfDef(pawn, Patient, medicineDef);
        if (replacement == null)
        {
            EndJobWith(JobCondition.Succeeded);
            return;
        }

        job.SetTarget(TargetIndex.B, replacement);
        if (MedicineIsHeldByDoctor())
        {
            JumpToToil(wait);
        }
        else
        {
            JumpToToil(reserveMedicine);
        }
    }

    private Toil MakeReserveMedicineToil()
    {
        Toil toil = ToilMaker.MakeToil("ReserveSelectiveReTendMedicine");
        toil.initAction = () =>
        {
            Thing medicine = job.targetB.Thing;
            if (medicine == null || medicine.Destroyed)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (MedicineIsHeldByDoctor())
            {
                return;
            }

            int reservable = pawn.Map.reservationManager.CanReserveStack(
                pawn,
                medicine,
                MaxMedicineReservation);
            int desired = Math.Min(reservable, DesiredMedicineCount(medicine));
            if (desired <= 0
                || !pawn.Reserve(
                    medicine,
                    job,
                    MaxMedicineReservation,
                    desired))
            {
                EndJobWith(JobCondition.Incompletable);
            }
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        toil.atomicWithPrevious = true;
        return toil;
    }

    private Toil MakePickupMedicineToil()
    {
        Toil toil = ToilMaker.MakeToil("PickupSelectiveReTendMedicine");
        toil.initAction = () =>
        {
            Thing medicine = job.targetB.Thing;
            if (medicine == null || medicine.Destroyed)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int count = DesiredMedicineCount(medicine);
            if (pawn.carryTracker.TryStartCarry(medicine, count) <= 0)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (medicine.Spawned)
            {
                pawn.Map.reservationManager.Release(medicine, pawn, job);
            }
            job.SetTarget(TargetIndex.B, pawn.carryTracker.CarriedThing);
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        return toil;
    }

    private bool MedicineIsHeldByDoctor()
    {
        Thing medicine = job.targetB.Thing;
        return medicine != null
            && (pawn.carryTracker.CarriedThing == medicine
                || pawn.inventory?.innerContainer.Contains(medicine) == true);
    }

    private void ReturnUnusedMedicineToInventory()
    {
        Thing carried = pawn.carryTracker?.CarriedThing;
        if (carried == null
            || carried.Destroyed
            || carried.def != medicineDef
            || pawn.inventory == null)
        {
            return;
        }

        pawn.carryTracker.innerContainer.TryTransferToContainer(
            carried,
            pawn.inventory.innerContainer,
            carried.stackCount);
    }

    private int DesiredMedicineCount(Thing medicine)
    {
        int desired = attemptsRemaining < 0
            ? MaxMedicineReservation
            : Math.Min(MaxMedicineReservation, attemptsRemaining);
        return Math.Min(Math.Max(1, desired), medicine.stackCount);
    }
}

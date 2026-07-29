using Verse;

namespace SelectiveReTend;

public sealed class ReTendCandidate
{
    public Hediff Hediff { get; }
    public HediffComp_TendDuration TendComp { get; }
    public TreatmentCategory Category { get; }
    public float TargetQuality { get; }

    public float CurrentQuality => TendComp.tendQuality;

    public ReTendCandidate(
        Hediff hediff,
        HediffComp_TendDuration tendComp,
        TreatmentCategory category,
        float targetQuality)
    {
        Hediff = hediff;
        TendComp = tendComp;
        Category = category;
        TargetQuality = targetQuality;
    }
}

public sealed class MedicineChoice
{
    public Thing InitialThing { get; }
    public ThingDef MedicineDef { get; }
    public int AvailableCount { get; }

    public bool UsesMedicine => MedicineDef != null;

    public MedicineChoice(Thing initialThing, ThingDef medicineDef, int availableCount)
    {
        InitialThing = initialThing;
        MedicineDef = medicineDef;
        AvailableCount = availableCount;
    }
}

public sealed class ReTendPlan
{
    public ReTendCandidate Candidate { get; }
    public MedicineChoice Medicine { get; }
    public float BaseQuality { get; }
    public float MaximumQuality { get; }
    public float ReferenceQuality { get; }
    public float TotalChanceToTarget { get; }
    public int ReferenceAttempts { get; }
    public bool UnlimitedWithoutMedicine { get; }

    public ReTendPlan(
        ReTendCandidate candidate,
        MedicineChoice medicine,
        float baseQuality,
        float maximumQuality,
        float referenceQuality,
        float totalChanceToTarget,
        int referenceAttempts,
        bool unlimitedWithoutMedicine)
    {
        Candidate = candidate;
        Medicine = medicine;
        BaseQuality = baseQuality;
        MaximumQuality = maximumQuality;
        ReferenceQuality = referenceQuality;
        TotalChanceToTarget = totalChanceToTarget;
        ReferenceAttempts = referenceAttempts;
        UnlimitedWithoutMedicine = unlimitedWithoutMedicine;
    }
}

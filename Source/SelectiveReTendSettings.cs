using Verse;

namespace SelectiveReTend;

public sealed class SelectiveReTendSettings : ModSettings
{
    public bool enableInfections = true;
    public bool enableDiseases = true;
    public bool enableInjuries;

    public float infectionTarget = 0.70f;
    public float diseaseTarget = 0.60f;
    public float injuryTarget = 0.60f;

    public bool skipSmallImprovement = true;
    public float minimumImprovement = 0.10f;
    public int maxAttempts = 1;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref enableInfections, "enableInfections", true);
        Scribe_Values.Look(ref enableDiseases, "enableDiseases", true);
        Scribe_Values.Look(ref enableInjuries, "enableInjuries", false);
        Scribe_Values.Look(ref infectionTarget, "infectionTarget", 0.70f);
        Scribe_Values.Look(ref diseaseTarget, "diseaseTarget", 0.60f);
        Scribe_Values.Look(ref injuryTarget, "injuryTarget", 0.60f);
        Scribe_Values.Look(ref skipSmallImprovement, "skipSmallImprovement", true);
        Scribe_Values.Look(ref minimumImprovement, "minimumImprovement", 0.10f);
        Scribe_Values.Look(ref maxAttempts, "maxAttempts", 1);
        base.ExposeData();
    }

    public bool Enabled(TreatmentCategory category)
    {
        return category switch
        {
            TreatmentCategory.Infection => enableInfections,
            TreatmentCategory.Disease => enableDiseases,
            TreatmentCategory.Injury => enableInjuries,
            _ => false
        };
    }

    public float TargetFor(TreatmentCategory category)
    {
        return category switch
        {
            TreatmentCategory.Infection => infectionTarget,
            TreatmentCategory.Disease => diseaseTarget,
            TreatmentCategory.Injury => injuryTarget,
            _ => 1f
        };
    }
}

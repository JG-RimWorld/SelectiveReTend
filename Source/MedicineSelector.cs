using RimWorld;
using Verse;
using Verse.AI;

namespace SelectiveReTend;

public static class MedicineSelector
{
    private sealed class MedicineGroup
    {
        public ThingDef Def;
        public Thing Closest;
        public int Count;
        public float Potency;
    }

    public static MedicineChoice Choose(Pawn doctor, Pawn patient, ReTendCandidate candidate)
    {
        List<MedicineGroup> groups = GetAvailableGroups(doctor, patient);

        if (candidate.Category != TreatmentCategory.Injury)
        {
            MedicineGroup strongest = groups
                .OrderByDescending(group => group.Potency)
                .FirstOrDefault();
            return strongest == null
                ? new MedicineChoice(null, null, int.MaxValue)
                : new MedicineChoice(strongest.Closest, strongest.Def, strongest.Count);
        }

        List<MedicineChoice> choices = new()
        {
            new MedicineChoice(null, null, int.MaxValue)
        };
        choices.AddRange(groups
            .OrderBy(group => group.Potency)
            .Select(group => new MedicineChoice(group.Closest, group.Def, group.Count)));

        SelectiveReTendSettings settings = SelectiveReTendMod.Settings;
        foreach (MedicineChoice choice in choices)
        {
            GetQualityInputs(doctor, patient, choice.MedicineDef, out float baseQuality, out float maxQuality);
            int attempts = EffectiveAttempts(settings.maxAttempts, choice);
            if (attempts == int.MaxValue)
            {
                if (QualityMath.ChancePerAttempt(
                        baseQuality,
                        candidate.TargetQuality,
                        maxQuality) > 0f)
                {
                    return choice;
                }
            }
            else if (attempts > 0
                && QualityMath.ReferenceQuality(baseQuality, maxQuality, attempts) >= candidate.TargetQuality)
            {
                return choice;
            }
        }

        return choices
            .OrderByDescending(choice =>
            {
                GetQualityInputs(doctor, patient, choice.MedicineDef, out float baseQuality, out float maxQuality);
                int attempts = EffectiveAttempts(settings.maxAttempts, choice);
                return attempts == int.MaxValue
                    ? maxQuality
                    : QualityMath.ReferenceQuality(baseQuality, maxQuality, Math.Max(1, attempts));
            })
            .First();
    }

    public static Thing FindMedicineOfDef(Pawn doctor, Pawn patient, ThingDef medicineDef)
    {
        if (medicineDef == null)
        {
            return null;
        }

        Thing inventoryMedicine = doctor.inventory?.innerContainer
            .FirstOrDefault(thing => thing.def == medicineDef && IsAllowed(patient, thing));
        if (inventoryMedicine != null)
        {
            return inventoryMedicine;
        }

        return doctor.MapHeld?.listerThings
            .ThingsInGroup(ThingRequestGroup.Medicine)
            .Where(thing => thing.def == medicineDef && IsUsableMapMedicine(doctor, patient, thing))
            .OrderBy(thing => thing.PositionHeld.DistanceToSquared(patient.PositionHeld))
            .FirstOrDefault();
    }

    public static int EffectiveAttempts(int configuredAttempts, MedicineChoice choice)
    {
        if (!choice.UsesMedicine)
        {
            return configuredAttempts == 0 ? int.MaxValue : configuredAttempts;
        }

        return configuredAttempts == 0
            ? choice.AvailableCount
            : Math.Min(configuredAttempts, choice.AvailableCount);
    }

    public static void GetQualityInputs(
        Pawn doctor,
        Pawn patient,
        ThingDef medicineDef,
        out float baseQuality,
        out float maximumQuality)
    {
        baseQuality = TendUtility.CalculateBaseTendQuality(doctor, patient, medicineDef);
        maximumQuality = medicineDef?.GetStatValueAbstract(StatDefOf.MedicalQualityMax)
            ?? TendUtility.NoMedicineQualityMax;
    }

    private static List<MedicineGroup> GetAvailableGroups(Pawn doctor, Pawn patient)
    {
        Dictionary<ThingDef, MedicineGroup> groups = new();

        if (doctor.inventory != null)
        {
            foreach (Thing thing in doctor.inventory.innerContainer)
            {
                if (thing.def.IsMedicine && IsAllowed(patient, thing))
                {
                    Add(thing);
                }
            }
        }

        if (doctor.MapHeld != null)
        {
            foreach (Thing thing in doctor.MapHeld.listerThings.ThingsInGroup(ThingRequestGroup.Medicine))
            {
                if (IsUsableMapMedicine(doctor, patient, thing))
                {
                    Add(thing);
                }
            }
        }

        return groups.Values.ToList();

        void Add(Thing thing)
        {
            if (!groups.TryGetValue(thing.def, out MedicineGroup group))
            {
                group = new MedicineGroup
                {
                    Def = thing.def,
                    Closest = thing,
                    Potency = thing.def.GetStatValueAbstract(StatDefOf.MedicalPotency)
                };
                groups.Add(thing.def, group);
            }

            group.Count += thing.stackCount;
            if (thing.PositionHeld.DistanceToSquared(patient.PositionHeld)
                < group.Closest.PositionHeld.DistanceToSquared(patient.PositionHeld))
            {
                group.Closest = thing;
            }
        }
    }

    private static bool IsUsableMapMedicine(Pawn doctor, Pawn patient, Thing thing)
    {
        return thing.def.IsMedicine
            && IsAllowed(patient, thing)
            && !thing.IsForbidden(doctor)
            && doctor.CanReserve(thing, 10, 1)
            && doctor.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly);
    }

    private static bool IsAllowed(Pawn patient, Thing medicine)
    {
        MedicalCareCategory care = patient.playerSettings?.medCare ?? MedicalCareCategory.NoMeds;
        return care.AllowsMedicine(medicine.def);
    }
}

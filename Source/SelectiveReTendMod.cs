using UnityEngine;
using Verse;

namespace SelectiveReTend;

public sealed class SelectiveReTendMod : Mod
{
    public static SelectiveReTendSettings Settings { get; private set; }

    public SelectiveReTendMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<SelectiveReTendSettings>();
    }

    public override string SettingsCategory()
    {
        return "SRT_SettingsCategory".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();
        listing.Begin(inRect);

        listing.Label("SRT_SettingsCategories".Translate());
        listing.CheckboxLabeled(
            "SRT_EnableInfections".Translate(),
            ref Settings.enableInfections,
            "SRT_EnableInfectionsTip".Translate());
        DrawPercentSlider(listing, "SRT_InfectionTarget", ref Settings.infectionTarget);

        listing.CheckboxLabeled(
            "SRT_EnableDiseases".Translate(),
            ref Settings.enableDiseases,
            "SRT_EnableDiseasesTip".Translate());
        DrawPercentSlider(listing, "SRT_DiseaseTarget", ref Settings.diseaseTarget);

        listing.CheckboxLabeled(
            "SRT_EnableInjuries".Translate(),
            ref Settings.enableInjuries,
            "SRT_EnableInjuriesTip".Translate());
        DrawPercentSlider(listing, "SRT_InjuryTarget", ref Settings.injuryTarget);

        listing.GapLine();
        listing.Label("SRT_SettingsAttempts".Translate());
        listing.CheckboxLabeled(
            "SRT_SkipSmallImprovement".Translate(),
            ref Settings.skipSmallImprovement,
            "SRT_SkipSmallImprovementTip".Translate());
        DrawPercentSlider(listing, "SRT_MinimumImprovement", ref Settings.minimumImprovement, 0f, 0.50f);

        string attemptsLabel = Settings.maxAttempts == 0
            ? "SRT_Unlimited".Translate()
            : Settings.maxAttempts.ToString();
        listing.Label("SRT_MaxAttempts".Translate(attemptsLabel));
        Rect attemptsRect = listing.GetRect(24f);
        Settings.maxAttempts = Mathf.RoundToInt(Widgets.HorizontalSlider(
            attemptsRect,
            Settings.maxAttempts,
            0f,
            20f,
            middleAlignment: true,
            label: null,
            leftAlignedLabel: "0",
            rightAlignedLabel: "20",
            roundTo: 1f));
        TooltipHandler.TipRegion(attemptsRect, "SRT_AttemptsTip".Translate());

        listing.Gap();
        listing.Label("SRT_Version".Translate(SelectiveReTendVersion.Current));

        listing.End();
        base.DoSettingsWindowContents(inRect);
    }

    private static void DrawPercentSlider(
        Listing_Standard listing,
        string translationKey,
        ref float value,
        float minimum = 0f,
        float maximum = 1f)
    {
        listing.Label(translationKey.Translate(value.ToStringPercent("F0")));
        Rect rect = listing.GetRect(22f);
        value = Widgets.HorizontalSlider(
            rect,
            value,
            minimum,
            maximum,
            middleAlignment: true,
            label: null,
            leftAlignedLabel: minimum.ToStringPercent("F0"),
            rightAlignedLabel: maximum.ToStringPercent("F0"),
            roundTo: 0.01f);
        listing.Gap(4f);
    }
}

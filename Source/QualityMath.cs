using System;

namespace SelectiveReTend;

public static class QualityMath
{
    public const float RandomVariance = 0.25f;

    public static float ReferenceQuality(float baseQuality, float medicineMax, int attempts)
    {
        if (attempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempts));
        }

        double percentile = baseQuality - RandomVariance
            + 0.5d * Math.Pow(0.5d, 1d / attempts);
        return Clamp((float)percentile, 0f, medicineMax);
    }

    public static float ChancePerAttempt(
        float baseQuality,
        float targetQuality,
        float medicineMax)
    {
        if (targetQuality > medicineMax)
        {
            return 0f;
        }

        return Clamp((baseQuality + RandomVariance - targetQuality) / (RandomVariance * 2f), 0f, 1f);
    }

    public static float TotalChance(
        float baseQuality,
        float targetQuality,
        float medicineMax,
        int attempts)
    {
        if (attempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempts));
        }

        double chance = ChancePerAttempt(baseQuality, targetQuality, medicineMax);
        return (float)(1d - Math.Pow(1d - chance, attempts));
    }

    public static float Clamp(float value, float minimum, float maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }
}

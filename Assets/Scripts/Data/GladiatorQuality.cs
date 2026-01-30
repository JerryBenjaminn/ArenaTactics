namespace ArenaTactics.Data
{
    public enum GladiatorQuality
    {
        Poor,
        Average,
        Good,
        Excellent
    }

    [System.Serializable]
    public class QualityModifiers
    {
        public GladiatorQuality quality;
        public int minStatVariance;
        public int maxStatVariance;
        public float priceMultiplier;

        public QualityModifiers(GladiatorQuality q, int min, int max, float price)
        {
            quality = q;
            minStatVariance = min;
            maxStatVariance = max;
            priceMultiplier = price;
        }
    }
}

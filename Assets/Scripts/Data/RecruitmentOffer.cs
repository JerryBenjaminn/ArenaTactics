namespace ArenaTactics.Data
{
    [System.Serializable]
    public class RecruitmentOffer
    {
        public GladiatorInstance gladiator;
        public int price;
        public GladiatorQuality quality;
        public bool purchased = false;

        public RecruitmentOffer(GladiatorInstance glad, int offerPrice, GladiatorQuality qual)
        {
            gladiator = glad;
            price = offerPrice;
            quality = qual;
            purchased = false;
        }
    }
}

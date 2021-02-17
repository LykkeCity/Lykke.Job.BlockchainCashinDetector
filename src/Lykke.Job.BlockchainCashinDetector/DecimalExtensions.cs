namespace Lykke.Job.BlockchainCashinDetector
{
    public static class DecimalExtensions
    {
        public static int GetScale(this decimal value)
        {
            uint[] bits = (uint[])(object)decimal.GetBits(value);

            uint scale = (bits[3] >> 16) & 31;

            return (int)scale;
        }
    }
}

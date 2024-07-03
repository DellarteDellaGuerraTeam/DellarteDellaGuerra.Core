namespace DellarteDellaGuerra.Domain.EquipmentPool.Util
{
    public class Random : IRandom
    {
        private readonly System.Random _random = new();

        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }
    }
}
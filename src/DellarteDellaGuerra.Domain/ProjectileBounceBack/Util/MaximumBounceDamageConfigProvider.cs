namespace DellarteDellaGuerra.Domain.ProjectileBounceBack
{
    public class MaximumBounceDamageConfigProvider : IMaximumBounceDamageConfigProvider
    {
        public MaximumBounceDamageConfig GetMaximumBounceDamageConfigProvider()
        {
            return new MaximumBounceDamageConfig();
        }
    }
}
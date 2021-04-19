namespace OwnID.Extensibility.Providers
{
    public interface IRandomPasswordProvider
    {
        string Generate(int length = 12);
    }
}
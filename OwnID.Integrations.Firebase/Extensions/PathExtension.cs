namespace OwnID.Integrations.Firebase.Extensions
{
    public static class PathExtension
    {
        public static string ReplaceSpecPathSymbols(this string path)
        {
            return path.Replace("/", "__");
        }
    }
}
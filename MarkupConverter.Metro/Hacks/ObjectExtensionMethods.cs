namespace MarkupConverter.Metro.Hacks
{
    public static class ObjectExtensionMethods
    {
        public static string ToLower(this object o)
        {
            return ((string)o).ToLower();
        }
    }
}
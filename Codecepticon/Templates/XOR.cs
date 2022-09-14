namespace %_NAMESPACE_%
{
    class %_CLASS_%
    {
        public static string %_FUNCTION_%(string text, string key)
        {
            byte[] data = System.Convert.FromBase64String(text);
            byte[] k = System.Text.Encoding.UTF8.GetBytes(key);
            byte[] output = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                output[i] = (byte)(data[i] ^ k[i % k.Length]);
            }
            return System.Text.Encoding.UTF8.GetString(output);
        }
    }
}

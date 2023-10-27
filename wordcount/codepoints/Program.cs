string path = args.Length > 0 ? args[0] : "";


char[] buffer = new char[1024];
using var stream = File.OpenText(path);

int count = 0;
while ((count = stream.Read(buffer)) > 0)
{
    Span<char> chars = buffer.AsSpan(0, count);

    while (chars.Length > 0)
    {
        if (char.IsWhiteSpace(chars[0]))
        {
            Console.WriteLine($"{(int)chars[0]} (whitespace)");
        }
        else
        {
            Console.WriteLine($"{(int)chars[0]}");
        }

        chars = chars.Slice(1);
    }
}

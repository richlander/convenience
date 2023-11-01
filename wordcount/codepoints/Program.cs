using System.Text;

string path = args.Length > 0 ? args[0] : "";

char[] buffer = new char[1024];
using var stream = File.OpenText(path);

Console.WriteLine("codepoint,byte-length,bytes,notes");

int count = 0;
while ((count = stream.Read(buffer)) > 0)
{
    Span<char> chars = buffer.AsSpan(0, count);

    while (chars.Length > 0)
    {
        char c = chars[0];
        byte[] bytes = Encoding.UTF8.GetBytes((char[])[c]);

        Console.Write($"{(int)c},{bytes.Length},");

        var afterFirst = false;
        foreach(byte b in bytes)
        {
            if (afterFirst)
            {
                Console.Write("_");
            }
            else
            {
                afterFirst = true;
            }

            PrintBinary(b);
        }
        
        var notes = char.IsWhiteSpace(c) ? "whitespace" : "";
        Console.WriteLine($",{notes}");

        chars = chars.Slice(1);
    }
}

void PrintBinary(byte b)
{
    int mask = 128;
    for (int i = 0; i < 8; i++)
    {
        int val = (b & mask) > 0 ? 1 : 0;
        Console.Write(val);
        mask >>= 1;
    }

}

Console.Write('[');

for (int i = char.MinValue; i <= char.MaxValue; i++)
{
    if (char.IsWhiteSpace((char)i))
    {
        Console.Write($" (char){i},");
    }
}

Console.WriteLine(']');

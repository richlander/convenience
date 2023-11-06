using System.Text;

char englishLetter = 'A';
char fancyQuote =  '“';
// char emoji = (char)0x1f600; // won't compile
string emoji = "\U0001f600";
Encoding encoding = Encoding.Unicode;

PrintChar(englishLetter);
PrintChar(fancyQuote);
PrintChar(emoji[0]);
PrintUnicodeCharacter(emoji);

void PrintChar(char c)
{
    int value = (int)c;
    // Rune rune = new Rune(c); // will throw since emoji[0] is an invalid rune
    Console.WriteLine($"{c}; bytes: {encoding.GetByteCount([c])}; integer value: {(int)c}; round-trip: {(char)value}");
}

void PrintUnicodeCharacter(string s)
{
    char[] chars = s.ToCharArray();
    int value = char.ConvertToUtf32(s, 0);
    Rune r1 = (Rune)value;
    Rune r2 = new Rune(chars[0], chars[1]);
    Console.WriteLine($"{s}; chars: {chars.Length}; bytes: {encoding.GetByteCount(chars)}; integer value: {value}; round-trip {char.ConvertFromUtf32(value)};");
    Console.WriteLine($"{s}; Runes match: {r1 == r2 && r1.Value == value}; {nameof(Rune.Utf8SequenceLength)}: {r1.Utf8SequenceLength}; {nameof(Rune.Utf16SequenceLength)}: {r1.Utf16SequenceLength}");
}

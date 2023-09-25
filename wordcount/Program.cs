int iterations = args.Length > 0 && int.TryParse(args[0], out int val) ? val : 16;


if (iterations > 100)
{
    MultiFileTest.MultiFileTest.Go(iterations);
}
else
{
    OneFileTest.OneFileTest.Go(iterations);
}



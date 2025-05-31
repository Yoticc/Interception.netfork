using System.Reflection;

if (System.Diagnostics.Debugger.IsAttached)
    throw new Exception("Samples are launched in the debugger");

Type[] samples = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetInterface(nameof(ISample)) is not null).ToArray();
while (true)
{
    Console.Clear();
    Console.WriteLine(string.Join('\n', samples.Select((sample, i) => $"{i}. {sample.Name}")));
    Console.WriteLine();

    Console.Write("Sample #: ");
    if (!uint.TryParse(Console.ReadLine(), out var sampleN))
        continue;

    if (sampleN >= samples.Length)
        continue;

    var sample = samples[sampleN];
    var startMethod = sample.GetMethod("Start")!;
    var stopMethod = sample.GetMethod("Stop")!;

    startMethod.Invoke(null, []);
    do
    {
        Console.Clear();
        Console.WriteLine($"Current sample: {sample.Name}");
        Console.WriteLine($"Write \"exit\" to choose another one sample.");
    }
    while (Console.ReadLine() != "exit");
    stopMethod.Invoke(null, []);
}
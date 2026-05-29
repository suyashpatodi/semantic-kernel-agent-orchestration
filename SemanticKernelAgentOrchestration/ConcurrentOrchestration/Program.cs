using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using OpenAI;
using System.ClientModel;

var builder = Kernel.CreateBuilder();

var apiKey = string.Empty;
var endpoint = "https://models.github.ai/inference";

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) }
);


var kernel = builder.Build();

ChatCompletionAgent physicist = new ChatCompletionAgent()
{
    Name = "PhysicsExpert",
    Instructions = "You are an expert in physics. You answer questions from a physics perspective.",
    Kernel = kernel
};

ChatCompletionAgent chemist = new ChatCompletionAgent
{
    Name = "ChemistryExpert",
    Instructions = "You are an expert in chemistry. You answer questions from a chemistry perspective.",
    Kernel = kernel,
};

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

ConcurrentOrchestration orchestration = new ConcurrentOrchestration(physicist, chemist);

InProcessRuntime runtime = new InProcessRuntime();
await runtime.StartAsync();

try
{
    var result = await orchestration.InvokeAsync("What is temperature?", runtime);

    // FIX 2: Always pass a timeout. Without it, GetValueAsync blocks forever
    // if the API call silently fails (confirmed SK bug on custom endpoints).
    string[] output = await result.GetValueAsync(TimeSpan.FromSeconds(60));

    Console.WriteLine($"# RESULT:\n{string.Join("\n\n", output)}");
}
catch (TimeoutException)
{
    Console.WriteLine("ERROR: Orchestration timed out. Check your API key and endpoint.");
}
catch (Exception ex)
{
    // FIX 3: Wrap in try/catch — SK swallows agent exceptions internally;
    // at minimum a TimeoutException will surface with the timeout set above.
    Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
}
finally
{
    // FIX 4: RunUntilIdleAsync in finally so it always runs for proper cleanup,
    // even if GetValueAsync threw.
    await runtime.RunUntilIdleAsync();
}
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TDModLoader.Handlers.Utils;

public static class CodeEvaluator
{
    private static int _assemblyCounter = 0;

    private static Tuple<List<string>, List<string>> SplitCodeFromImports(string code)
    {
        var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var usingStatements = new List<string>();
        var codeWithoutUsings = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("using ") && trimmedLine.EndsWith(";"))
            {
                usingStatements.Add(line);
            }
            else if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                codeWithoutUsings.Add(line);
            }
        }
        return Tuple.Create(usingStatements, codeWithoutUsings);
    }
    
    public static object Execute(string code)
    {
        try
        {
            var assemblyId = Interlocked.Increment(ref _assemblyCounter);
            var className = $"CodeRunner_{assemblyId}";
            var assemblyName = $"DynamicAssembly_{assemblyId}_{Guid.NewGuid():N}";

            var parsedCode = SplitCodeFromImports(code);

            var fullCode = $@"{string.Join(Environment.NewLine, parsedCode.Item1)}
public class {className}
{{ 
    public object Execute() 
    {{ 
        {string.Join(Environment.NewLine, parsedCode.Item2)}
    }} 
}}";
            var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);
            var compilation = CSharpCompilation.Create(assemblyName)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location))
                .AddSyntaxTrees(syntaxTree);

            using var ms = new MemoryStream();
            var compilationResult = compilation.Emit(ms);

            if (!compilationResult.Success)
            {
                var errors = compilationResult.Diagnostics
                    .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage())
                    .ToList();

                return new { success = false, error = "Compilation failed", details = errors };
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var type = assembly.GetType(className);
            Debug.Assert(type != null, nameof(type) + " != null");
            var instance = Activator.CreateInstance(type);
            var method = type.GetMethod("Execute");
            Debug.Assert(method != null, nameof(method) + " != null");
            var result = method.Invoke(instance, null);
            return new { success = true, result};
        }
        catch (Exception ex)
        {
            return new { success = false, error = "Runtime error", details = ex.Message };
        }
    }
}


   

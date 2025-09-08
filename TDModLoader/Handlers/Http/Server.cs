namespace TDModLoader.Handlers.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

public class Server
{
    private readonly int _port;
    private readonly HttpListener _listener;
    private static int _assemblyCounter = 0;

    public Server(int port = 8080)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("JSON RPC Code Evaluator started at http://localhost:8080/");
        Console.WriteLine("GET http://localhost:8080/ for web interface");
        Console.WriteLine("POST JSON: {\"code\": \"return 2 + 2;\"}");

        while (true)
        {
            var context = await _listener.GetContextAsync();
            await HandleRequestAsync(context);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            Console.WriteLine($"Request: {request.HttpMethod} {request.Url}");

            // Handle GET requests - serve HTML interface
            if (request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                await SendHtmlResponse(response);
                return;
            }

            // Handle POST requests - execute code
            if (!request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await SendJsonResponse(response, new { error = "Only GET and POST methods supported" }, 405);
                return;
            }

            string requestBody;
            using (var reader = new StreamReader(request.InputStream))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            var requestData = JsonSerializer.Deserialize<CodeRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (string.IsNullOrEmpty(requestData?.Code))
            {
                await SendJsonResponse(response, new { error = "Code field is required" }, 400);
                return;
            }

            var result = EvaluateCode(requestData.Code);
            await SendJsonResponse(response, result, 200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling request: {ex.Message}");
            await SendJsonResponse(response, new { error = ex.Message }, 500);
        }
    }

    private async Task SendHtmlResponse(HttpListenerResponse response)
    {
        var html = @"<!DOCTYPE html>
<html>
<head>
    <title>C# Code Evaluator</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .container { display: flex; gap: 20px; height: 90vh; }
        .panel { background: white; border-radius: 8px; padding: 20px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .left-panel { flex: 1; display: flex; flex-direction: column; }
        .right-panel { flex: 1; }
        h1 { margin-top: 0; color: #333; }
        textarea { width: 100%; height: 70%; border: 1px solid #ddd; border-radius: 4px; padding: 10px; font-family: 'Courier New', monospace; font-size: 14px; resize: none; }
        button { background: #007acc; color: white; border: none; padding: 12px 24px; border-radius: 4px; cursor: pointer; font-size: 16px; margin-top: 10px; }
        button:hover { background: #005a9e; }
        .results { width: 100%; height: 100%; border: 1px solid #ddd; border-radius: 4px; padding: 10px; font-family: 'Courier New', monospace; font-size: 14px; background: #f9f9f9; white-space: pre-wrap; overflow-y: auto; }
        .loading { color: #666; font-style: italic; }
        .error { color: #d32f2f; }
        .success { color: #2e7d32; }
    </style>
</head>
<body>
    <h1>C# Code Evaluator</h1>
    <div class='container'>
        <div class='panel left-panel'>
            <h3>Code Input</h3>
            <textarea id='codeInput' placeholder='Enter C# code here...
Example:
return 2 + 2;

or

var nums = new int[] {1,2,3,4,5};
return nums.Where(x => x > 3).Sum();'>return 2 + 2;</textarea>
            <button onclick='runCode()'>Run Code</button>
        </div>
        <div class='panel right-panel'>
            <h3>Results</h3>
            <div id='results' class='results'>Ready to execute code...</div>
        </div>
    </div>

    <script>
        async function runCode() {
            const code = document.getElementById('codeInput').value;
            const resultsDiv = document.getElementById('results');
            
            if (!code.trim()) {
                resultsDiv.innerHTML = 'Please enter some code to execute.';
                resultsDiv.className = 'results error';
                return;
            }
            
            resultsDiv.innerHTML = 'Executing code...';
            resultsDiv.className = 'results loading';
            
            try {
                const response = await fetch('/', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ code: code })
                });
                
                const result = await response.json();
                
                if (result.success) {
                    resultsDiv.innerHTML = `✅ Success!\n\nResult: ${result.result}`;
                    resultsDiv.className = 'results success';
                } else {
                    let errorMessage = `❌ ${result.error}`;
                    if (result.details) {
                        if (Array.isArray(result.details)) {
                            errorMessage += '\n\nDetails:\n' + result.details.join('\n');
                        } else {
                            errorMessage += '\n\nDetails:\n' + result.details;
                        }
                    }
                    resultsDiv.innerHTML = errorMessage;
                    resultsDiv.className = 'results error';
                }
            } catch (error) {
                resultsDiv.innerHTML = `❌ Network Error: ${error.message}`;
                resultsDiv.className = 'results error';
            }
        }
        
        // Allow Ctrl+Enter to run code
        document.getElementById('codeInput').addEventListener('keydown', function(e) {
            if (e.ctrlKey && e.key === 'Enter') {
                runCode();
            }
        });
    </script>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.StatusCode = 200;
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private object EvaluateCode(string code)
    {
        try
        {
            var assemblyId = Interlocked.Increment(ref _assemblyCounter);
            var className = $"CodeRunner_{assemblyId}";
            var assemblyName = $"DynamicAssembly_{assemblyId}_{Guid.NewGuid():N}";

// Extract using statements from the code
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

// Build the using statements section
            var allUsingStatements = string.Join(Environment.NewLine, new[]
            {
                "using System;",
                "using System.Linq;", 
                "using System.Collections.Generic;"
            }.Concat(usingStatements.Where(u => !u.Trim().StartsWith("using System;") && 
                                                !u.Trim().StartsWith("using System.Linq;") && 
                                                !u.Trim().StartsWith("using System.Collections.Generic;"))));

// Reconstruct code without using statements
            var cleanedCode = string.Join(Environment.NewLine, codeWithoutUsings);

// Wrap code in a simple method
            string fullCode = $@"{allUsingStatements}

public class {className}
{{ 
    public object Execute() 
    {{ 
        {cleanedCode}
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
            EmitResult compilationResult = compilation.Emit(ms);

            if (!compilationResult.Success)
            {
                var errors = compilationResult.Diagnostics
                    .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage())
                    .ToList();

                return new { success = false, error = "Compilation failed", details = errors };
            }

            ms.Seek(0, SeekOrigin.Begin);
            Assembly assembly = Assembly.Load(ms.ToArray());
            Type type = assembly.GetType(className);
            object instance = Activator.CreateInstance(type);
            MethodInfo method = type.GetMethod("Execute");

            object result = method.Invoke(instance, null);

            return new { success = true, result};
        }
        catch (Exception ex)
        {
            return new { success = false, error = "Runtime error", details = ex.Message };
        }
    }

    private async Task SendJsonResponse(HttpListenerResponse response, object data, int statusCode)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var buffer = Encoding.UTF8.GetBytes(json);

        response.ContentType = "application/json";
        response.StatusCode = statusCode;
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    public void Stop() => _listener.Stop();
    
}

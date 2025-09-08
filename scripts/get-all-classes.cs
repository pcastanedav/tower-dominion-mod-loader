using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

// Configuration - assemblies/namespaces to include
var includeList = new string[] {
    "Assembly-CSharp",
    "Il2CppNvizzio"
};

// Lambda to check if should include
Func<string, bool> shouldInclude = name => 
    includeList.Any(include => 
        name == include || 
        name.StartsWith(include)
    );

// Return the actual object structure instead of JSON string
var allClassesData = new Dictionary<string, object>();

foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    try 
    {
        var assemblyName = assembly.GetName().Name;
        
        // Only include specified assemblies
        if (!shouldInclude(assemblyName))
        {
            continue;
        }
        
        var assemblyTypes = new Dictionary<string, object>();
        
        foreach (var type in assembly.GetTypes().Where(t => t.IsClass))
        {
            try
            {
                // Include all types from included assemblies, or types with included namespaces
                bool includeType = shouldInclude(assemblyName) || 
                                 (type.Namespace != null && shouldInclude(type.Namespace));
                
                if (!includeType)
                {
                    continue;
                }
                
                var typeData = new Dictionary<string, object>
                {
                    ["FullName"] = type.FullName,
                    ["Namespace"] = type.Namespace ?? "",
                    ["IsAbstract"] = type.IsAbstract,
                    ["IsSealed"] = type.IsSealed,
                    ["IsPublic"] = type.IsPublic,
                    ["BaseType"] = type.BaseType?.FullName ?? "",
                    
                    ["Properties"] = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Select(p => new Dictionary<string, object>
                        {
                            ["Name"] = p.Name,
                            ["Type"] = p.PropertyType.Name,
                            ["CanRead"] = p.CanRead,
                            ["CanWrite"] = p.CanWrite
                        }).ToArray(),
                    
                    ["Methods"] = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .Where(m => !m.IsSpecialName && m.DeclaringType == type)
                        .Select(m => new Dictionary<string, object>
                        {
                            ["Name"] = m.Name,
                            ["ReturnType"] = m.ReturnType.Name,
                            ["Parameters"] = m.GetParameters().Select(param => new Dictionary<string, object>
                            {
                                ["Name"] = param.Name,
                                ["Type"] = param.ParameterType.Name
                            }).ToArray(),
                            ["ParameterCount"] = m.GetParameters().Length
                        }).ToArray()
                };
                
                assemblyTypes[type.Name] = typeData;
            }
            catch (Exception ex)
            {
                assemblyTypes[type.Name] = new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["FullName"] = type.FullName
                };
            }
        }
        
        // Only add assembly if it has types
        if (assemblyTypes.Count > 0)
        {
            allClassesData[assemblyName] = assemblyTypes;
        }
    }
    catch (Exception ex)
    {
        var assemblyName = assembly.GetName().Name;
        if (shouldInclude(assemblyName))
        {
            allClassesData[assemblyName] = new Dictionary<string, object>
            {
                ["Error"] = ex.Message
            };
        }
    }
}

// Return the object directly - let the API handle JSON serialization
return allClassesData;

using Python.Runtime;
using System.Diagnostics;

static class PythonUtils
{
    public static PyModule MainScope { get; private set; } = null!;
    public static bool isPythonInit = false;

    // safe method to init python if not already is
    public static void SetupPython()
    {
        if (isPythonInit) return;

        ColorLog.Log("&aInitializing python...");

        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments =
                "-c \"import sys,os; p=os.path.join(sys.prefix,f'python{sys.version_info.major}{sys.version_info.minor}.dll'); print(p if os.path.exists(p) else '')\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi)
            ?? throw new Exception("Failed to start python process");

        string output = p.StandardOutput.ReadToEnd().Trim();
        string error = p.StandardError.ReadToEnd().Trim();
        p.WaitForExit();

        if (p.ExitCode != 0 || string.IsNullOrEmpty(output))
            throw new Exception($"Python DLL not found. {error}");

        Runtime.PythonDLL = output;
        PythonEngine.Initialize();

        // create scope and add path project/python
        MainScope = Py.CreateScope();

        string pythonDir = Path.Combine(Directory.GetCurrentDirectory(), "python");

        RunCode($"import sys; sys.path.insert(0, r'{pythonDir.Replace("\\", "\\\\")}')");

        // done
        ColorLog.Log("&qSuccess!");
        isPythonInit = true;
    }

    public static void ShutdownPython()
    {
        if (isPythonInit)
        {
            PythonEngine.Shutdown();
            isPythonInit = false;
        }
    }

    public static void RunCode(string pythonCode)
    {
        using (Py.GIL())
            MainScope.Exec(pythonCode);
    }

    public static void RunFile(string filepath)
        => RunCode(File.ReadAllText(filepath));

    public static T? RunCode<T>(string pythonCode, string resultVar)
    {
        using (Py.GIL())
        {
            MainScope.Exec(pythonCode);
            dynamic result = MainScope.Get(resultVar);

            return (T)result;
        }
    }

    public static T? RunFile<T>(string filepath, string resultVar)
        => RunCode<T>(File.ReadAllText(filepath), resultVar);

    public static void RegisterEnvWrapper(EnvWrapper wrapper)
    {
        using (Py.GIL())
        {
            // wrapper as global variable
            MainScope.Set("env_wrapper", wrapper.ToPython());
        }
    }
}
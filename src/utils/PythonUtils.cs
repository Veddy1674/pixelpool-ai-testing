using Python.Runtime;
using System;
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

    public static void RunCode(string pythonCode, params object[] args)
    {
        using (Py.GIL())
        {
            // pass args as globals
            for (int i = 0; i < args.Length; i++)
                MainScope.Set($"arg{i}", args[i].ToPython());

            MainScope.Exec(pythonCode);
        }
    }

    public static void RunFile(string filepath, params object[] args)
        => RunCode(File.ReadAllText(filepath), args);

    // forgot why i made this, it looks mostly useless, why would python return a value to c#? they probably can communicate better via files
    /*
    public static T? RunCode<T>(string pythonCode, string resultVar, params object[] args)
    {
        using (Py.GIL())
        {
            // pass args as globals
            for (int i = 0; i < args.Length; i++)
                MainScope.Set($"arg{i}", args[i].ToPython());

            MainScope.Exec(pythonCode);
            dynamic result = MainScope.Get(resultVar);

            return (T)result;
        }
    }

    public static T? RunFile<T>(string filepath, string resultVar)
        => RunCode<T>(File.ReadAllText(filepath), resultVar);
    */

    // register globals to python
    public static void RegisterEnvWrapper(EnvWrapper wrapper)
    {
        using (Py.GIL())
        {
            // new module 'core'
            dynamic sys = Py.Import("sys");
            dynamic modules = sys.modules;

            var coreModule = new PyModule("core");

            // set globals ONLY to core.py:

            // environment wrapper (used by env.py)
            coreModule.SetAttr("env_wrapper", wrapper.ToPython());

            // global to print via ColorLog.Log (with colors)
            coreModule.SetAttr("printc", PyObject.FromManagedObject(
                new Action<string>(msg => ColorLog.Log(msg))
            ));

            // add to system modules
            modules["core"] = coreModule;
        }
    }
}
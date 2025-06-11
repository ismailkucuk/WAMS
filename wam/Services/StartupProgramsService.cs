using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;

namespace wam.Services
{
    public class StartupProgram
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Source { get; set; } // "HKCU", "HKLM", "Startup Folder"
    }

    public class StartupProgramsService
    {
        public static List<StartupProgram> GetStartupPrograms()
        {
            List<StartupProgram> programs = new List<StartupProgram>();

            // HKCU
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (key != null)
                {
                    foreach (string name in key.GetValueNames())
                    {
                        programs.Add(new StartupProgram
                        {
                            Name = name,
                            Path = key.GetValue(name)?.ToString(),
                            Source = "HKCU"
                        });
                    }
                }
            }

            // HKLM
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (key != null)
                {
                    foreach (string name in key.GetValueNames())
                    {
                        programs.Add(new StartupProgram
                        {
                            Name = name,
                            Path = key.GetValue(name)?.ToString(),
                            Source = "HKLM"
                        });
                    }
                }
            }

            // Startup folder
            string startupFolder = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup";
            if (Directory.Exists(startupFolder))
            {
                foreach (var file in Directory.GetFiles(startupFolder))
                {
                    programs.Add(new StartupProgram
                    {
                        Name = Path.GetFileName(file),
                        Path = file,
                        Source = "Startup Folder"
                    });
                }
            }

            return programs;
        }
    }
}

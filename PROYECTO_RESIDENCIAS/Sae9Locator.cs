using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PROYECTO_RESIDENCIAS
{
    public static class Sae9Locator
    {
        /// <summary>
        /// Devuelve la ruta completa del archivo FDB de SAE9 para la Empresa solicitada.
        /// </summary>
        public static string FindSaeDatabase(int empresa = 1)
        {
            if (empresa < 1 || empresa > 99) throw new ArgumentOutOfRangeException(nameof(empresa));
            string emp2 = empresa.ToString("00");

            var candidates = new List<string>();

            // 1) Registro
            candidates.AddRange(GetCandidatesFromRegistry(emp2));

            // 2) File system (ProgramData, Program Files (x86), etc.)
            candidates.AddRange(GetCandidatesFromFileSystem(emp2));

            // 3) Filtra existentes y únicos
            var found = candidates
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim('"'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists)
                .ToList();

            if (found.Count == 0)
                throw new FileNotFoundException($"No se encontró la BD de SAE 9 para Empresa {emp2}. Configura manualmente o instala SAE 9 con rutas estándar.");

            if (found.Count == 1)
                return found[0];

            // 4) Heurística de selección si hay varios
            string empPathFragment = Path.DirectorySeparatorChar + $"Empresa{emp2}" + Path.DirectorySeparatorChar + "Datos" + Path.DirectorySeparatorChar;

            string PickBest(IEnumerable<string> items)
            {
                return items
                    .Select(p =>
                    {
                        int score = 0;
                        string up = p.ToUpperInvariant();
                        string dirUp = Path.GetDirectoryName(p)?.ToUpperInvariant() ?? "";

                        // Máxima prioridad: exactamente en ...\EmpresaNN\Datos\
                        if ((p ?? "").IndexOf(empPathFragment, StringComparison.OrdinalIgnoreCase) >= 0) score += 100;

                        // Preferir archivos con EMPRENN en el nombre (p. ej. SAE90EMPRE01.FDB)
                        if (Path.GetFileName(up).Contains($"EMPRE{emp2}")) score += 10;

                        // Bonus si empieza con "SAE"
                        if (Path.GetFileName(up).StartsWith("SAE")) score += 3;

                        // Última modificación como desempate (más reciente = mejor)
                        DateTime mtime = File.GetLastWriteTime(p);
                        long mtScore = mtime.Ticks / TimeSpan.TicksPerSecond; // normaliza a segundos
                        return new { Path = p, Score = score, Mtime = mtScore };
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Mtime)
                    .Select(x => x.Path)
                    .First();
            }

            return PickBest(found);
        }


        private static IEnumerable<string> GetCandidatesFromRegistry(string emp2)
        {
            var paths = new List<string>();
            // Posibles ramas
            string[] hives = {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\ASPEL",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Aspel",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\ASPEL",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Aspel"
            };

            foreach (var hive in hives)
            {
                try
                {
                    using var baseKey = OpenHiveRoot(hive);
                    if (baseKey == null) continue;

                    foreach (var sub in baseKey.GetSubKeyNames())
                    {
                        if (!sub.Replace(" ", "").ToUpperInvariant().Contains("SAE9")) continue;

                        using var k = baseKey.OpenSubKey(sub);
                        if (k == null) continue;

                        foreach (var name in k.GetValueNames())
                        {
                            var val = k.GetValue(name)?.ToString() ?? "";
                            if (string.IsNullOrWhiteSpace(val)) continue;

                            // Buscamos directorios de Datos/Empresa
                            if (name.ToUpperInvariant().Contains("DAT") || name.ToUpperInvariant().Contains("RUTA") || name.ToUpperInvariant().Contains("PATH"))
                            {
                                // Si es carpeta, explora dentro
                                if (Directory.Exists(val))
                                {
                                    paths.AddRange(ProbeEmpresaPaths(val, emp2));
                                }
                                else
                                {
                                    // Si es archivo directo y coincide, tómalo
                                    if (IsSaeEmpresaFile(val, emp2))
                                        paths.Add(val);
                                }
                            }
                        }
                    }
                }
                catch { /* ignora, seguimos probando */ }
            }

            return paths;
        }

        private static RegistryKey OpenHiveRoot(string hivePath)
        {
            // hivePath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\ASPEL"
            string[] parts = hivePath.Split('\\');
            if (parts.Length < 2) return null;

            RegistryKey root = parts[0].ToUpperInvariant() switch
            {
                "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                _ => null
            };
            if (root == null) return null;

            string subPath = string.Join("\\", parts.Skip(1));
            return root.OpenSubKey(subPath);
        }



        private static IEnumerable<string> GetCandidatesFromFileSystem(string emp2)
        {
            var results = new List<string>();

            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData); // C:\ProgramData
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);       // C:\Program Files (x86)
            string systemRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";

            // Raíces y variantes típicas de SAE 9
            string[] bases =
            {
        Path.Combine(programData, @"Aspel\Sistemas Aspel\SAE 9.0\Datos"),
        Path.Combine(programData, @"Aspel\Sistemas Aspel\SAE9.00"),
        Path.Combine(pf86,        @"Common Files\Aspel\Sistemas Aspel\SAE 9.0"),
        Path.Combine(pf86,        @"Common Files\Aspel\Sistemas Aspel\SAE9.00"),
        Path.Combine(pf86,        @"Aspel\Aspel-SAE 9.0\Datos"),
        Path.Combine(systemRoot,  @"Aspel\Sistemas Aspel\SAE9.00")
    };

            foreach (var b in bases)
            {
                if (!Directory.Exists(b)) continue;

                // Caso + común: ...\EmpresaNN\Datos\*.FDB (cualquier nombre)
                string empDir = Path.Combine(b, $"Empresa{emp2}");
                string datos = Path.Combine(empDir, "Datos");

                if (Directory.Exists(datos))
                {
                    results.AddRange(Directory.GetFiles(datos, "*.FDB", SearchOption.TopDirectoryOnly));
                }

                // Explora recursivo por variaciones (EMPRESA01, subcarpetas, etc.)
                try
                {
                    var empDirs = Directory.GetDirectories(b, $"*mpresa{emp2}*", SearchOption.AllDirectories);
                    foreach (var ed in empDirs.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        // *.FDB directamente en la carpeta empresa
                        results.AddRange(Directory.GetFiles(ed, "*.FDB", SearchOption.TopDirectoryOnly));

                        // y también en un posible subdirectorio "Datos"
                        var datosSub = Path.Combine(ed, "Datos");
                        if (Directory.Exists(datosSub))
                            results.AddRange(Directory.GetFiles(datosSub, "*.FDB", SearchOption.TopDirectoryOnly));
                    }
                }
                catch { /* ignorar */ }
            }

            // Barrido acotado en Common Files\Aspel (por si la estructura es diferente)
            try
            {
                string cfAspel = Path.Combine(pf86, @"Common Files\Aspel");
                if (Directory.Exists(cfAspel))
                {
                    var fdbs = Directory.EnumerateFiles(cfAspel, "*.FDB", SearchOption.AllDirectories)
                                        .Where(p => p.IndexOf($"Empresa{emp2}", StringComparison.OrdinalIgnoreCase) >= 0
                                                 || p.IndexOf($"mpresa{emp2}", StringComparison.OrdinalIgnoreCase) >= 0);
                    results.AddRange(fdbs);
                }
            }
            catch { /* ignorar */ }

            return results;
        }


        private static IEnumerable<string> ProbeEmpresaPaths(string baseDir, string emp2)
        {
            var list = new List<string>();
            try
            {
                if (Directory.Exists(baseDir))
                    list.AddRange(Directory.GetFiles(baseDir, "*.FDB", SearchOption.TopDirectoryOnly));

                var datos = Path.Combine(baseDir, "Datos");
                if (Directory.Exists(datos))
                    list.AddRange(Directory.GetFiles(datos, "*.FDB", SearchOption.TopDirectoryOnly));
            }
            catch { /* ignorar */ }
            return list;
        }



        private static bool IsSaeEmpresaFile(string path, string emp2)
        {
            try
            {
                var name = Path.GetFileName(path).ToUpperInvariant();
                return name.Contains($"EMPRE{emp2}") && name.EndsWith(".FDB");
            }
            catch { return false; }
        }
    }
}

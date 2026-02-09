using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace PROYECTO_RESIDENCIAS
{
    /// <summary>
    /// Localiza la base de datos Firebird de Aspel SAE 9 para una Empresa dada (01..99),
    /// usando Registro de Windows y rutas típicas de instalación.
    /// Pensado para usarse en configuración inicial, no en cada arranque de la aplicación.
    /// </summary>
    public static class Sae9Locator
    {
        /// <summary>
        /// Devuelve la ruta completa del archivo FDB de SAE 9 para la Empresa solicitada.
        /// Lanza FileNotFoundException si no encuentra ninguna coincidencia.
        /// </summary>
        public static string FindSaeDatabase(int empresa = 1)
        {
            if (empresa < 1 || empresa > 99)
                throw new ArgumentOutOfRangeException(nameof(empresa), "La empresa debe estar entre 1 y 99.");

            string emp2 = empresa.ToString("00");

            var candidates = new List<string>();

            // 1) Registro
            candidates.AddRange(GetCandidatesFromRegistry(emp2));

            // 2) File system (ProgramData, Program Files (x86), etc.)
            candidates.AddRange(GetCandidatesFromFileSystem(emp2));

            // 3) Filtra existentes, únicos
            var found = candidates
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim('"'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists)
                .ToList();

            if (found.Count == 0)
                throw new FileNotFoundException(
                    $"No se encontró la BD de SAE 9 para Empresa {emp2}. " +
                    "Configura manualmente o instala SAE 9 con rutas estándar.");

            if (found.Count == 1)
                return found[0];

            // 4) Heurística de selección si hay varios
            return PickBest(found, emp2);
        }

        /// <summary>
        /// Intenta localizar la BD de SAE 9 para la empresa indicada.
        /// Devuelve true/false en lugar de lanzar excepción.
        /// </summary>
        public static bool TryFindSaeDatabase(int empresa, out string path, out Exception error)
        {
            try
            {
                path = FindSaeDatabase(empresa);
                error = null!;
                return true;
            }
            catch (Exception ex)
            {
                path = string.Empty;
                error = ex;
                return false;
            }
        }

        // ---------------------- DETALLE DE LOCALIZACIÓN ----------------------

        private static string PickBest(IEnumerable<string> items, string emp2)
        {
            string empPathFragment = Path.DirectorySeparatorChar
                                     + $"Empresa{emp2}"
                                     + Path.DirectorySeparatorChar
                                     + "Datos"
                                     + Path.DirectorySeparatorChar;

            return items
                .Select(p =>
                {
                    int score = 0;
                    string up = p.ToUpperInvariant();

                    // Máxima prioridad: exactamente en ...\EmpresaNN\Datos\
                    if ((p ?? "").IndexOf(empPathFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 100;

                    // Preferir archivos con EMPRENN en el nombre (p. ej. SAE90EMPRE01.FDB)
                    if (Path.GetFileName(up).Contains($"EMPRE{emp2}"))
                        score += 10;

                    // Bonus si empieza con "SAE"
                    if (Path.GetFileName(up).StartsWith("SAE", StringComparison.Ordinal))
                        score += 3;

                    // Última modificación como desempate (más reciente = mejor)
                    DateTime mtime = File.GetLastWriteTime(p);
                    long mtScore = mtime.Ticks / TimeSpan.TicksPerSecond;

                    return new { Path = p, Score = score, Mtime = mtScore };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Mtime)
                .Select(x => x.Path)
                .First();
        }

        private static IEnumerable<string> GetCandidatesFromRegistry(string emp2)
        {
            var paths = new List<string>();

            // Posibles ramas
            string[] hives =
            {
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
                        if (!sub.Replace(" ", "").ToUpperInvariant().Contains("SAE9"))
                            continue;

                        using var k = baseKey.OpenSubKey(sub);
                        if (k == null) continue;

                        foreach (var name in k.GetValueNames())
                        {
                            var val = k.GetValue(name)?.ToString() ?? "";
                            if (string.IsNullOrWhiteSpace(val)) continue;

                            if (name.ToUpperInvariant().Contains("DAT")
                             || name.ToUpperInvariant().Contains("RUTA")
                             || name.ToUpperInvariant().Contains("PATH"))
                            {
                                // Si es carpeta, explora dentro buscando EmpresaNN
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
                catch
                {
                    // Ignorar problemas de permisos/estructura, continuar con el resto
                }
            }

            return paths;
        }

        private static RegistryKey? OpenHiveRoot(string hivePath)
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
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);              // C:\Program Files (x86)
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

                // Caso más común: ...\EmpresaNN\Datos\*.FDB
                string empDir = Path.Combine(b, $"Empresa{emp2}");
                string datos = Path.Combine(empDir, "Datos");

                if (Directory.Exists(datos))
                {
                    results.AddRange(Directory.GetFiles(datos, "*.FDB", SearchOption.TopDirectoryOnly)
                                              .Where(p => IsSaeEmpresaFile(p, emp2)));
                }

                // Explorar variaciones EmpresaNN en subcarpetas
                try
                {
                    var empDirs = Directory.GetDirectories(b, $"*mpresa{emp2}*", SearchOption.AllDirectories);
                    foreach (var ed in empDirs.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        // *.FDB directamente en la carpeta empresa
                        results.AddRange(Directory.GetFiles(ed, "*.FDB", SearchOption.TopDirectoryOnly)
                                                  .Where(p => IsSaeEmpresaFile(p, emp2)));

                        // y también en un posible subdirectorio "Datos"
                        var datosSub = Path.Combine(ed, "Datos");
                        if (Directory.Exists(datosSub))
                        {
                            results.AddRange(Directory.GetFiles(datosSub, "*.FDB", SearchOption.TopDirectoryOnly)
                                                      .Where(p => IsSaeEmpresaFile(p, emp2)));
                        }
                    }
                }
                catch
                {
                    // Ignorar problemas de permisos
                }
            }

            // Barrido acotado en Common Files\Aspel (último recurso)
            try
            {
                string cfAspel = Path.Combine(pf86, @"Common Files\Aspel");
                if (Directory.Exists(cfAspel))
                {
                    var fdbs = Directory.EnumerateFiles(cfAspel, "*.FDB", SearchOption.AllDirectories)
                        .Where(p =>
                            (p.IndexOf($"Empresa{emp2}", StringComparison.OrdinalIgnoreCase) >= 0
                          || p.IndexOf($"mpresa{emp2}", StringComparison.OrdinalIgnoreCase) >= 0)
                            && IsSaeEmpresaFile(p, emp2));

                    results.AddRange(fdbs);
                }
            }
            catch
            {
                // Ignorar
            }

            return results;
        }

        private static IEnumerable<string> ProbeEmpresaPaths(string baseDir, string emp2)
        {
            var list = new List<string>();

            try
            {
                // 1) Si el mismo baseDir contiene archivos .FDB válidos para la empresa
                if (Directory.Exists(baseDir))
                {
                    list.AddRange(Directory.GetFiles(baseDir, "*.FDB", SearchOption.TopDirectoryOnly)
                                           .Where(p => IsSaeEmpresaFile(p, emp2)));
                }

                // 2) Buscar subcarpetas tipo EmpresaNN
                var empDirs = Directory.GetDirectories(baseDir, $"*mpresa{emp2}*", SearchOption.AllDirectories);
                foreach (var ed in empDirs.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    list.AddRange(Directory.GetFiles(ed, "*.FDB", SearchOption.TopDirectoryOnly)
                                           .Where(p => IsSaeEmpresaFile(p, emp2)));

                    var datos = Path.Combine(ed, "Datos");
                    if (Directory.Exists(datos))
                    {
                        list.AddRange(Directory.GetFiles(datos, "*.FDB", SearchOption.TopDirectoryOnly)
                                               .Where(p => IsSaeEmpresaFile(p, emp2)));
                    }
                }
            }
            catch
            {
                // Ignorar problemas de permisos/path
            }

            return list;
        }

        private static bool IsSaeEmpresaFile(string path, string emp2)
        {
            try
            {
                var name = Path.GetFileName(path).ToUpperInvariant();
                return name.Contains($"EMPRE{emp2}") && name.EndsWith(".FDB", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}

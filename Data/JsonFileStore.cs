using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RetailInventorySales.Data
{
    /// <summary>
    /// Very small generic helper for persisting a list of objects to a
    /// JSON file on disk and loading it back. This project intentionally
    /// avoids a database engine / ORM NuGet dependency so the solution
    /// can be opened and run offline with nothing beyond the .NET SDK -
    /// see the "Technologies used" section of the documentation for the
    /// reasoning behind this choice.
    /// </summary>
    public static class JsonFileStore<T>
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true
        };

        public static List<T> Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new List<T>();
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<T>();
                }

                var result = JsonSerializer.Deserialize<List<T>>(json, Options);
                return result ?? new List<T>();
            }
            catch (Exception ex)
            {
                // A corrupt data file should not crash the application on
                // startup. Fall back to an empty list and let the user
                // continue working; the file will be re-created on the
                // next save.
                System.Diagnostics.Debug.WriteLine($"Failed to load {path}: {ex.Message}");
                return new List<T>();
            }
        }

        public static void Save(string path, List<T> items)
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(items, Options);
            File.WriteAllText(path, json);
        }
    }
}
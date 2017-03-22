﻿using System;
using System.IO;
using System.Threading;
using GestureSign.Common.Log;
using Newtonsoft.Json;

namespace GestureSign.Common.Configuration
{
    public static class FileManager
    {
        #region Constructors

        static FileManager()
        {
        }

        #endregion

        #region Public Methods

        public static bool SaveObject(object serializableObject, string filePath, bool typeName = false)
        {
            try
            {
                string backup = null;
                if (File.Exists(filePath))
                {
                    backup = BackupFile(filePath);
                    WaitFile(filePath);
                }

                // Open json file
                using (StreamWriter sWrite = new StreamWriter(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    if (typeName)
                    {
                        serializer.TypeNameHandling = TypeNameHandling.Objects;
                        serializer.TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                    }
                    serializer.Serialize(sWrite, serializableObject);
                }
                //  File.WriteAllText(filePath, JsonConvert.SerializeObject(SerializableObject));

                if (File.Exists(backup))
                    File.Delete(backup);
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                return false;
            }
        }

        public static T LoadObject<T>(string filePath, bool backup, bool typeName = false)
        {
            try
            {
                if (!File.Exists(filePath)) return default(T);

                WaitFile(filePath);

                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<T>(json, typeName
                        ? new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects }
                        : new JsonSerializerSettings());
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                if (backup)
                    BackupFile(filePath);
                return default(T);
            }
        }

        public static void WaitFile(string filePath)
        {
            int count = 0;
            while (IsFileLocked(filePath) && count != 10)
            {
                count++;
                Thread.Sleep(50);
            }
        }

        private static string BackupFile(string filePath)
        {
            try
            {
                string backupFileName = Path.Combine(Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath) +
            DateTime.Now.ToString("yyMMddHHmmssffff") +
            Path.GetExtension(filePath));
                File.Copy(filePath, backupFileName, true);
                return backupFileName;
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return null;
            }
        }

        private static bool IsFileLocked(string file)
        {
            try
            {
                using (File.Open(file, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException exception)
            {
                var errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 65535;
                return errorCode == 32 || errorCode == 33;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
}

/*
;    Project:       Open-Vehicle-LibNet
;
;    Changes:
;    1.0    2018-12-01  Initial release
;
;    (C) 2018       Anko Hanse
;
; Permission is hereby granted, free of charge, to any person obtaining a copy
; of this software and associated documentation files (the "Software"), to deal
; in the Software without restriction, including without limitation the rights
; to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
; copies of the Software, and to permit persons to whom the Software is
; furnished to do so, subject to the following conditions:
;
; The above copyright notice and this permission notice shall be included in
; all copies or substantial portions of the Software.
;
; THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
; IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
; FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
; AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
; LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
; OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
; THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace OpenVehicle.App.Helpers
{
    internal static class SettingsHelper
    {
        #region Settings

        public enum StorageStrategies
        {
            /// <summary>Local, isolated folder</summary>
            Local,
            /// <summary>Cloud, isolated folder. 100k cumulative limit.</summary>
            Roaming,
            /// <summary>Local, temporary folder (not for settings)</summary>
            Temporary
        }


        /// <summary>Returns if a setting is found in the specified storage strategy</summary>
        /// <param name="key">Path of the setting in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Boolean: true if found, false if not found</returns>
        public static bool SettingExists(string key, StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                ApplicationDataContainer container = GetContainer(location);
                return container.Values.ContainsKey(key);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::SettingsExists: " + e.Message);
                return false;
            }
        }


        public static bool SettingExists(string key, ApplicationDataContainer container)
        {
            try
            { 
                return container.Values.ContainsKey(key);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::SettingsExists: " + e.Message);
                return false;
            }
        }


        /// <summary>Reads and converts a setting into specified type T</summary>
        /// <typeparam name="T">Specified type into which to value is converted</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="otherwise">Return value if key is not found or convert fails</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Specified type T</returns>
        public static T GetSetting<T>(string key, T otherwise = default(T), StorageStrategies location = StorageStrategies.Local)
        {
            try
            {
                ApplicationDataContainer container = GetContainer(location);

                if (SettingExists(key, container))
                    return (T)container.Values[key.ToString()];
                else
                    return otherwise;
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::GetSetting: " + e.Message);
                return otherwise;
            }
        }


        /// <summary>Serializes an object and write to file in specified storage strategy</summary>
        /// <typeparam name="T">Specified type of object to serialize</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="value">Instance of object to be serialized and written</param>
        /// <param name="location">Location storage strategy</param>
        public static T GetSettingClass<T>(string key, T otherwise = default(T), StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                string sValue = GetSetting<string>(key, null, location);
                if (sValue == null)
                    return otherwise;

                T result = Deserialize<T>(sValue);
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::SetSettingClass: " + e.Message);
                return otherwise;
            }
        }


        /// <summary>Serializes an object and write to file in specified storage strategy</summary>
        /// <typeparam name="T">Specified type of object to serialize</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="value">Instance of object to be serialized and written</param>
        /// <param name="location">Location storage strategy</param>
        public static void SetSetting<T>(string key, T value, StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                ApplicationDataContainer container = GetContainer(location);

                if (SettingExists(key, container))
                    container.Values[key] = value;
                else
                    container.Values.Add(key, value);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::SetSetting: " + e.Message);
            }
        }


        /// <summary>Serializes an object and write to file in specified storage strategy</summary>
        /// <typeparam name="T">Specified type of object to serialize</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="value">Instance of object to be serialized and written</param>
        /// <param name="location">Location storage strategy</param>
        public static void SetSettingClass<T>(string key, T value, StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                string sValue = Serialize(value);
                
                SetSetting<string>(key, sValue, location);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::SetSettingClass: " + e.Message);
            }
        }


        public static void DeleteSetting(string key, StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                ApplicationDataContainer container = GetContainer(location);

                if (SettingExists(key, container))
                    container.Values.Remove(key);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::DeleteSetting: " + e.Message);
            }
        }


        private static ApplicationDataContainer GetContainer(StorageStrategies location = StorageStrategies.Local)
        {
            try
            {
                switch (location)
                {
                    case StorageStrategies.Local:   return ApplicationData.Current.LocalSettings;
                    case StorageStrategies.Roaming: return ApplicationData.Current.RoamingSettings;
                    default:
                        throw new NotSupportedException(location.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::GetContainer: " + e.Message);
                return null;
            }
        }

        #endregion


        #region File

        /// <summary>Returns if a file is found in the specified storage strategy</summary>
        /// <param name="key">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Boolean: true if found, false if not found</returns>
        public static async Task<bool> FileExistsAsync(string key, StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                StorageFile file = await GetIfFileExistsAsync(key, location);
                
                return (file != null);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::FileExistsAsync: " + e.Message);
                return false;
            }
        }


        /// <summary>Deletes a file in the specified storage strategy</summary>
        /// <param name="key">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        public static async Task<bool> DeleteFileAsync(string key, StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                var file = await GetIfFileExistsAsync(key, location);
                if (file != null)
                    await file.DeleteAsync();

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::DeleteFileAsync: " + e.Message);
                return false;
            }
        }


        /// <summary>Reads and deserializes a file into specified type T</summary>
        /// <typeparam name="T">Specified type into which to deserialize file content</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Specified type T</returns>
        public static async Task<T> ReadFileAsync<T>(string key, StorageStrategies location = StorageStrategies.Local)
        {
            try
            {
                // fetch file
                var file = await GetIfFileExistsAsync(key, location);
                if (file == null)
                    return default(T);

                // read content
                var str = await FileIO.ReadTextAsync(file);

                // convert to obj
                return Deserialize<T>(str);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::ReadFileAsync: " + e.Message);
                 return default(T);
            }
        }


        /// <summary>Serializes an object and write to file in specified storage strategy</summary>
        /// <typeparam name="T">Specified type of object to serialize</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="value">Instance of object to be serialized and written</param>
        /// <param name="location">Location storage strategy</param>
        public static async Task<bool> WriteFileAsync<T>(string key, T value, StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                // create file
                StorageFolder folder = GetFolder(location);
                StorageFile   file   = await folder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);

                // convert to string
                var str = Serialize(value);
                // save string to file
                await FileIO.WriteTextAsync(file, str);

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::WriteFileAsync: " + e.Message);
                return false;
            }
        }


        public static async Task<IReadOnlyList<StorageFile>> GetFilesAsync(StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                StorageFolder folder = GetFolder(location);

                return await folder.GetFilesAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::GetFilesAsync: " + e.Message);
                return null;
            }
        }


        private static StorageFolder GetFolder(StorageStrategies location = StorageStrategies.Local)
        {
            try
            { 
                switch (location)
                {
                    case StorageStrategies.Local:       return ApplicationData.Current.LocalFolder;     
                    case StorageStrategies.Roaming:     return ApplicationData.Current.RoamingFolder;   
                    case StorageStrategies.Temporary:   return ApplicationData.Current.TemporaryFolder; 
                    default:
                        throw new NotSupportedException(location.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::GetFolder: " + e.Message);
                return null;
            }
        }


        /// <summary>Returns a file if it is found in the specified storage strategy</summary>
        /// <param name="key">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>StorageFile</returns>
        private static async Task<StorageFile> GetIfFileExistsAsync(string key, StorageStrategies location = StorageStrategies.Local)
        {
            StorageFile retval;
            try
            {
                StorageFolder folder = GetFolder(location);

                retval = await folder.GetFileAsync(key);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::GetIfFileExistsAsync: " + e.Message);
                return null;
            }

            return retval;
        }

        #endregion


        /// <summary>Serializes the specified object as a JSON string</summary>
        /// <param name="objectToSerialize">Specified object to serialize</param>
        /// <returns>JSON string of serialzied object</returns>
        private static string Serialize(object objectToSerialize)
        {
            try
            {
                using (System.IO.MemoryStream stm = new System.IO.MemoryStream())
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(objectToSerialize.GetType());
                    ser.WriteObject(stm, objectToSerialize);
                    stm.Position = 0;

                    using (StreamReader rd = new StreamReader(stm))
                    {
                        return rd.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::Serialize:" + e.Message);
                return string.Empty;
            }
        }
        

        /// <summary>Deserializes the JSON string as a specified object</summary>
        /// <typeparam name="T">Specified type of target object</typeparam>
        /// <param name="jsonString">JSON string source</param>
        /// <returns>Object of specied type</returns>
        private static T Deserialize<T>(string jsonString)
        {
            try
            { 
                using (MemoryStream stm = new MemoryStream( Encoding.Unicode.GetBytes(jsonString)) )
                {
                    var ser = new DataContractJsonSerializer(typeof(T));

                    return (T)ser.ReadObject(stm);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("EXCEPTION in SettingsHelper::Deserialize:" + e.Message);
                return default(T);
            }
        }

    }
}

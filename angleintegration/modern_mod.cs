﻿using System;
using System.IO;
using System.Reflection;
using Modding;
using UnityEngine;

// ReSharper disable MemberCanBePrivate.Global

namespace angleintegration
{
    public struct modern_mod_vars
    {
        public readonly string modName;
        public readonly string version;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly int modVersionInt;
        public readonly int loadPriority;

        public modern_mod_vars(string modName, string version, int modVersionInt, int loadPriority)
        {
            this.modName = modName;
            this.version = version;
            this.modVersionInt = modVersionInt;
            this.loadPriority = loadPriority;
        }
    }

    public class modern_mod : Mod
    {
        protected modern_mod_vars modVars;

        protected modern_mod()
        {
            setupModVars(new modern_mod_vars("UNNAMED MOD PLEASE NAME", "UNKNOWN_VERSION", 0, -5));
        }

        protected void setupModVars(modern_mod_vars v)
        {
            modVars = v;
            var field = typeof(modern_mod).GetField
                ("Name", BindingFlags.Instance | BindingFlags.Public);
            field?.SetValue(this, modVars.modName);

            Log("modname is " + Name);

            angleint.modernMods.Add(modVars);
        }

        public override string GetVersion()
        {
            return modVars.version;
        }

        public override int LoadPriority()
        {
            Log("Load prio is " + modVars.loadPriority);
            return modVars.loadPriority;
        }

        public virtual string getVersionAppend()
        {
            return "";
        }
    }


    public class modern_mod<TSaveSettings, TGlobalSettings> :
        modern_mod<TSaveSettings> where TSaveSettings : IModSettings, new() where TGlobalSettings : IModSettings, new()
    {
        private readonly string globalSettingsFilename;
        private TGlobalSettings modGlobalSettings;

        protected modern_mod()
        {
            globalSettingsFilename = Application.persistentDataPath + ModHooks.PathSeperator + "Redwing" +
                                     ".settings.json";
            loadGlobalSettings();
        }

        protected TGlobalSettings globalSettings
        {
            get
            {
                var gSettings = modGlobalSettings;
                if (gSettings != null)
                    return gSettings;
                return modGlobalSettings = Activator.CreateInstance<TGlobalSettings>();
            }
            set => modGlobalSettings = value;
        }

        protected void saveGlobalSettings()
        {
            Log("Saving redwing settings!");
            if (File.Exists(globalSettingsFilename + ".bak"))
                File.Delete(globalSettingsFilename + ".bak");
            if (File.Exists(globalSettingsFilename))
                File.Move(globalSettingsFilename, globalSettingsFilename + ".bak");
            using (var fileStream = File.Create(globalSettingsFilename))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    var json = JsonUtility.ToJson(globalSettings, true);
                    streamWriter.Write(json);
                }
            }
        }

        public void loadGlobalSettings()
        {
            Log("Loading redwing settings!");
            if (!File.Exists(globalSettingsFilename))
                return;
            using (var fileStream = File.OpenRead(globalSettingsFilename))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    modGlobalSettings = JsonUtility.FromJson<TGlobalSettings>(streamReader.ReadToEnd());
                }
            }
        }
    }


    public class modern_mod<TSaveSettings> : modern_mod where TSaveSettings : IModSettings, new()
    {
        private TSaveSettings modSettings;


        protected modern_mod()
        {
            ModHooks.Instance.BeforeSavegameSaveHook += saveSettings;
            ModHooks.Instance.AfterSavegameLoadHook += loadSettings;
        }

        public TSaveSettings settings
        {
            get
            {
                var set = modSettings;
                if (set != null)
                    return set;
                return modSettings = Activator.CreateInstance<TSaveSettings>();
            }
            set => modSettings = value;
        }

        private void loadSettings(SaveGameData data)
        {
            var name = GetType().Name;
            Log("Loading savegame data!");
            if (data?.modData == null || !data.modData.ContainsKey(name))
                return;
            settings = Activator.CreateInstance<TSaveSettings>();
            settings.SetSettings(data.modData[name]);
            ISerializationCallbackReceiver set;
            if ((set = (object) settings as ISerializationCallbackReceiver) == null)
                return;
            set.OnAfterDeserialize();
        }

        private void saveSettings(SaveGameData data)
        {
            var name = GetType().Name;
            Log("Adding Settings to Save file");
            if (data.modData == null)
                data.modData = new ModSettingsDictionary();
            if (data.modData.ContainsKey(name))
                data.modData[name] = settings;
            else
                data.modData.Add(name, settings);
        }
    }

    public class modern_mod<TSaveSettings, TGlobalSettings, TSecondarySettings> :
        modern_mod<TSaveSettings, TGlobalSettings> where TSaveSettings : IModSettings, new()
        where TGlobalSettings : IModSettings, new()
        where TSecondarySettings : IModSettings, new()
    {
        private readonly string secondarySettingsFilename;
        private TSecondarySettings modSecondarySettings;

        protected modern_mod()
        {
            secondarySettingsFilename = Application.persistentDataPath + ModHooks.PathSeperator + "Redwing" +
                                        ".flamegen.json";
            loadSecondarySettings();
        }

        protected TSecondarySettings secondarySettings
        {
            get
            {
                var sSettings = modSecondarySettings;
                if (sSettings != null)
                    return sSettings;
                return modSecondarySettings = Activator.CreateInstance<TSecondarySettings>();
            }
            set => modSecondarySettings = value;
        }

        protected void saveSecondarySettings()
        {
            Log("Saving flamegen Settings");
            if (File.Exists(secondarySettingsFilename + ".bak"))
                File.Delete(secondarySettingsFilename + ".bak");
            if (File.Exists(secondarySettingsFilename))
                File.Move(secondarySettingsFilename, secondarySettingsFilename + ".bak");
            using (var fileStream = File.Create(secondarySettingsFilename))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    var json = JsonUtility.ToJson(secondarySettings, true);
                    streamWriter.Write(json);
                }
            }
        }

        public void loadSecondarySettings()
        {
            Log("Loading flamegen Settings");
            if (!File.Exists(secondarySettingsFilename))
                return;
            using (var fileStream = File.OpenRead(secondarySettingsFilename))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    modSecondarySettings = JsonUtility.FromJson<TSecondarySettings>(streamReader.ReadToEnd());
                }
            }
        }
    }
}
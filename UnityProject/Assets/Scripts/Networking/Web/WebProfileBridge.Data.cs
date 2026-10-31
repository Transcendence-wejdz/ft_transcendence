using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

// JSON shapes exchanged with the web page and backend.
public partial class WebProfileBridge
{
    [Serializable]
    private class WebProfile
    {
        public string userId;
        public string displayName;
        public string playerClass;
        public string color;
        public string settingsJson;
        public int gold;
        public string upgradesJson;
    }

    [Serializable]
    private class WebSettings
    {
        public bool showEnemyHealthBars;
        public bool screenShake;
        public bool showDamageNumbers;
        public bool showFps;
        public bool showPing;
        public string keybinds;
    }

    [Serializable]
    private class SelectionPatch
    {
        public string @class;
        public string color;
    }

    [Serializable]
    private class NamePatch
    {
        public string display_name;
    }

    [Serializable]
    private class SettingsPatch
    {
        public WebSettings settings;
    }
}

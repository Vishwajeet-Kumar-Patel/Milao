// <copyright file="GPGSAndroidSetupUI.cs" company="Google Inc.">
// Copyright (C) Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>

namespace GooglePlayGames.Editor
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Xml;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Google Play Game Services Setup dialog for Android.
    /// </summary>
    public class GPGSAndroidSetupUI : EditorWindow
    {
        // --- Helper to normalize any object (e.g., GPGSStrings entries) to string ---
        private static string S(object o) => o?.ToString() ?? string.Empty;

        /// <summary>The configuration data from the Play Console "resource data".</summary>
        private string mConfigData = string.Empty;

        /// <summary>The name of the class to generate containing the resource constants.</summary>
        private string mClassName = "GPGSIds";

        /// <summary>The scroll position.</summary>
        private Vector2 scroll;

        /// <summary>The directory for the constants class.</summary>
        private string mConstantDirectory = "Assets";

        /// <summary>The web client identifier.</summary>
        private string mWebClientId = string.Empty;

        // -------------------- Menu Items --------------------

        /// <summary>Menus the item for GPGS android setup.</summary>
        [MenuItem("Window/Google Play Games/Setup/Android setup...", false, 1)]
        public static void MenuItemFileGPGSAndroidSetup()
        {
            var window = GetWindow<GPGSAndroidSetupUI>(utility: true);

            // Defensive: GPGSStrings may store values as object/localized wrappers.
            var title = (GPGSStrings.AndroidSetup.Title as string)
                        ?? GPGSStrings.AndroidSetup.Title?.ToString()
                        ?? "Google Play Games - Android Setup";

            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(500, 400);
        }

        [MenuItem("Window/Google Play Games/Setup/Android setup...", true)]
        public static bool EnableAndroidMenuItem()
        {
#if UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        // -------------------- Automated Setup Entrypoints --------------------

        /// <summary>
        /// Performs setup using the Android resources downloaded XML string from the Play Console.
        /// </summary>
        /// <param name="clientId">The web client id.</param>
        /// <param name="classDirectory">Directory to write the constants file to.</param>
        /// <param name="className">Fully qualified class name for the resource Ids.</param>
        /// <param name="resourceXmlData">Resource XML data (as text).</param>
        /// <param name="nearbySvcId">Optional Nearby Connections serviceId.</param>
        public static bool PerformSetup(
            string clientId,
            string classDirectory,
            string className,
            string resourceXmlData,
            string nearbySvcId)
        {
            // If no resource XML but we have a Nearby service ID, fall back to appId flow.
            if (string.IsNullOrEmpty(resourceXmlData) && !string.IsNullOrEmpty(nearbySvcId))
            {
                return PerformSetup(
                    clientId,
                    GPGSProjectSettings.Instance.Get(GPGSUtil.APPIDKEY),
                    nearbySvcId);
            }

            if (ParseResources(classDirectory, className, resourceXmlData))
            {
                var settings = GPGSProjectSettings.Instance;
                settings.Set(GPGSUtil.CLASSDIRECTORYKEY, classDirectory);
                settings.Set(GPGSUtil.CLASSNAMEKEY, className);
                settings.Set(GPGSUtil.ANDROIDRESOURCEKEY, resourceXmlData);

                // Ensure bundle id matches what came from resources.
                CheckBundleId();

                // Resolve/refresh dependencies.
                GPGSUtil.CheckAndFixDependencies();
                GPGSUtil.CheckAndFixVersionedAssestsPaths();
                AssetDatabase.Refresh();

                // External Dependency Manager (VersionHandler) steps.
                Google.VersionHandler.VerboseLoggingEnabled = true;
                Google.VersionHandler.UpdateVersionedAssets(forceUpdate: true);
                Google.VersionHandler.Enabled = true;
                AssetDatabase.Refresh();

                

                // Continue with appId / clientId / nearby part.
                return PerformSetup(
                    clientId,
                    GPGSProjectSettings.Instance.Get(GPGSUtil.APPIDKEY),
                    nearbySvcId);
            }

            return false;
        }

        /// <summary>
        /// Provide static access to setup for facilitating automated builds.
        /// </summary>
        /// <param name="webClientId">OAuth2 web client id for the game.</param>
        /// <param name="appId">Google Play Games app id.</param>
        /// <param name="nearbySvcId">Optional Nearby Connections serviceId.</param>
        /// <returns>true if successful.</returns>
        public static bool PerformSetup(string webClientId, string appId, string nearbySvcId)
        {
            if (!string.IsNullOrEmpty(webClientId))
            {
                if (!GPGSUtil.LooksLikeValidClientId(webClientId))
                {
                    GPGSUtil.Alert(S(GPGSStrings.Setup.ClientIdError));
                    return false;
                }

                // Validate web client id belongs to same project/app as appId.
                string serverAppId = webClientId.Split('-')[0];
                if (!serverAppId.Equals(appId))
                {
                    GPGSUtil.Alert(S(GPGSStrings.Setup.AppIdMismatch));
                    return false;
                }
            }

            // Validate app id unless we're only configuring Nearby.
            if (!GPGSUtil.LooksLikeValidAppId(appId) && string.IsNullOrEmpty(nearbySvcId))
            {
                GPGSUtil.Alert(S(GPGSStrings.Setup.AppIdError));
                return false;
            }

            if (nearbySvcId != null)
            {
#if UNITY_ANDROID
                if (!NearbyConnectionUI.PerformSetup(nearbySvcId, true))
                {
                    return false;
                }
#endif
            }

            var settings = GPGSProjectSettings.Instance;
            settings.Set(GPGSUtil.APPIDKEY, appId);
            settings.Set(GPGSUtil.WEBCLIENTIDKEY, webClientId);
            settings.Save();
            GPGSUtil.UpdateGameInfo();

            // Check Android SDK presence.
            if (!GPGSUtil.HasAndroidSdk())
            {
                Debug.LogError("Android SDK not found.");
                EditorUtility.DisplayDialog(
                    S(GPGSStrings.AndroidSetup.SdkNotFound),
                    S(GPGSStrings.AndroidSetup.SdkNotFoundBlurb),
                    S(GPGSStrings.Ok));
                return false;
            }

            // Generate AndroidManifest.xml
            GPGSUtil.GenerateAndroidManifest();

            // Refresh assets, mark setup done.
            AssetDatabase.Refresh();
            settings.Set(GPGSUtil.ANDROIDSETUPDONEKEY, true);
            settings.Save();

            return true;
        }

        // -------------------- Editor Window Lifecycle --------------------

        public void OnEnable()
        {
            var settings = GPGSProjectSettings.Instance;
            mConstantDirectory = settings.Get(GPGSUtil.CLASSDIRECTORYKEY, mConstantDirectory);
            mClassName = settings.Get(GPGSUtil.CLASSNAMEKEY, mClassName);
            mConfigData = settings.Get(GPGSUtil.ANDROIDRESOURCEKEY);
            mWebClientId = settings.Get(GPGSUtil.WEBCLIENTIDKEY);
        }

        public void OnGUI()
        {
            GUI.skin.label.wordWrap = true;
            GUILayout.BeginVertical();

            GUIStyle link = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0f, 0f, 1f) } };

            GUILayout.Space(10);
            GUILayout.Label(S(GPGSStrings.AndroidSetup.Blurb));
            if (GUILayout.Button("Open Play Games Console", link, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("https://play.google.com/apps/publish");
            }

            // Divider line
            Rect last = GUILayoutUtility.GetLastRect();
            last.y += last.height - 2;
            last.x += 3;
            last.width -= 6;
            last.height = 2;
            GUI.Box(last, string.Empty);

            GUILayout.Space(15);
            GUILayout.Label("Constants class name", EditorStyles.boldLabel);
            GUILayout.Label("Enter the fully qualified name of the class to create containing the constants");
            GUILayout.Space(10);

            mConstantDirectory = EditorGUILayout.TextField(
                "Directory to save constants",
                mConstantDirectory,
                GUILayout.MinWidth(480));

            mClassName = EditorGUILayout.TextField(
                "Constants class name",
                mClassName,
                GUILayout.MinWidth(480));

            GUILayout.Label("Resources Definition", EditorStyles.boldLabel);
            GUILayout.Label("Paste in the Android Resources from the Play Console");
            GUILayout.Space(10);

            scroll = GUILayout.BeginScrollView(scroll);
            mConfigData = EditorGUILayout.TextArea(
                mConfigData ?? string.Empty,
                GUILayout.MinWidth(475),
                GUILayout.Height(Screen.height));
            GUILayout.EndScrollView();
            GUILayout.Space(10);

            // Client ID field
            GUILayout.Label(S(GPGSStrings.Setup.WebClientIdTitle), EditorStyles.boldLabel);
            GUILayout.Label(S(GPGSStrings.AndroidSetup.WebClientIdBlurb));

            mWebClientId = EditorGUILayout.TextField(
                S(GPGSStrings.Setup.ClientId),
                mWebClientId ?? string.Empty,
                GUILayout.MinWidth(450));

            GUILayout.Space(10);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(S(GPGSStrings.Setup.SetupButton), GUILayout.Width(100)))
            {
                try
                {
                    if (GPGSUtil.LooksLikeValidPackageName(mClassName))
                    {
                        DoSetup();
                        return;
                    }
                    else
                    {
                        GPGSUtil.Alert(S(GPGSStrings.Error), "Invalid classname: Must be a valid package/class name.");
                    }
                }
                catch (Exception e)
                {
                    GPGSUtil.Alert(S(GPGSStrings.Error), "Invalid classname: " + e.Message);
                }
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.EndVertical();
        }

        // -------------------- Actions --------------------

        /// <summary>Starts the setup process.</summary>
        public void DoSetup()
        {
            if (PerformSetup(mWebClientId, mConstantDirectory, mClassName, mConfigData, null))
            {
                CheckBundleId();

                EditorUtility.DisplayDialog(
                    S(GPGSStrings.Success),
                    S(GPGSStrings.AndroidSetup.SetupComplete),
                    S(GPGSStrings.Ok));

                GPGSProjectSettings.Instance.Set(GPGSUtil.ANDROIDSETUPDONEKEY, true);
                Close();
            }
            else
            {
                GPGSUtil.Alert(
                    S(GPGSStrings.Error),
                    "Invalid or missing XML resource data. Make sure the data is valid and contains the app_id element.");
            }
        }

        /// <summary>
        /// Checks the bundle identifier and aligns it with the resource package if needed.
        /// </summary>
        public static void CheckBundleId()
        {
            string packageName = GPGSProjectSettings.Instance.Get(GPGSUtil.ANDROIDBUNDLEIDKEY, string.Empty);
            string currentId;
#if UNITY_5_6_OR_NEWER
            currentId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
#else
            currentId = PlayerSettings.bundleIdentifier;
#endif
            if (!string.IsNullOrEmpty(packageName))
            {
                if (string.IsNullOrEmpty(currentId) || currentId == "com.Company.ProductName")
                {
#if UNITY_5_6_OR_NEWER
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);
#else
                    PlayerSettings.bundleIdentifier = packageName;
#endif
                }
                else if (currentId != packageName)
                {
                    if (EditorUtility.DisplayDialog(
                        "Set Bundle Identifier?",
                        "The server configuration is using " + packageName +
                        ", but the player settings is set to " + currentId +
                        ".\nSet the Bundle Identifier to " + packageName + "?",
                        "OK",
                        "Cancel"))
                    {
#if UNITY_5_6_OR_NEWER
                        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);
#else
                        PlayerSettings.bundleIdentifier = packageName;
#endif
                    }
                }
            }
            else
            {
                Debug.Log("NULL package!!");
            }
        }

        /// <summary>
        /// Parses the resources XML and sets properties; also generates the constants file.
        /// </summary>
        private static bool ParseResources(string classDirectory, string className, string res)
        {
            if (string.IsNullOrEmpty(res))
            {
                return false;
            }

#pragma warning disable 618
            var reader = new XmlTextReader(new StringReader(res));
#pragma warning restore 618

            bool inResource = false;
            string lastProp = null;
            Hashtable resourceKeys = new Hashtable();
            string appId = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "resources")
                {
                    inResource = true;
                }

                if (inResource && reader.NodeType == XmlNodeType.Element && reader.Name == "string")
                {
                    lastProp = reader.GetAttribute("name");
                }
                else if (inResource && !string.IsNullOrEmpty(lastProp) && reader.NodeType == XmlNodeType.Text)
                {
                    if (reader.HasValue)
                    {
                        if (lastProp == "app_id")
                        {
                            appId = reader.Value;
                            GPGSProjectSettings.Instance.Set(GPGSUtil.APPIDKEY, appId);
                        }
                        else if (lastProp == "package_name")
                        {
                            GPGSProjectSettings.Instance.Set(GPGSUtil.ANDROIDBUNDLEIDKEY, reader.Value);
                        }
                        else
                        {
                            resourceKeys[lastProp] = reader.Value;
                        }

                        lastProp = null;
                    }
                }
            }

            reader.Close();

            if (resourceKeys.Count > 0)
            {
                GPGSUtil.WriteResourceIds(classDirectory, className, resourceKeys);
            }

            return appId != null;
        }
    }
}

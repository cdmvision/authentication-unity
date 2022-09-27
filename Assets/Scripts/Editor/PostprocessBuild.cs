using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.iOS.Xcode;
#endif

namespace Editor
{
    public class PostprocessBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS
            if (report.summary.platform != BuildTarget.iOS)
                return;
            
            if (AuthConfigurationLoader.TryLoad(out var configuration))
            {
                var plistPath = report.summary.outputPath + "/Info.plist";
                var plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));
                
                // Register ios URL scheme for external apps to launch this app.
                var urlTypes = plist.root.CreateArray("CFBundleURLTypes");
                var urlTypeDict = urlTypes.AddDict();
                urlTypeDict.SetString("CFBundleURLName", "");
                urlTypeDict.SetString("CFBundleTypeRole", "Editor");

                var urlSchemes = urlTypeDict.CreateArray("CFBundleURLSchemes");
                urlSchemes.AddString(new Uri(configuration.redirectUri).Scheme);

                // Save all changes.
                File.WriteAllText(plistPath, plist.WriteToString());
            }
#endif
        }
    }
}
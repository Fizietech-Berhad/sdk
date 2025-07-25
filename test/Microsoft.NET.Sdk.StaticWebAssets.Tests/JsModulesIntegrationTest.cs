﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.StaticWebAssets.Tasks;

namespace Microsoft.NET.Sdk.StaticWebAssets.Tests
{
    public class JsModulesIntegrationTest(ITestOutputHelper log) : IsolatedNuGetPackageFolderAspNetSdkBaselineTest(log, nameof(JsModulesIntegrationTest))
    {
        [Fact]
        public void Build_NoOps_WhenJsModulesIsDisabled()
        {
            var testAsset = "RazorComponentApp";
            var projectDirectory = CreateAspNetSdkTestAsset(testAsset);

            Directory.CreateDirectory(Path.Combine(projectDirectory.TestRoot, "wwwroot"));
            File.WriteAllText(Path.Combine(projectDirectory.TestRoot, "wwwroot", "ComponentApp.lib.module.js"), "console.log('Hello world!');");

            var build = CreateBuildCommand(projectDirectory);
            ExecuteCommand(build, "/p:JsModulesEnabled=false").Should().Pass();

            var intermediateOutputPath = Path.Combine(build.GetBaseIntermediateDirectory().ToString(), "Debug", DefaultTfm);

            new FileInfo(Path.Combine(intermediateOutputPath, "jsmodules", "jsmodules.build.manifest.json")).Should().NotExist();
        }

        [Fact]
        public void Build_GeneratesManifestWhenItFindsALibrary()
        {
            var testAsset = "RazorComponentApp";
            var projectDirectory = CreateAspNetSdkTestAsset(testAsset)
                .WithProjectChanges(p => {
                     var fingerprintContent = p.Descendants()
                         .SingleOrDefault(e => e.Name.LocalName == "StaticWebAssetsFingerprintContent");
                     fingerprintContent.Value = "true";
                 });

            Directory.CreateDirectory(Path.Combine(projectDirectory.TestRoot, "wwwroot"));
            File.WriteAllText(Path.Combine(projectDirectory.TestRoot, "wwwroot", "ComponentApp.lib.module.js"), "console.log('Hello world!');");

            var build = CreateBuildCommand(projectDirectory);
            ExecuteCommand(build).Should().Pass();

            var intermediateOutputPath = Path.Combine(build.GetBaseIntermediateDirectory().ToString(), "Debug", DefaultTfm);

            var file = new FileInfo(Path.Combine(intermediateOutputPath, "jsmodules", "jsmodules.build.manifest.json"));
            file.Should().Exist();
            file.Should().Match("""ComponentApp\.[a-zA-Z-0-9]{10}\.lib\.module\.js""");
        }

        [Fact]
        public void Build_DiscoversJsModulesBasedOnPatterns()
        {
            var testAsset = "RazorComponentApp";
            ProjectDirectory = CreateAspNetSdkTestAsset(testAsset);

            // Components
            CreateFile("", ProjectDirectory.TestRoot, "Components", "Pages", "Counter.razor.js");

            // MVC | Razor pages
            CreateFile("", ProjectDirectory.TestRoot, "Pages", "Index.cshtml");
            CreateFile("", ProjectDirectory.TestRoot, "Pages", "Index.cshtml.js");

            var build = CreateBuildCommand(ProjectDirectory);
            ExecuteCommand(build).Should().Pass();

            var intermediateOutputPath = build.GetIntermediateDirectory(DefaultTfm, "Debug").ToString();
            var outputPath = build.GetOutputDirectory(DefaultTfm, "Debug").ToString();

            var finalPath = Path.Combine(intermediateOutputPath, "staticwebassets.build.json");
            var buildManifest = StaticWebAssetsManifest.FromJsonBytes(File.ReadAllBytes(finalPath));
            AssertManifest(
                buildManifest,
                LoadBuildManifest());

            buildManifest.Should().NotBeNull();
            buildManifest.DiscoveryPatterns.Should().BeEmpty();

            AssertBuildAssets(
                buildManifest,
                outputPath,
                intermediateOutputPath);
        }

        [Fact]
        public void Publish_PublishesJsModuleBundleBundleToTheRightLocation()
        {
            var testAsset = "RazorComponentApp";
            ProjectDirectory = CreateAspNetSdkTestAsset(testAsset)
                .WithProjectChanges(p => {
                    var fingerprintContent = p.Descendants()
                        .SingleOrDefault(e => e.Name.LocalName == "StaticWebAssetsFingerprintContent");
                    fingerprintContent.Value = "true";
                });
            Directory.CreateDirectory(Path.Combine(ProjectDirectory.TestRoot, "wwwroot"));
            File.WriteAllText(Path.Combine(ProjectDirectory.TestRoot, "wwwroot", "ComponentApp.lib.module.js"), "console.log('Hello world!');");

            var publish = CreatePublishCommand(ProjectDirectory);
            var publishResult = ExecuteCommand(publish);
            publishResult.Should().Pass();

            var outputPath = publish.GetOutputDirectory(DefaultTfm).ToString();
            var intermediateOutputPath = publish.GetIntermediateDirectory(DefaultTfm, "Debug").ToString();

            var path = Path.Combine(intermediateOutputPath, "staticwebassets.publish.json");
            new FileInfo(path).Should().Exist();
            var manifest = StaticWebAssetsManifest.FromJsonBytes(File.ReadAllBytes(path));
            AssertManifest(manifest, LoadPublishManifest());

            AssertPublishAssets(
                manifest,
                outputPath,
                intermediateOutputPath);
        }

        [Fact]
        public void Publish_DoesNotPublishAnyFile_WhenThereAreNoJsModulesFiles()
        {
            var testAsset = "RazorComponentApp";
            var projectDirectory = CreateAspNetSdkTestAsset(testAsset);

            var publish = CreatePublishCommand(projectDirectory);
            ExecuteCommand(publish).Should().Pass();

            var publishOutputPath = publish.GetOutputDirectory(DefaultTfm, "Debug").ToString();

            new FileInfo(Path.Combine(publishOutputPath, "wwwroot", "ComponentApp.lib.module.js")).Should().NotExist();
            new FileInfo(Path.Combine(publishOutputPath, "wwwroot", "ComponentApp.modules.json")).Should().NotExist();
        }

        [Fact]
        public void Does_Nothing_WhenThereAreNoJsModulesFiles()
        {
            var testAsset = "RazorComponentApp";
            var projectDirectory = CreateAspNetSdkTestAsset(testAsset);

            var build = CreateBuildCommand(projectDirectory);
            ExecuteCommand(build).Should().Pass();

            var intermediateOutputPath = Path.Combine(build.GetBaseIntermediateDirectory().ToString(), "Debug", DefaultTfm);

            var file = new FileInfo(Path.Combine(intermediateOutputPath, "jsmodules", "jsmodules.build.manifest.json"));
            file.Should().NotExist();
        }

        [Fact]
        public void Build_JsModules_IsIncremental()
        {
            // Arrange
            var thumbprintLookup = new Dictionary<string, FileThumbPrint>();

            var testAsset = "RazorComponentApp";
            var projectDirectory = CreateAspNetSdkTestAsset(testAsset);

            Directory.CreateDirectory(Path.Combine(projectDirectory.TestRoot, "wwwroot"));
            File.WriteAllText(Path.Combine(projectDirectory.TestRoot, "wwwroot", "ComponentApp.lib.module.js"), "console.log('Hello world!');");

            // Act & Assert 1
            var build = CreateBuildCommand(projectDirectory);
            ExecuteCommand(build).Should().Pass();

            var intermediateOutputPath = Path.Combine(build.GetBaseIntermediateDirectory().ToString(), "Debug", DefaultTfm);
            var directoryPath = Path.Combine(intermediateOutputPath, "jsmodules");

            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var thumbprint = FileThumbPrint.Create(file);
                thumbprintLookup[file] = thumbprint;
            }

            // Act & Assert 2
            for (var i = 0; i < 2; i++)
            {
                build = CreateBuildCommand(projectDirectory);
                ExecuteCommand(build).Should().Pass();

                foreach (var file in files)
                {
                    var thumbprint = FileThumbPrint.Create(file);
                    Assert.Equal(thumbprintLookup[file], thumbprint);
                }
            }
        }

        private void CreateFile(string content, params string[] path)
        {
            Directory.CreateDirectory(Path.Combine(path[..^1].Prepend(ProjectDirectory.TestRoot).ToArray()));
            File.WriteAllText(Path.Combine(path.Prepend(ProjectDirectory.TestRoot).ToArray()), content);
        }
    }

    public class JsModulesPackagesIntegrationTest(ITestOutputHelper log) : IsolatedNuGetPackageFolderAspNetSdkBaselineTest(log, nameof(JsModulesPackagesIntegrationTest))
    {
        [Fact]
        public void BuildProjectWithReferences_IncorporatesInitializersFromClassLibraries()
        {
            var testAsset = "RazorAppWithPackageAndP2PReference";
            ProjectDirectory = CreateAspNetSdkTestAsset(testAsset);

            EnsureLocalPackagesExists();

            var restore = CreateRestoreCommand(ProjectDirectory, "AppWithPackageAndP2PReference");
            ExecuteCommand(restore).Should().Pass();

            CreateFile("console.log('Hello world AnotherClassLib')", "AnotherClassLib", "wwwroot", "AnotherClassLib.lib.module.js");
            CreateFile("console.log('Hello world ClassLibrary')", "ClassLibrary", "wwwroot", "ClassLibrary.lib.module.js");

            var build = CreateBuildCommand(ProjectDirectory, "AppWithPackageAndP2PReference");
            ExecuteCommand(build).Should().Pass();

            var intermediateOutputPath = build.GetIntermediateDirectory(DefaultTfm, "Debug").ToString();
            var outputPath = build.GetOutputDirectory(DefaultTfm, "Debug").ToString();

            var finalPath = Path.Combine(intermediateOutputPath, "staticwebassets.build.json");
            var manifest = StaticWebAssetsManifest.FromJsonBytes(File.ReadAllBytes(finalPath));
            AssertManifest(
                manifest,
                LoadBuildManifest());

            AssertBuildAssets(
                manifest,
                outputPath,
                intermediateOutputPath);

            var file = new FileInfo(Path.Combine(intermediateOutputPath, "jsmodules", "jsmodules.build.manifest.json"));
            file.Should().Exist();
            file.Should().Contain("_content/AnotherClassLib/AnotherClassLib.lib.module.js");
            file.Should().Contain("_content/ClassLibrary/ClassLibrary.lib.module.js");
        }

        [Fact]
        public void PublishProjectWithReferences_IncorporatesInitializersFromClassLibrariesAndPublishesAssetsToTheRightLocation()
        {
            var testAsset = "RazorAppWithPackageAndP2PReference";
            ProjectDirectory = CreateAspNetSdkTestAsset(testAsset);

            EnsureLocalPackagesExists();

            var restore = CreateRestoreCommand(ProjectDirectory, "AppWithPackageAndP2PReference");
            ExecuteCommand(restore).Should().Pass();

            CreateFile("console.log('Hello world AnotherClassLib')", "AnotherClassLib", "wwwroot", "AnotherClassLib.lib.module.js");

            // Notice that it does not follow the pattern $(PackageId).lib.module.js
            CreateFile("console.log('Hello world ClassLibrary')", "ClassLibrary", "wwwroot", "AnotherClassLib.lib.module.js");

            var publish = CreatePublishCommand(ProjectDirectory, "AppWithPackageAndP2PReference");
            ExecuteCommand(publish).Should().Pass();

            var intermediateOutputPath = publish.GetIntermediateDirectory(DefaultTfm, "Debug").ToString();
            var outputPath = publish.GetOutputDirectory(DefaultTfm, "Debug").ToString();

            var path = Path.Combine(intermediateOutputPath, "staticwebassets.build.json");
            var buildManifest = StaticWebAssetsManifest.FromJsonBytes(File.ReadAllBytes(path));
            AssertManifest(
                buildManifest,
                LoadBuildManifest());

            var finalPath = Path.Combine(intermediateOutputPath, "staticwebassets.publish.json");
            var publishManifest = StaticWebAssetsManifest.FromJsonBytes(File.ReadAllBytes(finalPath));
            AssertManifest(
                publishManifest,
                LoadPublishManifest());

            AssertPublishAssets(
                publishManifest,
                outputPath,
                intermediateOutputPath);

            var file = new FileInfo(Path.Combine(outputPath, "wwwroot", "AppWithPackageAndP2PReference.modules.json"));
            file.Should().Exist();
            file.Should().Contain("_content/AnotherClassLib/AnotherClassLib.lib.module.js");
            file.Should().NotContain("_content/ClassLibrary/AnotherClassLib.lib.module.js");
        }

        [Fact]
        public void PublishProjectWithReferences_DifferentBuildAndPublish_LibraryInitializers()
        {
            var testAsset = "RazorAppWithPackageAndP2PReference";
            ProjectDirectory = CreateAspNetSdkTestAsset(testAsset);

            EnsureLocalPackagesExists();

            var restore = CreateRestoreCommand(ProjectDirectory, "AppWithPackageAndP2PReference");
            ExecuteCommand(restore).Should().Pass();

            CreateFile("console.log('Hello world AnotherClassLib publish')", "AnotherClassLib", "wwwroot", "AnotherClassLib.lib.module.js");
            CreateFile("console.log('Hello world AnotherClassLib')", "AnotherClassLib", "wwwroot", "AnotherClassLib.lib.module.build.js");
            ProjectDirectory.WithProjectChanges((project, document) =>
            {
                if (project.EndsWith("AnotherClassLib.csproj"))
                {
                    document.Root.Add(new XElement("ItemGroup",
                        new XElement("Content",
                            new XAttribute("Update", "wwwroot\\AnotherClassLib.lib.module.build.js"),
                            new XAttribute("CopyToPublishDirectory", "Never"),
                            new XAttribute("TargetPath", "wwwroot\\AnotherClassLib.lib.module.js"))));
                }
            });
            var publish = CreatePublishCommand(ProjectDirectory, "AppWithPackageAndP2PReference");
            ExecuteCommand(publish).Should().Pass();

            var intermediateOutputPath = publish.GetIntermediateDirectory(DefaultTfm, "Debug").ToString();
            var outputPath = publish.GetOutputDirectory(DefaultTfm, "Debug").ToString();

            var path = Path.Combine(intermediateOutputPath, "staticwebassets.build.json"); ;
            var buildManifest = StaticWebAssetsManifest.FromJsonBytes(File.ReadAllBytes(path));

            var initializers = buildManifest.Assets.Where(a => a.RelativePath == "AnotherClassLib.lib.module.js");
            initializers.Should().HaveCount(1);
            initializers.Should().Contain(a => a.IsBuildOnly());

            AssertManifest(
                buildManifest,
                LoadBuildManifest());

            var finalPath = Path.Combine(intermediateOutputPath, "staticwebassets.publish.json");
            var manifest = StaticWebAssetsManifest.FromJsonBytes(File.ReadAllBytes(finalPath));
            AssertManifest(
                manifest,
                LoadPublishManifest());

            AssertBuildAssets(
                manifest,
                outputPath,
                intermediateOutputPath);

            var modulesManifest = new FileInfo(Path.Combine(outputPath, "wwwroot", "AppWithPackageAndP2PReference.modules.json"));
            modulesManifest.Should().Exist();
            modulesManifest.Should().Contain("_content/AnotherClassLib/AnotherClassLib.lib.module.js");
            modulesManifest.Should().NotContain("_content/ClassLibrary/AnotherClassLib.lib.module.js");

            var moduleFile = new FileInfo(Path.Combine(outputPath, "wwwroot", "_content", "AnotherClassLib", "AnotherClassLib.lib.module.js"));
            moduleFile.Should().Exist();
            moduleFile.Should().Contain("console.log('Hello world AnotherClassLib publish')");
        }

        private void CreateFile(string content, params string[] path)
        {
            Directory.CreateDirectory(Path.Combine(path[..^1].Prepend(ProjectDirectory.TestRoot).ToArray()));
            File.WriteAllText(Path.Combine(path.Prepend(ProjectDirectory.TestRoot).ToArray()), content);
        }
    }
}

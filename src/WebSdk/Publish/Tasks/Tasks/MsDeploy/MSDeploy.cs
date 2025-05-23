﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.Sdk.Publish.Tasks.Properties;
using CultureInfo = System.Globalization.CultureInfo;
using Framework = Microsoft.Build.Framework;
using Utilities = Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.Publish.Tasks.MsDeploy
{
    /// <summary>
    /// The MSDeploy task, which is a wrapper around msdeploy.exe
    /// </summary>
    public class MSDeploy : Utilities.ToolTask
    {
        /* 
         *Microsoft (R) Web Deployment Command Line Tool (MSDeploy.exe)
            Version 7.1.495.0
            Copyright (c) Microsoft Corporation. All rights reserved.

            MSDeploy.exe <-verb:<name>> <-source:<object>> [-dest:<object>] [args ...]

              -verb:<name>                   Action to perform (required).
              -source:<object>               The source object for the operation 
                                             (required).
              -dest:<object>                 The destination object for the operation. 
              -declareParam:<parms>          Declares a parameter for synchronization.
              -setParam:<parms>              Sets a parameter for synchronization.
              -disableLink:<name>            Disables the specified link extension(s).
              -enableLink:<name>             Enables the specified link extension(s).
              -disableRule:<name>            Disables the specified synchronization 
                                             rule(s).
              -enableRule:<name>             Enables the specified synchronization rule(s).
              -replace:<arg settings>        Specifies an attribute replacement rule.
              -retryAttempts                 The number of times a provider will retry 
                                             after a failed action (not all providers 
                                             support retrying). Defaults to 5.
              -retryInterval                 Interval in milliseconds between retry 
                                             attempts (-retryAttempts). The default is 
                                             1000.
              -skip:<arg settings>           Specifies an object to skip during 
                                             synchronization.
              -disableSkipDirective:<name>   Disables the specified skip directive.
              -enableSkipDirective:<name>    Enables the specified skip directive.
              -useAdminShares                When possible, use UNC admin shares for file 
                                             synchronization.
              -verbose                       Enables more verbose output.
              -whatif                        Displays what would have happened without 
                                             actually performing any operations.
              -xpath:<path>                  An XPath expression to apply to XML output.
              -xml                           Return results in XML format.
              -allowUntrusted                Allow untrusted server certificate when using 
                                             SSL.
              -showSecure                    Show secure attributes in XML output instead 
                                             of hiding them.^
              -preSync:<command>             A command to execute before the 
                                             synchronization on the destination.  For 
                                             instance, net stop a service.
              -postSync:<command>            A command to execute after the 
                                             synchronization on the destination.  For 
                                             instance, net start a service.
         * 
         * 
         *         // not documented, part of IISExpress
              public const string AppHostConfigDirectory = "-appHostConfigDir";
         *    public const string WebServerDirectory = "-webServerDir";
              public const string WebServerManifest = "-webServerManifest";


              
            Supported Verbs:

              dump                           Displays the details of the specified source 
                                             object.
              migrate                        Migrates the source object to the destination 
                                             object.
              sync                           Synchronizes the destination object with the 
                                             source object.
              delete                         Deletes specified destination object.
              getDependencies                Retrieve dependencies for given object
              getParameters                  Return parameters supported by object
              getSystemInfo                  Retrieve system information associated with 
                                             given object

            <object> format:

              provider-type=[provider-path],[provider settings],...
              
            Supported provider-types (and sample paths, if applicable):

              appHostConfig                  IIS 7+ configuration
              appHostSchema                  IIS 7+ configuration schema
              appPoolConfig                  IIS 7+ Application Pool
              archiveDir                     Archive directory
              auto                           Automatic destination
              cert                           Certificate
              comObject32                    32-bit COM object
              comObject64                    64-bit COM object
              contentPath                    File System Content
              dbFullSql                      Deploy SQL database
              dbMySql                        Deploy MySql database
              delete                         Special source-only provider used to delete a 
                                             given destination.
              fcgiExtConfig                  FcgiExt.ini settings or fastCgi section 
                                             configuration
              gacAssembly                    GAC assembly
              iisApp                         Web Application
              machineConfig32                .NET 32-bit machine configuration
              machineConfig64                .NET 64-bit machine configuration
              manifest                       Custom manifest file
              metaKey                        Metabase key
              package                        A .zip file package
              regKey                         Registry key
              regValue                       Registry value
              rootWebConfig32                .NET 32-bit root Web configuration
              rootWebConfig64                .NET 64-bit root Web configuration
              runCommand                     Runs a command on the destination when sync 
                                             is called.
              setAcl                         Grant permissions
              urlScanConfig                  UrlScan.ini settings or requestFiltering 
                                             section configuration
              webServer                      Full IIS 7+ Web server
              webServer60                    Full IIS 6.0 Web server


            Common settings (can be used with all providers):

                computerName=<name>       Name of remote computer or proxy-URL
                wmsvc=<name>              Name of remote computer or proxy-URL for the Web 
                                          Management Service (WMSvc). Assumes that the 
                                          service is listening on port 8172.
                authtype=<name>           Authentication scheme to use. NTLM is the 
                                          default setting. If the wmsvc option is 
                                          specified, then Basic is the default setting.
                userName=<name>           User name to authenticate for remote connections 
                                          (required if using Basic authentication).
                password=<password>       Password of the user for remote connections 
                                          (required if using Basic authentication).
                encryptPassword=<pwd>     Password to use for encrypting/decrypting any 
                                          secure data.
                includeAcls=<bool>        If true, include ACLs in the operation (applies 
                                          to the file system, registry, and metabase).
                useStatusRequest=<bool>   Controls whether remote destination 
                                          synchronization status should appear 
                                          immediately. The default setting is true.
                tempAgent=<bool>          Temporarily install the remote agent for the 
                                          duration of a remote operation
                
    
        */

        private string? m_exePath;
        private string? m_disableRule;
        private string? m_verb;
        private string? m_failureLevel;
        private string? m_xpath;
        private string? m_enableRule;
        private string? m_replace;
        private string? m_skip;
        private string? m_disableLink;
        private string? m_enableLink;
        private string? m_disableSkipDirective;
        private string? m_enableSkipDirective;
        private string? m_lastCommandLine;
        private bool m_xml;
        private bool m_whatif;
        private bool m_useChecksum;
        private bool m_verbose;
        private bool m_allowUntrusted;
        private bool m_enableTransaction;
        private int m_retryAttempts;
        private int m_retryInterval;
        private bool m_useDoubleQuoteForValue = false;
        private string? m_strValueQuote = null;

        //public const string AppHostConfigDirectory = "-appHostConfigDir";
        // *    public const string WebServerDirectory = "-webServerDir";
        //      public const string WebServerManifest = "-webServerManifest";

        public string? WebServerAppHostConfigDirectory { get; set; }
        public string? WebServerDirectory { get; set; }
        public string? WebServerManifest { get; set; }

        private Framework.ITaskItem[]? m_sourceITaskItem = null;
        private Framework.ITaskItem[]? m_destITaskItem = null;
        private Framework.ITaskItem[]? m_replaceRuleItemsITaskItem = null;
        private Framework.ITaskItem[]? m_skipRuleItemsITaskItem = null;
        private Framework.ITaskItem[]? m_declareParameterItems = null;
        private Framework.ITaskItem[]? m_importDeclareParametersItems = null;
        private Framework.ITaskItem[]? m_simpleSetParameterItems = null;
        private Framework.ITaskItem[]? m_importSetParametersItems = null;
        private Framework.ITaskItem[]? m_setParameterItems = null;

        private bool m_previewOnly = false;

        public class Provider
        {
            public static readonly string Unknown = "Unknown";
            public static readonly string Package = "Package";
            public static readonly string ArchiveDir = "ArchiveDir";
            public static readonly string DbDacFx = "DbDacFx";
            public static readonly string MetaKey = "MetaKey";
            public static readonly string AppHostConfig = "AppHostConfig";
            public static readonly string DBFullSql = "DBFullSql";
            public static readonly string DBCodeFirst = "DBCodeFirst";
        }

        public class TypeName
        {
            public static readonly string DeploymentWellKnownProvider = "Microsoft.Web.Deployment.DeploymentWellKnownProvider";
            public static readonly string DeploymentEncryptionException = "Microsoft.Web.Deployment.DeploymentEncryptionException";
            public static readonly string DeploymentException = "Microsoft.Web.Deployment.DeploymentException";
        }

        public class Extensions
        {
            public static readonly string DbFullSql = ".sql";
            public static readonly string DbDacFx = ".dacpac";
        }

        /// <summary>
        /// Location for the MSdeploy.exe path
        /// </summary>
        public string? ExePath
        {
            get
            {
#if NET472
                if (string.IsNullOrEmpty(m_exePath))
                {
                    // if path is not set, we optimize to latest version of msdeploy
                    using (Win32.RegistryKey registryKey = Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\IIS Extensions\MSDeploy"))
                    {
                        if (registryKey != null)
                        {
                            string[] strVersions = registryKey.GetSubKeyNames();
                            if (strVersions != null)
                            {
                                int [] versions = registryKey.GetSubKeyNames().Select(p => Convert.ToInt32(p)).ToArray();
                                Array.Sort(versions);

                                for (int i = versions.Length -1; i >= 0; i--)
                                {
                                    int version = versions[i];
                                    using (Win32.RegistryKey versionRegistry = registryKey.OpenSubKey(version.ToString(CultureInfo.InvariantCulture)))
                                    {
                                        if (versionRegistry != null)
                                        {
                                            m_exePath = versionRegistry.GetValue(@"InstallPath").ToString();
                                            if (!string.IsNullOrEmpty(m_exePath))
                                            {
                                                // found the valid m_exePath
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
#else
                if (string.IsNullOrEmpty(m_exePath))
                {
                    string programFiles = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");
                    string msdeployExePath = Path.Combine("IIS", "Microsoft Web Deploy V3");
                    m_exePath = Path.Combine(programFiles, msdeployExePath);
                    if (!File.Exists(Path.Combine(m_exePath, ToolName)))
                    {
                        /// On 32-bit Operating Systems, this will return C:\Program Files
                        /// On 64-bit Operating Systems - regardless of process bitness, this will return C:\Program Files
                        if (!Environment.Is64BitOperatingSystem || Environment.Is64BitProcess)
                        {
                            programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                            m_exePath = Path.Combine(programFiles, msdeployExePath);
                        }
                        else
                        {
                            // 32 bit process on a 64 bit OS can't use SpecialFolder.ProgramFiles to get the 64-bit program files folder
                            programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                            m_exePath = Path.Combine(programFiles, msdeployExePath);
                        }
                    }
                }
#endif
                return m_exePath;
            }
            set { m_exePath = value; }
        }

        public string? DisableRule
        {
            get { return m_disableRule; }
            set { m_disableRule = value; }
        }

        [Framework.Required]
        public string? Verb
        {
            get { return m_verb; }
            set { m_verb = value; }
        }

        [Framework.Required]
        public Framework.ITaskItem[]? Source
        {
            get { return m_sourceITaskItem; }
            set { m_sourceITaskItem = value; }
        }

        public Framework.ITaskItem[]? Destination
        {
            get { return m_destITaskItem; }
            set { m_destITaskItem = value; }
        }
        public bool WhatIf
        {
            get { return m_whatif; }
            set { m_whatif = value; }
        }

        public bool OptimisticParameterDefaultValue { get; set; }

        public bool UseChecksum
        {
            get { return m_useChecksum; }
            set { m_useChecksum = value; }
        }

        public bool AllowUntrusted
        {
            get { return m_allowUntrusted; }
            set { m_allowUntrusted = value; }
        }

        public bool Verbose
        {
            get { return m_verbose; }
            set { m_verbose = value; }
        }
        public string? FailureLevel
        {
            get { return m_failureLevel; }
            set { m_failureLevel = value; }
        }
        public bool Xml
        {
            get { return m_xml; }
            set { m_xml = value; }
        }
        public string? XPath
        {
            get { return m_xpath; }
            set { m_xpath = value; }
        }
        public string? EnableRule
        {
            get { return m_enableRule; }
            set { m_enableRule = value; }
        }
        public string? Replace
        {
            get { return m_replace; }
            set { m_replace = value; }
        }
        public string? Skip
        {
            get { return m_skip; }
            set { m_skip = value; }
        }
        public string? DisableLink
        {
            get { return m_disableLink; }
            set { m_disableLink = value; }
        }

        public string? EnableLink
        {
            get { return m_enableLink; }
            set { m_enableLink = value; }
        }

        public bool EnableTransaction
        {
            get { return m_enableTransaction; }
            set { m_enableTransaction = value; }
        }
        public int RetryAttempts
        {
            get { return m_retryAttempts; }
            set { m_retryAttempts = value; }
        }
        public int RetryInterval
        {
            get { return m_retryInterval; }
            set { m_retryInterval = value; }
        }

        public bool UseDoubleQuoteForValue
        {
            get { return m_useDoubleQuoteForValue; }
            set
            {
                m_useDoubleQuoteForValue = value;
                m_strValueQuote = (m_useDoubleQuoteForValue) ? "\"" : null;
            }
        }

        public Framework.ITaskItem[]? ReplaceRuleItems
        {
            get { return m_replaceRuleItemsITaskItem; }
            set { m_replaceRuleItemsITaskItem = value; }
        }

        public Framework.ITaskItem[]? SkipRuleItems
        {
            get { return m_skipRuleItemsITaskItem; }
            set { m_skipRuleItemsITaskItem = value; }
        }

        public string? DisableSkipDirective
        {
            get { return m_disableSkipDirective; }
            set { m_disableSkipDirective = value; }
        }

        public string? EnableSkipDirective
        {
            get { return m_enableSkipDirective; }
            set { m_enableSkipDirective = value; }
        }

        public Framework.ITaskItem[]? DeclareParameterItems
        {
            get { return m_declareParameterItems; }
            set { m_declareParameterItems = value; }
        }
        public Framework.ITaskItem[]? ImportDeclareParametersItems
        {
            get { return m_importDeclareParametersItems; }
            set { m_importDeclareParametersItems = value; }
        }

        public Framework.ITaskItem[]? ImportSetParametersItems
        {
            get { return m_importSetParametersItems; }
            set { m_importSetParametersItems = value; }
        }

        public Framework.ITaskItem[]? SimpleSetParameterItems
        {
            get { return m_simpleSetParameterItems; }
            set { m_simpleSetParameterItems = value; }
        }

        public Framework.ITaskItem[]? SetParameterItems
        {
            get { return m_setParameterItems; }
            set { m_setParameterItems = value; }
        }

        public Framework.ITaskItem[]? AdditionalDestinationProviderOptions { get; set; }

        private string? _userAgent;
        public string? UserAgent
        {
            get { return _userAgent; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _userAgent = Utility.GetFullUserAgentString(value);
                }
            }
        }

        [Framework.Output]
        public string CommandLine
        {
            get
            {
                return string.Concat("\"", GenerateFullPathToTool(), "\" ", GenerateCommandLineCommands());
            }
        }

        [Framework.Output]
        public string CommandLineArguments
        {
            get
            {
                return GenerateCommandLineCommands();
            }
        }

        [Framework.Output]
        public string MSDeployToolPath
        {
            get
            {
                return string.Concat("\"", GenerateFullPathToTool(), "\" ");
            }
        }

        /// <summary>
        /// This enable us not to execute the msdeploy, but just have the output of PreviewMSDeploy
        /// </summary>
        public bool PreviewCommandLineOnly
        {
            get
            {
                return m_previewOnly;
            }
            set
            {
                m_previewOnly = value;
            }
        }

        // controlling whether a task should be execute.
        protected override bool SkipTaskExecution()
        {
            if (PreviewCommandLineOnly)
                return true;
            else
                return base.SkipTaskExecution();
        }

        protected override Framework.MessageImportance StandardOutputLoggingImportance
        {
            get
            {
                return Framework.MessageImportance.High;
            }
        }

        /// <summary>
        /// Override the Execute method to be able to send ExternalProjectStarted/Finished events.
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            bool bSuccess = false;
            if (PreviewCommandLineOnly)
            {
                // useful information about what was wrong with the parameters.
                if (!ValidateParameters())
                {
                    return false;
                }
                Log.LogMessage(Framework.MessageImportance.Low, Resources.MSDEPLOY_EXE_PreviewOnly);
                return true;
            }

            try
            {
                Log.LogMessage(Framework.MessageImportance.Normal, Resources.MSDEPLOY_EXE_Start);
                bSuccess = base.Execute();
                if (bSuccess)
                    Log.LogMessage(Framework.MessageImportance.Normal, Resources.MSDEPLOY_EXE_Succeeded);
            }
            catch (Exception ex)
            {
                // Log Failure
                Log.LogMessage(Framework.MessageImportance.High, Resources.MSDEPLOY_EXE_Failed);
                Log.LogErrorFromException(ex);
                bSuccess = false;
            }
            if (bSuccess)
            {
                string type = string.Empty;
                string path = string.Empty;
                Framework.ITaskItem? taskItem = null;
                if (Destination != null && Destination.GetLength(0) == 1)
                {
                    taskItem = Destination[0];
                }
                else
                {
                    if (Source != null)
                        taskItem = Source[0];
                }
                if (taskItem != null)
                {
                    type = taskItem.ItemSpec;
                    path = taskItem.GetMetadata("Path");
                }
                Utility.MsDeployExeEndOfExecuteMessage(bSuccess, type, path, Log);
            }
            return bSuccess;
        }

        // utility function to add the replace rule for the option
        public static void AddReplaceRulesToOptions(Utilities.CommandLineBuilder commandLineBuilder, Framework.ITaskItem[]? replaceRuleItems, string? valueQuoteChar)
        {
            if (commandLineBuilder is not null && replaceRuleItems is not null)// Dev10 bug 496639 foreach will throw the exception if the replaceRuleItem is null
            {
                List<string> arguments = new(6);

                foreach (Framework.ITaskItem item in replaceRuleItems)
                {
                    arguments.Clear();
                    Utility.BuildArgumentsBaseOnEnumTypeName(item, arguments, typeof(ReplaceRuleMetadata), valueQuoteChar);
                    commandLineBuilder.AppendSwitchUnquotedIfNotNull("-replace:", arguments.Count == 0 ? null : string.Join(",", arguments.ToArray()));
                }
            }
        }

        public static void AddSkipDirectiveToBaseOptions(Utilities.CommandLineBuilder commandLineBuilder, Framework.ITaskItem[]? skipRuleItems, string? valueQuoteChar)
        {
            if (commandLineBuilder is not null && skipRuleItems is not null)// Dev10 bug 496639 foreach will throw the exception if the replaceRuleItem is null
            {
                List<string> arguments = new(6);

                foreach (Framework.ITaskItem item in skipRuleItems)
                {
                    arguments.Clear();
                    Utility.BuildArgumentsBaseOnEnumTypeName(item, arguments, typeof(SkipRuleMetadata), valueQuoteChar);
                    commandLineBuilder.AppendSwitchUnquotedIfNotNull("-skip:", arguments.Count == 0 ? null : string.Join(",", arguments.ToArray()));
                }
            }
        }

        public static void AddDeclareParameterToCommandArgument(List<string> arguments,
                                                                Framework.ITaskItem? item,
                                                                string? valueQuote,
                                                                Dictionary<string, string> lookupDictionary)
        {
            if (arguments is not null && item is not null && lookupDictionary is not null)
            {
                // special for the name
                arguments.Clear();
                List<string> idenities = new(6);

                string name = item.ItemSpec;
                if (!string.IsNullOrEmpty(name))
                {
                    string element = item.GetMetadata(ExistingParameterValidationMetadata.Element.ToString());
                    if (string.IsNullOrEmpty(element))
                        element = "parameterEntry";
                    if (string.Compare(element, "parameterEntry", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        idenities.Add(name);
                        foreach (string dPIdentity in Enum.GetNames(typeof(ExistingDeclareParameterMetadata)))
                        {
                            idenities.Add(item.GetMetadata(dPIdentity));
                        }

                        string identity = string.Join(",", idenities.ToArray());
                        if (!lookupDictionary.ContainsKey(identity))
                        {
                            string nameValue = Utility.PutValueInQuote(name, valueQuote);
                            arguments.Add(string.Concat("name=", nameValue));
                            Type enumType = lookupDictionary.ContainsValue(name) ? typeof(ExistingDeclareParameterMetadata) : typeof(DeclareParameterMetadata);
                            // the rest, build on the Enum Name
                            Utility.BuildArgumentsBaseOnEnumTypeName(item, arguments, enumType, valueQuote);
                            lookupDictionary.Add(identity, name);
                        }
                    }
                    else if (string.Compare(element, "parameterValidation", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        System.Diagnostics.Debug.Assert(false, "msdeploy.exe doesn't support parameterValidation entry in the command line for declareParameter yet.");
                    }
                }
            }
        }

        /// <summary>
        /// Utility to build DeclareParameterOptions
        /// </summary>
        /// <param name="commandLineBuilder"></param>
        /// <param name="items"></param>
        public static void AddDeclareParametersOptions(Utilities.CommandLineBuilder commandLineBuilder, Framework.ITaskItem[]? originalItems, string? valueQuote, bool foptimisticParameterDefaultValue)
        {
            IList<Framework.ITaskItem> items = Utility.SortParametersTaskItems(originalItems, foptimisticParameterDefaultValue, DeclareParameterMetadata.DefaultValue.ToString());
            if (commandLineBuilder is not null && items is not null)
            {
                List<string> arguments = new(6);
                Dictionary<string, string> lookupDictionary = new(items.Count);

                foreach (Framework.ITaskItem item in items)
                {
                    AddDeclareParameterToCommandArgument(arguments, item, valueQuote, lookupDictionary);
                    commandLineBuilder.AppendSwitchUnquotedIfNotNull("-declareParam:", arguments.Count == 0 ? null : string.Join(",", arguments.ToArray()));
                }
            }
        }

        public static void AddImportDeclareParametersFilesOptions(Utilities.CommandLineBuilder commandLineBuilder, Framework.ITaskItem[]? items)
        {
            AddImportParametersFilesOptions(commandLineBuilder, "-declareParamFile:", items);
        }

        public static void AddImportSetParametersFilesOptions(Utilities.CommandLineBuilder commandLineBuilder, Framework.ITaskItem[]? items)
        {
            AddImportParametersFilesOptions(commandLineBuilder, "-setParamFile:", items);
        }

        internal static void AddImportParametersFilesOptions(Utilities.CommandLineBuilder commandLineBuilder, string parameterFlag, Framework.ITaskItem[]? items)
        {
            if (commandLineBuilder is not null && !string.IsNullOrEmpty(parameterFlag) && items is not null)// Dev10 bug 496639 foreach will throw the exception if the replaceRuleItem is null
            {
                foreach (Framework.ITaskItem item in items)
                {
                    string fileName = item.ItemSpec;
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        commandLineBuilder.AppendSwitch(string.Concat(parameterFlag, "\"", fileName, "\""));
                    }
                }
            }
        }

        /// <summary>
        /// Utility function to set SimpleSyncParameter Name/Value
        /// </summary>
        /// <param name="commandLineBuilder"></param>
        /// <param name="items"></param>
        public static void AddSimpleSetParametersToObject(Utilities.CommandLineBuilder commandLineBuilder, Framework.ITaskItem[]? originalItems, string? valueQuoteChar, bool foptimisticParameterDefaultValue)
        {
            IList<Framework.ITaskItem> items = Utility.SortParametersTaskItems(originalItems, foptimisticParameterDefaultValue, SimpleSyncParameterMetadata.Value.ToString());
            if (commandLineBuilder is not null && items is not null)
            {
                List<string> arguments = new(6);
                foreach (Framework.ITaskItem item in items)
                {
                    arguments.Clear();

                    // special for name
                    string name = item.ItemSpec;
                    if (!string.IsNullOrEmpty(name))
                    {
                        string valueData = Utility.PutValueInQuote(name, valueQuoteChar);
                        arguments.Add(string.Concat("name=", valueData));
                    }
                    // the rest, build on the enum name

                    Utility.BuildArgumentsBaseOnEnumTypeName(item, arguments, typeof(SimpleSyncParameterMetadata), valueQuoteChar);
                    commandLineBuilder.AppendSwitchUnquotedIfNotNull("-setParam:", arguments.Count == 0 ? null : string.Join(",", arguments.ToArray()));
                }
            }
        }

        /// <summary>
        /// Utility function to setParameters in type, scope, match, value of SyncParameter
        /// </summary>
        /// <param name="commandLineBuilder"></param>
        /// <param name="items"></param>
        public static void AddSetParametersToObject(Utilities.CommandLineBuilder commandLineBuilder, Framework.ITaskItem[]? originalItems, string? valueQuote, bool foptimisticParameterDefaultValue)
        {
            IList<Framework.ITaskItem> items = Utility.SortParametersTaskItems(originalItems, foptimisticParameterDefaultValue, ExistingSyncParameterMetadata.Value.ToString());
            if (commandLineBuilder is not null && items is not null)
            {
                List<string> arguments = new(6);
                Dictionary<string, string> lookupDictionary = new(items.Count);
                Dictionary<string, string> nameValueDictionary = new(items.Count, StringComparer.OrdinalIgnoreCase);

                foreach (Framework.ITaskItem item in items)
                {
                    arguments.Clear();

                    List<string> identities = new(6);

                    string name = item.ItemSpec;
                    if (!string.IsNullOrEmpty(name))
                    {
                        string element = item.GetMetadata(ExistingParameterValidationMetadata.Element.ToString());
                        if (string.IsNullOrEmpty(element))
                            element = "parameterEntry";

                        if (string.Compare(element, "parameterEntry", StringComparison.OrdinalIgnoreCase) == 0)
                        {

                            identities.Add(name);
                            foreach (string dPIdentity in Enum.GetNames(typeof(ExistingDeclareParameterMetadata)))
                            {
                                identities.Add(item.GetMetadata(dPIdentity));
                            }

                            string identity = string.Join(",", identities.ToArray());
                            if (!lookupDictionary.ContainsKey(identity))
                            {
                                string? data = null;
                                bool fExistingName = nameValueDictionary.ContainsKey(name);

                                if (nameValueDictionary.ContainsKey(name))
                                {
                                    data = nameValueDictionary[name];
                                }
                                else
                                {
                                    data = item.GetMetadata(ExistingSyncParameterMetadata.Value.ToString());
                                    nameValueDictionary.Add(name, data);
                                }

                                // the rest, build on the Enum Name
                                Utility.BuildArgumentsBaseOnEnumTypeName(item, arguments, typeof(ExistingDeclareParameterMetadata), valueQuote);
                                if (arguments.Count > 0 && !string.IsNullOrEmpty(data))
                                {
#if NET472
                                    arguments.Add(string.Concat(ExistingSyncParameterMetadata.Value.ToString().ToLower(CultureInfo.InvariantCulture),
                                                            "=", Utility.PutValueInQuote(data, valueQuote)));
#else
                                    arguments.Add(string.Concat(ExistingSyncParameterMetadata.Value.ToString().ToLower(),
                                                            "=", Utility.PutValueInQuote(data, valueQuote)));
#endif
                                }
                                lookupDictionary.Add(identity, name);

                            }
                        }
                        else if (string.Compare(element, "parameterValidation", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            System.Diagnostics.Debug.Assert(false, "msdeploy.exe doesn't support parameterValidation entry in the command line for declareParameter yet.");
                        }
                    }
                    commandLineBuilder.AppendSwitchUnquotedIfNotNull("-setParam:", arguments.Count == 0 ? null : string.Join(",", arguments.ToArray()));
                }
            }
        }

        public static void AddDestinationProviderSettingToObject(Utilities.CommandLineBuilder commandLineBuilder, string? dest, Framework.ITaskItem[]? items, string? valueQuoteChar,
                                                                Framework.ITaskItem[]? additionalProviderItems, MSDeploy msdeploy)
        {
            //commandLineBuilder.AppendSwitchUnquotedIfNotNull("-source:", m_source);
            //commandLineBuilder.AppendSwitchUnquotedIfNotNull("-dest:", m_dest);
            List<string> arguments = new(6);

            if (items != null && items.GetLength(0) == 1)
            {
                Framework.ITaskItem taskItem = items[0];
                string provider = taskItem.ItemSpec;
                string path = taskItem.GetMetadata("Path");
                string valueData = Utility.PutValueInQuote(path, valueQuoteChar);
                string setData = (string.IsNullOrEmpty(path)) ? provider : string.Concat(provider, "=", valueData);
                arguments.Add(setData);

                //Commonly supported provider settings:
                //    computerName=<name>     Name of remote computer or proxy-URL
                //    wmsvc=<name>            Name of remote computer or proxy-URL for the Web
                //                            Management Service (wmsvc)
                //    userName=<name>         User name to authenticate
                //    password=<password>     Password of user name
                //    encryptPassword=<pwd>   Password to use for encryption related operations
                //    includeAcls=<bool>      If true, include ACLs in the operation for the
                //                            specified path

                foreach (string name in taskItem.MetadataNames)
                {
                    if (!Utility.IsInternalMsdeployWellKnownItemMetadata(name))
                    {
                        string value = taskItem.GetMetadata(name);
                        if (!string.IsNullOrEmpty(value))
                        {
                            valueData = Utility.PutValueInQuote(value, valueQuoteChar);
                            setData = string.Concat(name, "=", valueData);
                            arguments.Add(setData);
                        }
                    }
                    else
                    {
                        Utility.IISExpressMetadata expressMetadata;
                        if (Enum.TryParse(name, out expressMetadata))
                        {
                            string value = taskItem.GetMetadata(name);
                            if (!string.IsNullOrEmpty(value))
                            {
                                switch (expressMetadata)
                                {
                                    case Utility.IISExpressMetadata.WebServerAppHostConfigDirectory:
                                        msdeploy.WebServerAppHostConfigDirectory = value;
                                        break;
                                    case Utility.IISExpressMetadata.WebServerDirectory:
                                        msdeploy.WebServerDirectory = value;
                                        break;
                                    case Utility.IISExpressMetadata.WebServerManifest:
                                        msdeploy.WebServerManifest = value;
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            // If additional parameters are specified, we add these too. the itemSpec will be something like iisApp, contentPath, etc and 
            // each item should have a name\value pair defined as metadata. Each provider will be written as itemSpec.Name=Value
            if (additionalProviderItems != null)
            {
                foreach (Framework.ITaskItem item in additionalProviderItems)
                {
                    if (!string.IsNullOrEmpty(item.ItemSpec))
                    {
                        string settingName = item.GetMetadata("Name");
                        string settingValue = item.GetMetadata("Value");
                        if (!string.IsNullOrEmpty(settingName) && !string.IsNullOrEmpty(settingValue))
                        {
                            string providerString = string.Concat(item.ItemSpec, ".", settingName, "=", settingValue);
                            arguments.Add(providerString);
                        }
                    }
                }
            }

            commandLineBuilder.AppendSwitchUnquotedIfNotNull(dest, arguments.Count == 0 ? null : string.Join(",", arguments.ToArray()));
            return;
        }

        /// <summary>
        /// Utility function to help to generate Switch per item
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="strSwitch"></param>
        /// <param name="args"></param>
        private static void GenerateSwitchPerItem(Utilities.CommandLineBuilder commandLine, string strSwitch, string? args)
        {
            if (args is not null && args.Length != 0)
            {
                foreach (string dl in args.Split(new char[] { ';' }))
                {
                    if (!string.IsNullOrEmpty(dl))
                    {
                        commandLine.AppendSwitchUnquotedIfNotNull(strSwitch, dl);
                    }
                }
            }
        }

        internal static void IncorporateSettingsFromHostObject(ref Framework.ITaskItem[]? skipRuleItems, Framework.ITaskItem[]? destProviderSetting, IEnumerable<Framework.ITaskItem>? hostObject)
        {
            if (hostObject != null)
            {
                //retrieve user credentials
                Framework.ITaskItem? credentialItem = hostObject.FirstOrDefault(p => p.ItemSpec == VSMsDeployTaskHostObject.CredentialItemSpecName);
                if (credentialItem != null && destProviderSetting != null && destProviderSetting[0] != null)
                {
                    Framework.ITaskItem destSettings = destProviderSetting[0];
                    string userName = credentialItem.GetMetadata(VSMsDeployTaskHostObject.UserMetaDataName);
                    if (!string.IsNullOrEmpty(userName))
                    {
                        destSettings.SetMetadata(VSMsDeployTaskHostObject.UserMetaDataName, userName);
                        destSettings.SetMetadata(VSMsDeployTaskHostObject.PasswordMetaDataName, credentialItem.GetMetadata(VSMsDeployTaskHostObject.PasswordMetaDataName));
                    }
                }

                //retrieve skip rules
                IEnumerable<Framework.ITaskItem> skips = hostObject.Where(item => item.ItemSpec == VSMsDeployTaskHostObject.SkipFileItemSpecName);
                if (skips != null)
                {
                    if (skipRuleItems != null)
                    {
                        skipRuleItems = skips.Concat(skipRuleItems).ToArray();
                    }
                    else
                    {
                        skipRuleItems = skips.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Generates command line arguments for msdeploy.exe
        /// </summary>
        protected override string GenerateCommandLineCommands()
        {
            Utilities.CommandLineBuilder commandLine = new();
            IncorporateSettingsFromHostObject(ref m_skipRuleItemsITaskItem, Destination, HostObject as IEnumerable<Framework.ITaskItem>);
            AddDestinationProviderSettingToObject(commandLine, "-source:", Source, m_strValueQuote, null, this);
            AddDestinationProviderSettingToObject(commandLine, "-dest:", Destination, m_strValueQuote, AdditionalDestinationProviderOptions, this);

            commandLine.AppendSwitchUnquotedIfNotNull("-verb:", m_verb);
            commandLine.AppendSwitchUnquotedIfNotNull("-failureLevel:", m_failureLevel);
            commandLine.AppendSwitchUnquotedIfNotNull("-xpath:", m_xpath);
            commandLine.AppendSwitchUnquotedIfNotNull("-replace:", m_replace);
            commandLine.AppendSwitchUnquotedIfNotNull("-skip:", m_skip);

            GenerateSwitchPerItem(commandLine, "-enableRule:", m_enableRule);
            GenerateSwitchPerItem(commandLine, "-disableRule:", m_disableRule);
            GenerateSwitchPerItem(commandLine, "-enableLink:", m_enableLink);
            GenerateSwitchPerItem(commandLine, "-disableLink:", m_disableLink);
            GenerateSwitchPerItem(commandLine, "-disableSkipDirective:", m_disableSkipDirective);
            GenerateSwitchPerItem(commandLine, "-enableSkipDirective:", m_enableSkipDirective);

            // this allow multiple replace rule to happen, we should consider do the same thing for skip:
            AddReplaceRulesToOptions(commandLine, m_replaceRuleItemsITaskItem, m_strValueQuote);
            AddSkipDirectiveToBaseOptions(commandLine, m_skipRuleItemsITaskItem, m_strValueQuote);
            AddImportDeclareParametersFilesOptions(commandLine, m_importDeclareParametersItems);
            AddDeclareParametersOptions(commandLine, m_declareParameterItems, m_strValueQuote, OptimisticParameterDefaultValue);

            AddImportSetParametersFilesOptions(commandLine, m_importSetParametersItems);
            AddSimpleSetParametersToObject(commandLine, m_simpleSetParameterItems, m_strValueQuote, OptimisticParameterDefaultValue);
            AddSetParametersToObject(commandLine, m_setParameterItems, m_strValueQuote, OptimisticParameterDefaultValue);

            if (m_xml) commandLine.AppendSwitch("-xml");
            if (m_whatif) commandLine.AppendSwitch("-whatif");
            if (m_verbose) commandLine.AppendSwitch("-verbose");
            if (m_allowUntrusted) commandLine.AppendSwitch("-allowUntrusted");
            if (m_useChecksum) commandLine.AppendSwitch("-useChecksum");

            if (m_enableTransaction) commandLine.AppendSwitch("-enableTransaction");
            if (m_retryAttempts > 0) commandLine.AppendSwitchUnquotedIfNotNull("-retryAttempts=", m_retryAttempts.ToString(CultureInfo.InvariantCulture));
            if (m_retryInterval > 0) commandLine.AppendSwitchUnquotedIfNotNull("-retryInterval=", m_retryInterval.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrEmpty(UserAgent))
            {
                commandLine.AppendSwitchUnquotedIfNotNull("-userAgent=", string.Concat("\"", UserAgent, "\""));
            }

            //IISExpress support
            //public const string AppHostConfigDirectory = "-appHostConfigDir";
            // *    public const string WebServerDirectory = "-webServerDir";
            //      public const string WebServerManifest = "-webServerManifest";
            commandLine.AppendSwitchIfNotNull("-appHostConfigDir:", WebServerAppHostConfigDirectory);
            commandLine.AppendSwitchIfNotNull("-webServerDir:", WebServerDirectory);
            // bug in msdeploy.exe currently only take the file name
            commandLine.AppendSwitchIfNotNull("-webServerManifest:", Path.GetFileName(WebServerManifest));

            m_lastCommandLine = commandLine.ToString();

            // show arguments in the output 
            Log.LogMessage(Framework.MessageImportance.Low, string.Concat("\"", GenerateFullPathToTool(), "\" ", m_lastCommandLine));
            return m_lastCommandLine;
        }

        /// <summary>
        /// The name of the tool to execute
        /// </summary>
        protected override string ToolName
        {
            get { return "msdeploy.exe"; }
        }

        /// <summary>
        /// Determine the path to msdeploy.exe
        /// </summary>
        /// <returns>path to aspnet_merge.exe, null if not found</returns>
        protected override string GenerateFullPathToTool()
        {
            string result = ExePath is null ? string.Empty : Path.Combine(ExePath, ToolName);

            if (string.Compare(ExePath, "%MSDeployPath%", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // if it comes in as %msdeploypath% don't use Path.Combine because it will add a \ which is 
                // not necessary since reg key for MSDeployPath already contains it
                result = string.Format("{0}{1}", ExePath, ToolName);
            }

            return result;
        }

        /// <summary>
        /// Validate the task arguments, log any warnings/errors
        /// </summary>
        /// <returns>true if arguments are current enough to continue processing, false otherwise</returns>
        protected override bool ValidateParameters()
        {
            if (Source is not null && Source.GetLength(0) > 1)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, Resources.MSDEPLOY_InvalidSourceCount, Source.GetLength(0)), null);
                return false;
            }

            if (Destination != null && Destination.GetLength(0) > 1)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, Resources.MSDEPLOY_InvalidDestinationCount, Destination.GetLength(0)), null);
                return false;
            }
            else
            {
                string[]? validVerbs = null;
                bool fNullDestination = false;
                if (Destination == null || Destination.GetLength(0) == 0)
                {
                    fNullDestination = true;
                    validVerbs = new string[] {
                        "dump",
                        "getDependencies",
                        "getParameters",
                        "getSystemInfo",
                    };
                }
                if (Source == null || Source.GetLength(0) == 0)
                {
                    validVerbs = new string[] {
                        "delete",
                    };
                }
                else
                {
                    validVerbs = new string[] {
                        "sync",
                        "migrate",
                    };
                }
                if (validVerbs != null)
                {
                    foreach (string verb in validVerbs)
                    {
                        if (string.Compare(Verb, verb, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return true;
                        }
                    }
                }
                Log.LogError(string.Format(CultureInfo.CurrentCulture, Resources.MSDEPLOY_InvalidVerbForTheInput, Verb, Source?[0].ItemSpec, (fNullDestination) ? null : Destination?[0].ItemSpec), null);
                return false;
            }
        }
    }
}

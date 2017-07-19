﻿// The MIT License (MIT) 
// Copyright (c) 1994-2017 The Sage Group plc or its licensors.  All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of 
// this software and associated documentation files (the "Software"), to deal in 
// the Software without restriction, including without limitation the rights to use, 
// copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
// Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
// OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TemplateWizard;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace Sage300UICustomizationWizard
{
    /// <summary> Class for UI Wizard </summary>
    public class Sage300UICustomizationUserInterface : IWizard
    {

        /// <summary> Property for Name </summary>
        public const string PropertyName = "Name";

        /// <summary> Property for PackageId </summary>
        public const string PropertyPackageId = "PackageId";

        /// <summary> Property for Description </summary>
        public const string PropertyDescription = "Description";

        /// <summary> Property for BusinessPartnerName </summary>
        public const string PropertyBusinessPartnerName = "BusinessPartnerName";

        /// <summary> Property for SageCompatibility </summary>
        public const string PropertySageCompatibility = "SageCompatibility";

        /// <summary> Property for EULA </summary>
        public const string PropertyEula = "EULA";

        /// <summary> Property for Bootstrapper </summary>
        public const string PropertyBootstrapper = "Bootstrapper";

        /// <summary> Property for Assembly </summary>
        public const string PropertyAssembly = "Assembly";

        /// <summary> Property for Version </summary>
        public const string PropertyVersion = "Version";

        /// <summary> Suffix for Bootstrapper XML </summary>
        public const string XmlBootstrapperSuffix = "Bootstrapper.xml";

        /// <summary> Suffix for Project DLL </summary>
        public const string DllProjectSuffix = ".dll";

        /// <summary> Business Partner Name ($companyname$) </summary>
        private string _companyName;
        /// <summary> Module ($module$) </summary>
        private string _moduleName;
        /// <summary> Project Name ($project$) </summary>
        private string _projectName;
        /// <summary> Assembly Name </summary>
        private string _assemblyName;
        /// <summary> Customization Manifest </summary>
        private JObject _customizationManifest;
        /// <summary> Customization File Name </summary>
        private string _customizationFileName;

        /// <summary> Solution folder specified by user before wizard appears </summary>
        private string _solutionFolder;
        /// <summary> Destination folder for solution </summary>
        private string _destinationFolder;

        private DTE _dte;
        private string _safeprojectname;

        /// <summary> Before opening file </summary>
        /// <param name="projectItem">Project Item</param>
        /// <remarks>Called before opening any item that has the OpenInEditor attribute</remarks>
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        /// <summary> Project finished generating </summary>
        /// <param name="project">Project</param>
        public void ProjectFinishedGenerating(Project project)
        {
        }

        /// <summary> Project item finished generating </summary>
        /// <param name="projectItem">Project Item</param>
        /// <remarks>Called for item templates, not project templates</remarks>
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        /// <summary> Run finished </summary>
        /// <remarks>Invoked after project is generated</remarks>
        public void RunFinished()
        {
            var sln = (Solution2)_dte.Solution;
            var csTemplatePath = sln.GetProjectTemplate("Sage300UICustomizationWeb.zip|FrameworkVersion=4.6.2", "CSharp");

            sln.Create(_destinationFolder, _safeprojectname);

            var parameters = "$companyname$=" + _companyName + "|$module$=" + _moduleName + "|$project$=" + _projectName;
            var sourceFilenameAndParameters = csTemplatePath + "|" + parameters;
            var destFolder = Path.Combine(_destinationFolder, _assemblyName);
            sln.AddFromTemplate(sourceFilenameAndParameters, destFolder, _assemblyName);

            // Remove properties from Manifest first
            if (_customizationManifest.Property(PropertyBootstrapper) != null)
            {
                _customizationManifest.Property(PropertyBootstrapper).Remove();
            }
            if (_customizationManifest.Property(PropertyAssembly) != null)
            {
                _customizationManifest.Property(PropertyAssembly).Remove();
            }

            // Update Manifest with new file names for Assembly and Bootstrapper
            _customizationManifest.Property(PropertyDescription).AddAfterSelf(new JProperty(PropertyBootstrapper, _projectName + _moduleName + XmlBootstrapperSuffix));
            _customizationManifest.Property(PropertyBootstrapper).AddAfterSelf(new JProperty(PropertyAssembly, _assemblyName + DllProjectSuffix));

            // Delete file
            File.Delete(_customizationFileName);

            // Write out updated manifest
            File.WriteAllText(_customizationFileName, _customizationManifest.ToString());
        }

        /// <summary> Run started </summary>
        /// <param name="automationObject">Automation Object</param>
        /// <param name="replacementsDictionary">Replacements</param>
        /// <param name="runKind">Run Kind</param>
        /// <param name="customParams">Custom Params</param>
        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {

            _dte = (DTE)automationObject;

            _solutionFolder = replacementsDictionary["$solutiondirectory$"];
            _destinationFolder = replacementsDictionary["$destinationdirectory$"];
            _safeprojectname = replacementsDictionary["$safeprojectname$"];

            try
            {
                // Display Customization Wizard Form
                var inputForm = new UserInputForm();
                var res = inputForm.ShowDialog();

                // Abort wizard if not proceeding with solution generation
                if (res != DialogResult.OK)
                {
                    throw new WizardBackoutException();
                }

                // Get parameters from screen
                _companyName = inputForm.BusinessPartnerName;
                _moduleName = inputForm.ModuleName;
                _projectName = inputForm.ProjectName;
                _assemblyName = inputForm.AssemblyName;
                _customizationManifest = inputForm.CustomizationManifest;
                _customizationFileName = inputForm.CustomizationFileName;

                // Add custom parameters for token replacement 
                replacementsDictionary.Add("$companyname$", _companyName);
                replacementsDictionary.Add("$module$", _moduleName);
                replacementsDictionary.Add("$project$", _projectName);
            }
            catch
            {
                // Clean up the template that was written to disk
                if (!Directory.Exists(_solutionFolder))
                {
                    throw;
                }

                try
                {
                    Directory.Delete(_solutionFolder);
                }
                catch
                {
                    // Ignore
                }

                throw;
            }

        }

        /// <summary> Should add project item </summary>
        /// <param name="filePath">File path</param>
        /// <returns>True</returns>
        /// <remarks>Called for item templates not project templates</remarks>
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }


    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Dbg = System.Diagnostics.Debug;
using System.Management.Automation;
using Microsoft.PowerShell.PowerShellGet.UtilClasses;
using NuGet.Versioning;

namespace Microsoft.PowerShell.PowerShellGet.Cmdlets
{
    /// <summary>
    /// The Save-PSResource cmdlet combines the Save-Module, Save-Script cmdlets from V2.
    /// It saves from a package found from a repository (local or remote) based on the -Name parameter argument.
    /// It does not return an object. Other parameters allow the returned results to be further filtered.
    /// </summary>

    [Cmdlet(VerbsData.Save,
        "PSResource",
        DefaultParameterSetName = NameParameterSet,
        SupportsShouldProcess = true,
        HelpUri = "<add>")]
    [OutputType(typeof(PSResourceInfo))]
    public sealed
    class SavePSResource : PSCmdlet
    {
        #region Members
        private const string NameParameterSet = "NameParameterSet";
        private const string InputObjectParameterSet = "InputObjectParameterSet";
        private CancellationTokenSource _source;
        private CancellationToken _cancellationToken;
        #endregion

        #region Parameters

        /// <summary>
        /// Specifies name of a resource or resources to save.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = NameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string[] Name { get; set; }

        /// <summary>
        /// Specifies the version or version range of the resource to be saved.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Version { get; set; }

        /// <summary>
        /// When specified, allow saving prerelease versions.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public SwitchParameter Prerelease { get; set; }

        /// <summary>
        /// Specifies one or more repository names to search and save packages from.
        /// If not specified, search will include all currently registered repositories in order of highest priority.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string[] Repository { get; set; }

        /// <summary>
        /// Specifies optional credentials to be used when accessing a private repository.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// When specified, saves the resource as a .nupkg.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public SwitchParameter AsNupkg { get; set; }

        /// <summary>
        /// Saves the metadata XML file with the resource.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public string[] IncludeXML { get; set; }

        /// <summary>
        /// Specifies the destination where the resource is to be saved.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public string Path { get; set; }

        /// <summary>
        /// When specified, supresses being prompted for untrusted sources.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public SwitchParameter TrustRepository { get; set; }

        /// <summary>
        /// Used to pass in an object via pipeline to save.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ParameterSetName = NameParameterSet)]
        public PSCustomObject[] InputObject { get; set; }

        /// <summary>
        /// When specified, displays the succcessfully saved resource and its information.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public SwitchParameter PassThru { get; set; }

        #endregion

        #region Methods

        protected override void BeginProcessing()
        {
            _source = new CancellationTokenSource();
            _cancellationToken = _source.Token;
        }

        protected override void ProcessRecord()
        {
            foreach (string pkgName in Name)
            {
                if (WildcardPattern.ContainsWildcardCharacters(pkgName))
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new PSNotImplementedException("Name parameter for Save-PSResource cannot contain wildcards"),
                        "NameParameterCannotContainWildcards",
                        ErrorCategory.NotImplemented,
                        this));
                }
            }

            VersionRange versionRange = new VersionRange();

            if (Version !=null && !Utils.TryParseVersionOrVersionRange(Version, out versionRange))
            {
                WriteError(new ErrorRecord(
                    new PSInvalidOperationException("Cannot parse Version parameter provided into VersionRange"),
                    "ErrorParsingVersionParamIntoVersionRange",
                    ErrorCategory.InvalidArgument,
                    this));
                versionRange = new VersionRange(); // or should I return here instead?
            }

            InstallHelper installHelper = new InstallHelper(
                    update: false,
                    save: true,
                    cancellationToken: _cancellationToken,
                    cmdletPassedIn: this);

            switch (ParameterSetName)
            {
                case NameParameterSet:
                    installHelper.ProcessInstallParams(
                        names: Name,
                        versionRange: versionRange,
                        prerelease: Prerelease,
                        repository: Repository,
                        scope: null,
                        acceptLicense: false,
                        quiet: false, // todo: do we need to add this field? think not
                        reinstall: false,
                        force: false, // todo: do we need to add this value?
                        trustRepository: TrustRepository,
                        noClobber: false, // todo: do we need to add this property?
                        credential: Credential,
                        requiredResourceFile: null,
                        requiredResourceJson: null,
                        requiredResourceHash: null,
                        specifiedPath: null, // TODO: do we need to add? Think so!
                        asNupkg: false, // TODO: do we need to add? Think so!
                        includeXML: false, // do we need to add? Think so
                        pathsToInstallPkg: null); // do we need to add? think not

                    break;

                case InputObjectParameterSet:
                    // TODO
                    break;

                default:
                    Dbg.Assert(false, "Invalid parameter set");
                    break;
            }
        }

        #endregion
    }
}
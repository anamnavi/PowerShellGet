// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation;
using System.Globalization;
using System.Collections.Generic;

namespace Microsoft.PowerShell.PowerShellGet.Cmdlets
{
    /// <summary>
    /// The Register-PSResourceRepository cmdlet replaces the Register-PSRepository from V2.
    /// It registers a repository for PowerShell modules.
    /// The repository is registered to the current user's scope and does not have a system-wide scope.
    /// </summary>

    using RepositorySettings = Microsoft.PowerShell.PowerShellGet.RepositorySettings.RepositorySettings;
    using PSRepositoryItem = Microsoft.PowerShell.PowerShellGet.PSRepositoryItem.PSRepositoryItem;

    [Cmdlet(VerbsLifecycle.Register,
        "PSResourceRepository",
        DefaultParameterSetName = NameParameterSet,
        SupportsShouldProcess = true,
        HelpUri = "<add>")]
    public sealed
    class RegisterPSResourceRepository : PSCmdlet
    {
        #region Members
        private readonly string PSGalleryRepoName = "PSGallery";
        private readonly string PSGalleryRepoURL = "https://www.powershellgallery.com/api/v2";
        private const int defaultPriority = 50;
        private const bool defaultTrusted = false;
        private const string NameParameterSet = "NameParameterSet";
        private const string PSGalleryParameterSet = "PSGalleryParameterSet";
        private const string RepositoriesParameterSet = "RepositoriesParameterSet";

        #endregion

        #region Parameters
        /// <summary>
        /// Specifies name for the repository to be registered.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = NameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get
            { return _name; }

            set
            { _name = value; }
        }
        private string _name;

        /// <summary>
        /// Specifies the location of the repository to be registered.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = NameParameterSet)]
        [ValidateNotNullOrEmpty]
        public Uri URL
        {
            get
            { return _url; }

            set
            {
                Uri url;
                if(!(Uri.TryCreate(value, string.Empty, out url)
                    && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeFtp || url.Scheme == Uri.UriSchemeFile)))
                    {
                        var message = string.Format(CultureInfo.InvariantCulture, "The URL provided is not valid: {0}", value);
                        var ex = new ArgumentException(message);
                        var moduleManifestNotFound = new ErrorRecord(ex, "InvalidUrl", ErrorCategory.InvalidArgument, null);
                        ThrowTerminatingError(moduleManifestNotFound);
                    }
                _url = url;
            }
        }
        private Uri _url;

        /// <summary>
        /// When specified, registers PSGallery repository.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = PSGalleryParameterSet)]
        public SwitchParameter PSGallery
        {
            get
            { return _psgallery; }

            set
            { _psgallery = value; }
        }
        private SwitchParameter _psgallery;

        /// <summary>
        /// Specifies a hashtable of repositories and is used to register multiple repositories at once.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "RepositoriesParameterSet")]
        [ValidateNotNullOrEmpty]
        public List<Hashtable> Repositories
        {
            get
            { return _repositories; }

            set
            { _repositories = value; }
        }
        private List<Hashtable> _repositories;

        /// <summary>
        /// Specifies whether the repository should be trusted.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [Parameter(ParameterSetName = PSGalleryParameterSet)]
        public SwitchParameter Trusted
        {
            get
            { return _trusted; }

            set
            { _trusted = value; }
        }
        private SwitchParameter _trusted;

        /// <summary>
        /// Specifies the priority ranking of the repository, such that repositories with higher ranking priority are searched
        /// before a lower ranking priority one, when searching for a repository item across multiple registered repositories.
        /// Valid priority values range from 0 to 50, such that a lower numeric value (i.e 10) corresponds
        /// to a higher priority ranking than a higher numeric value (i.e 40). Has default value of 50.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [Parameter(ParameterSetName = PSGalleryParameterSet)]
        [ValidateNotNullOrEmpty]
        [ValidateRange(0, 50)]
        public int Priority
        {
            get
            { return _priority; }

            set
            { _priority = value; }
        }
        private int _priority = defaultPriority;

        /// <summary>
        /// Specifies a proxy server for the request, rather than a direct connection to the internet resource.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public Uri Proxy
        {
            get
            { return _proxy; }

            set
            { _proxy = value; }
        }
        private Uri _proxy;

        /// <summary>
        /// Specifies a user account that has permission to use the proxy server that is specified by the Proxy parameter.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public PSCredential ProxyCredential
        {
            get
            { return _proxycredential; }

            set
            { _proxycredential = value; }
        }
        private PSCredential _proxycredential;

        /// <summary>
        /// When specified, displays the succcessfully registered repository and its information
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            { return _passThru; }

            set
            { _passThru = value; }
        }
        private SwitchParameter _passThru;
        #endregion

        /// <summary>
        /// Process the repository being registered
        /// </summary>
        protected override void ProcessRecord()
        {
            List<PSRepositoryItem> items = new List<PSRepositoryItem>();

            if(ParameterSetName.Equals(PSGalleryParameterSet)){
                PSGalleryParameterSetHelper(items);
            }
            else if(ParameterSetName.Equals(NameParameterSet))
            {
                NameParameterSetHelper(items);
            }
            else if(ParameterSetName.Equals(RepositoriesParameterSet))
            {
                RepositoriesParameterSetHelper(items);
            }

            if(_passThru)
            {
                foreach (PSRepositoryItem item in items)
                {
                    WriteObject(item);
                }
            }

        }

        private void PSGalleryParameterSetHelper(List<PSRepositoryItem> items)
        {
            var psGalleryUri = new Uri(PSGalleryRepoURL);
            try
            {
                items.Add(RepositorySettings.Add(PSGalleryRepoName, psGalleryUri, _priority, _trusted));
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private void NameParameterSetHelper(List<PSRepositoryItem> items)
        {
            if(_name.Equals("PSGallery"))
            {
                throw new ArgumentException(String.Format("Use 'Register-PSResourceRepository -PSGallery' to register the PSGallery repository."));
            }
            else{
                try
                {
                    items.Add(RepositorySettings.Add(_name, _url, _priority, _trusted));
                }
                catch(Exception e)
                {
                    throw new Exception(e.Message);
                }
            }

        }

        private void RepositoriesParameterSetHelper(List<PSRepositoryItem> items)
        {
            foreach(Hashtable repo in _repositories)
            {
                if(repo.ContainsKey(PSGalleryRepoName))
                {
                    _priority = repo.ContainsKey("Priority") ? (int)repo["Priority"] : defaultPriority;
                    _trusted = repo.ContainsKey("Trusted") ? (bool)repo["Trusted"] : defaultTrusted;
                    PSGalleryParameterSetHelper(items);
                    continue;
                }

                // if not PSGallery, then check that sufficient keys exist for NameParameterSet style register option
                if(!repo.ContainsKey("Name") || String.IsNullOrEmpty(repo["Name"].ToString()))
                {
                    throw new System.ArgumentException(string.Format(CultureInfo.InvariantCulture, "Repository name cannot be null"));
                }
                if(!repo.ContainsKey("Url") || String.IsNullOrEmpty(repo["Url"].ToString()))
                {
                    throw new System.ArgumentException(string.Format(CultureInfo.InvariantCulture, "Repository url cannot be null"));
                }

                Uri _repoURL;
                if(!(Uri.TryCreate(repo["URL"].ToString(), UriKind.Absolute, out _repoURL)
                    && (_repoURL.Scheme == Uri.UriSchemeHttp || _repoURL.Scheme == Uri.UriSchemeHttps || _repoURL.Scheme == Uri.UriSchemeFtp || _repoURL.Scheme == Uri.UriSchemeFile)))
                {
                        throw new System.ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid Url"));
                }

                _name = repo["Name"].ToString();
                _url = _repoURL;
                _priority = repo.ContainsKey("Priority") ? Convert.ToInt32(repo["Priority"].ToString()) : defaultPriority;
                _trusted = repo.ContainsKey("Trusted") ? Convert.ToBoolean(repo["Trusted"].ToString()) : defaultTrusted;

                NameParameterSetHelper(items);
            }
        }
    }
}
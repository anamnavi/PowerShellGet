// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PowerShell.PowerShellGet.UtilClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml;

namespace Microsoft.PowerShell.PowerShellGet.Cmdlets
{
    internal class LocalResponseUtil : ResponseUtil
    {
        #region Members

        public override PSRepositoryInfo repository { get; set; }
        public readonly string fileTypeKey = "filetype";

        #endregion

        #region Constructor

        public LocalResponseUtil(PSRepositoryInfo repository) : base(repository)
        {
            this.repository = repository;
        }

        #endregion

        #region Overriden Methods
        public override IEnumerable<PSResourceResult> ConvertToPSResourceResult(FindResults responseResults)
        {
            Hashtable[] responses = responseResults.HashtableResponse;

            foreach (Hashtable response in responses)
            {
                if (!response.ContainsKey(fileTypeKey) || fileTypeKey.Equals(Utils.MetadataFileType.None))
                {
                    yield return new PSResourceResult(returnedObject: null, errorMsg: "Package response did not contain metadata file type key so will be skipped.", isTerminatingError: false);
                    continue;
                }

                Utils.MetadataFileType fileType = (Utils.MetadataFileType) response[fileTypeKey];
                response.Remove(fileTypeKey);
                PSResourceResult pkgInfo = null;

                switch (fileType)
                {
                    case Utils.MetadataFileType.ModuleManifest:
                        if (!PSResourceInfo.TryConvertFromHashtableForPsd1(
                            pkgMetadata: response,
                            psGetInfo: out PSResourceInfo pkgWithPsd1,
                            out string psd1ErrorMsg,
                            repository: repository))
                        {
                            yield return new PSResourceResult(returnedObject: null, errorMsg: psd1ErrorMsg, isTerminatingError: false);
                        }

                        pkgInfo = new PSResourceResult(returnedObject: pkgWithPsd1, errorMsg: psd1ErrorMsg, isTerminatingError: false);
                        break;

                    case Utils.MetadataFileType.ScriptFile:
                        if (!PSResourceInfo.TryConvertFromHashtableForPs1(
                            pkgMetadata: response,
                            psGetInfo: out PSResourceInfo pkgWithPs1,
                            out string ps1ErrorMsg,
                            repository: repository
                        ))
                        {
                            yield return new PSResourceResult(returnedObject: null, errorMsg: ps1ErrorMsg, isTerminatingError: false);
                        }

                        pkgInfo = new PSResourceResult(returnedObject: pkgWithPs1, errorMsg: ps1ErrorMsg, isTerminatingError: false);
                        break;

                    case Utils.MetadataFileType.Nuspec:
                        if (!PSResourceInfo.TryConvertFromHashtableForNuspec(
                            pkgMetadata: response,
                            psGetInfo: out PSResourceInfo pkgWithNuspec,
                            out string nuspecErrorMsg,
                            repository: repository))
                        {
                            yield return new PSResourceResult(returnedObject: null, errorMsg: nuspecErrorMsg, isTerminatingError: false);
                        }

                        pkgInfo = new PSResourceResult(returnedObject: pkgWithNuspec, errorMsg: nuspecErrorMsg, isTerminatingError: false);
                        break;
                }

                yield return pkgInfo;
            }

            yield break;
        }

        #endregion
    }
}
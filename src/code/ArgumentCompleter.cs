// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PowerShell.PowerShellGet.UtilClasses;
using Microsoft.PowerShell.PowerShellGet.Cmdlets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using NuGet.Versioning;

internal class RepositoryNameCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        List<PSRepositoryInfo> listOfRepositories = RepositorySettings.Read(null, out string[] _);

        wordToComplete = Utils.TrimQuotes(wordToComplete);
        var wordToCompletePattern = WildcardPattern.Get(
            pattern: string.IsNullOrWhiteSpace(wordToComplete) ? "*" : wordToComplete + "*",
            options: WildcardOptions.IgnoreCase);

        foreach (PSRepositoryInfo repo in listOfRepositories)
        {
            string repoName = repo.Name;
            if (wordToCompletePattern.IsMatch(repoName))
            {
                yield return new CompletionResult(Utils.QuoteName(repoName));
            }
        }
    }
}

internal class InstalledPackagesNameCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
        {
            wordToComplete = Utils.TrimQuotes(wordToComplete);
            wordToComplete = String.IsNullOrWhiteSpace(wordToComplete) ? "*" : wordToComplete + "*";

            GetHelper getHelper = new GetHelper(null); // null won't work for Verbose/DEbug/errors I think...

            // getHelper handles wildcards
            foreach (PSResourceInfo pkg in getHelper.FilterPkgPaths(
                name: new string[] {wordToComplete},
                versionRange: VersionRange.All, 
                pathsToSearch: Utils.GetAllResourcePaths(null))) // null probably won't work!
            {
                yield return new CompletionResult(Utils.QuoteName(pkg.Name));
            }
        }
}

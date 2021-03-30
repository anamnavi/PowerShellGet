---
external help file: PowerShellGet.dll-Help.xml
Module Name: PowerShellGet
online version:
schema: 2.0.0
---

# Get-PSResourceRepository

## SYNOPSIS
Finds and returns registered repository information.

## SYNTAX

```
Get-PSResourceRepository [[-Name] <String[]>] [<CommonParameters>]
```

## DESCRIPTION
The Get-PSResourceRepository cmdlet replaces the Get-PSRepository cmdlet from V2. It searches for the PowerShell module repositories that are registered for the current user. By default it will return all registered repositories, or if the -Name parameter argument is specified then it wil return the repository which matches that name. It returns PSRepositoryInfo objects which contain information for each repository item found.

## EXAMPLES

### Example 1
```
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
{{ Fill Name Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS

[<add>]()


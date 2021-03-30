---
external help file: PowerShellGet.dll-Help.xml
Module Name: PowerShellGet
online version:
schema: 2.0.0
---

# Register-PSResourceRepository

## SYNOPSIS
{{ Fill in the Synopsis }}

## SYNTAX

### NameParameterSet (Default)
```
Register-PSResourceRepository [-Name] <String> [-URL] <Uri> [-Trusted] [-Priority <Int32>] [-PassThru]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### PSGalleryParameterSet
```
Register-PSResourceRepository [-PSGallery] [-Trusted] [-Priority <Int32>] [-PassThru] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### RepositoriesParameterSet
```
Register-PSResourceRepository -Repositories <Hashtable[]> [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

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
Type: String
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Priority
{{ Fill Priority Description }}

```yaml
Type: Int32
Parameter Sets: NameParameterSet, PSGalleryParameterSet
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PSGallery
{{ Fill PSGallery Description }}

```yaml
Type: SwitchParameter
Parameter Sets: PSGalleryParameterSet
Aliases:

Required: True
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Repositories
{{ Fill Repositories Description }}

```yaml
Type: Hashtable[]
Parameter Sets: RepositoriesParameterSet
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Trusted
{{ Fill Trusted Description }}

```yaml
Type: SwitchParameter
Parameter Sets: NameParameterSet, PSGalleryParameterSet
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -URL
{{ Fill URL Description }}

```yaml
Type: Uri
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
{{ Fill PassThru Description }}

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Management.Automation.PSCredential
### System.Uri
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS

[<add>]()


function Sign-StringWithRSA {
    [CmdletBinding(PositionalBinding = $false,
        HelpUri = 'https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsacryptoserviceprovider?view=net-5.0',
        ConfirmImpact = 'Low')]
    [OutputType([string])]

    param (
        [Parameter(Mandatory = $true,
            Position = 0,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            ValueFromRemainingArguments = $false,
            ParameterSetName = 'String',
            HelpMessage = 'String to be signed')]
        [ValidateNotNull()]
        [ValidateNotNullOrEmpty()]
        [String]
        $String,

        [Parameter(Mandatory = $true,
            Position = 1,
            ValueFromPipeline = $false,
            ValueFromRemainingArguments = $false,
            ParameterSetName = 'String',
            HelpMessage = 'Private RSA key in XML format used for signing')]
        [ValidateNotNull()]
        [ValidateNotNullOrEmpty()]
        [String]
        $PrivateKey
    )

    begin {
        $signature = $null
        $rsaProvider = $null
        $sha512 = $null

        # String to byte
        $stringToSignBytes = [System.Text.Encoding]::UTF8.GetBytes($String)

        # create RSA Crypto Service Provider
        $rsaProvider = [System.Security.Cryptography.RSACryptoServiceProvider]::Create()
        $rsaProvider.PersistKeyInCsp = $false

        # Try importing the private key
        try {
            # imports private key into the provider object
            $rsaProvider.FromXmlString($PrivateKey)
        } catch {
            throw "Failed to import private RSA key. Ensure it is in valid XML format."
        }

        $sha512 = [System.Security.Cryptography.SHA512]::Create()
    }

    process {
        try {
            # encrypts string with rsa and converts to base64
            $signature = [System.Convert]::ToBase64String($rsaProvider.SignData($stringToSignBytes, $sha512))
        } catch {
            Write-Error "Error signing data: $_"
            throw
        }
    }

    end {
        $rsaProvider.PersistKeyInCsp = $false;
        $rsaProvider.Dispose()
        $sha512.Dispose()

        return $signature
    }
}

function Sign-VersionInfoFile {
    [CmdletBinding(SupportsShouldProcess = $true,
        PositionalBinding = $false,
        ConfirmImpact = 'Low')]
    param (
        [Parameter(Mandatory = $false,
            Position = 0,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            ValueFromRemainingArguments = $false,
            ParameterSetName = 'String',
            HelpMessage = 'Path to the version.info file')]
        [string]
        $Path = "version.info"
    )

    begin {
        $lines = @()

        if (Test-Path $Path) {
            # Read all lines, trim whitespace and remove empty lines
            $lines = (Get-Content -Path $Path | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" })
        } else {
            Write-Error "Aborting: '$Path' not found"
            return
        }

        # Check if there are at least three lines
        if ($lines.Count -lt 3) {
            Write-Error "Aborting: '$Path' must contain at least three non-empty lines"
            return
        }

        # Take the first character of the first line as separator
        $separator = $lines[0][0]

        # Loop through remaining lines and concatenate until a second line starting with separator is found
        $resultLines = @()
        $foundSecondSeparator = $false

        for ($i = 1; $i -lt $lines.Count; $i++) {
            if (!$foundSecondSeparator -and $lines[$i].StartsWith($separator)) {
                $foundSecondSeparator = $true
                break
            }
            $resultLines += $lines[$i]
        }

        # Abort if the second separator was not found
        if (-not $foundSecondSeparator) {
            Write-Error "Aborting: Second separator line not found in '$Path'"
            return
        }

        $concatenated = ($resultLines -join "`n") + "`n"
    }

    process {
        Write-Host "Signing '$Path'`n"

        # Prompt the user for their private RSA key
        Write-Host "Please enter your private RSA key (XML format):"
        $userPrivateKey = Read-Host
        $userPrivateKey = $userPrivateKey.Trim()
        Write-Host ""

        # Abort if the key does not start with <RSAKeyValue>
        if (-not $userPrivateKey.StartsWith("<RSAKeyValue>")) {
            Write-Error "Aborting: Private RSA key does not start with <RSAKeyValue>"
            return
        }

        # Sign the concatenated string
        $signature = Sign-StringWithRSA -String $concatenated -PrivateKey $userPrivateKey

        # Put the signature into version.info
        $lines[0] = $separator + $signature

        if ($PSCmdlet.ShouldProcess($Path)) {
            # Write all lines back to the file
            Set-Content -Path $Path -Value $lines
            Write-Host "Wrote signature to '$Path':"
            Write-Host $signature
        }
        else {
            Write-Host "Would write signature to '$Path':"
            Write-Host $signature
        }
    }
}

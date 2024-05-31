param (
    [string]$domain,
    [string]$key
)

# Function to parse TXT record
function Parse-TxtRecord {
    param (
        [string]$txtRecord
    )

    $result = @{}
    $parts = $txtRecord -split '=', 2
    if ($parts.Length -eq 2) {
        $key = $parts[0].Trim()
        $value = $parts[1].Trim()
        $result[$key] = $value
    }
    return $result
}

# Function to get TXT records for a domain and parse them
function Get-ParsedTxtRecords {
    param (
        [string]$domain
    )

    $records = Resolve-DnsName -Name $domain -Type TXT
    $parsedRecords = @{}

    foreach ($record in $records) {
        foreach ($txt in $record.strings) {
            $parsedRecord = Parse-TxtRecord -txtRecord $txt
            foreach ($key in $parsedRecord.Keys) {
                $parsedRecords[$key] = $parsedRecord[$key]
            }
        }
    }

    return $parsedRecords
}

if (-not $domain -or -not $key) {
    Write-Host "Usage: .\GetParsedTxtRecordValue.ps1 -domain <domain> -key <key>"
    exit 1
}

try {
    $parsedRecords = Get-ParsedTxtRecords -domain $domain
    if ($parsedRecords.ContainsKey($key)) {
        $value = $parsedRecords[$key]
        Write-Output $value
    } else {
        Write-Output ""
    }
} catch {
    Write-Host "Error fetching and parsing TXT records: $_"
    exit 1
}

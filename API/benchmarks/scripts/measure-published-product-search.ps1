param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,
    [string[]]$Queries = @('kolye', 'sonil', 'bulunmayan-urun'),
    [ValidateRange(1, 10000)]
    [int]$Iterations = 200,
    [ValidateRange(1, 128)]
    [int]$Concurrency = 16
)

$ErrorActionPreference = 'Stop'

# Burada ölçüm dizisindeki istenen yüzdelik gecikmeyi milisaniye olarak hesaplıyorum.
function Get-Percentile {
    param([double[]]$Values, [double]$Percentile)
    $ordered = $Values | Sort-Object
    $index = [Math]::Ceiling(($Percentile / 100) * $ordered.Count) - 1
    return $ordered[[Math]::Max(0, $index)]
}

# Burada tek suggestion isteğinin süre, durum ve payload boyutunu ölçüyorum.
function Invoke-MeasuredSearch {
    param([string]$Uri)
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing
    $watch.Stop()
    [PSCustomObject]@{
        Milliseconds = $watch.Elapsed.TotalMilliseconds
        StatusCode = [int]$response.StatusCode
        PayloadBytes = [Text.Encoding]::UTF8.GetByteCount($response.Content)
    }
}

$normalizedBaseUrl = $BaseUrl.TrimEnd('/')
$requests = for ($index = 0; $index -lt $Iterations; $index++) {
    $query = $Queries[$index % $Queries.Count]
    "$normalizedBaseUrl/api/products/published/search-suggestions?Query=$([Uri]::EscapeDataString($query))&Limit=10"
}

if ($PSVersionTable.PSVersion.Major -ge 7) {
    $results = $requests | ForEach-Object -Parallel {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri $_ -UseBasicParsing
        $watch.Stop()
        [PSCustomObject]@{
            Milliseconds = $watch.Elapsed.TotalMilliseconds
            StatusCode = [int]$response.StatusCode
            PayloadBytes = [Text.Encoding]::UTF8.GetByteCount($response.Content)
        }
    } -ThrottleLimit $Concurrency
    $effectiveConcurrency = $Concurrency
}
else {
    # Burada Windows PowerShell 5 üzerinde de tekrarlanabilir ölçüm için sıralı geri dönüşü kullanıyorum.
    $results = $requests | ForEach-Object { Invoke-MeasuredSearch -Uri $_ }
    $effectiveConcurrency = 1
}

$latencies = [double[]]($results | ForEach-Object Milliseconds)
[PSCustomObject]@{
    Requests = $results.Count
    Concurrency = $effectiveConcurrency
    Successes = @($results | Where-Object StatusCode -eq 200).Count
    P50Milliseconds = [Math]::Round((Get-Percentile $latencies 50), 2)
    P95Milliseconds = [Math]::Round((Get-Percentile $latencies 95), 2)
    P99Milliseconds = [Math]::Round((Get-Percentile $latencies 99), 2)
    MaximumPayloadBytes = ($results | Measure-Object PayloadBytes -Maximum).Maximum
} | Format-List

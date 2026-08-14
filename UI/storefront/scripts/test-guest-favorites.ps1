Add-Type -AssemblyName System.Net.Http

$published = Invoke-RestMethod -Uri "http://localhost:3300/api/products/published?pageNumber=1&pageSize=1" -Method Get
$productId = $published.items[0].id
if (-not $productId) { throw "Published product not found" }

# Burada her test kullanıcısı için cookie değerini çıktıya açmayan bağımsız bir HTTP oturumu oluşturuyorum.
function New-GuestClient {
  $handler = [System.Net.Http.HttpClientHandler]::new()
  $handler.UseCookies = $true
  $handler.CookieContainer = [System.Net.CookieContainer]::new()
  $client = [System.Net.Http.HttpClient]::new($handler)
  $client.BaseAddress = [Uri]"http://localhost:3000"
  return @{ Client = $client; Handler = $handler }
}

# Burada gerçek BFF çağrısının yalnız durum, gövde ve süre bilgisini ölçüp hassas headerları rapora almıyorum.
function Send-GuestRequest($guest, $method, $path, $mutation) {
  $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($method), $path)
  if ($mutation) { $request.Headers.Add("Origin", "http://localhost:3000") }
  $watch = [System.Diagnostics.Stopwatch]::StartNew()
  $response = $guest.Client.SendAsync($request).GetAwaiter().GetResult()
  $watch.Stop()
  $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
  return @{ Response = $response; Body = $body; Milliseconds = $watch.ElapsedMilliseconds }
}

$guestA = New-GuestClient
$guestB = New-GuestClient
$guestC = New-GuestClient

try {
  $initial = Send-GuestRequest $guestA "GET" "/api/favorites" $false
  $initialJson = $initial.Body | ConvertFrom-Json
  $added = Send-GuestRequest $guestA "POST" "/api/favorites/$productId" $true
  $afterAdd = Send-GuestRequest $guestA "GET" "/api/favorites" $false
  $afterAddJson = $afterAdd.Body | ConvertFrom-Json
  $duplicate = Send-GuestRequest $guestA "POST" "/api/favorites/$productId" $true
  $isolated = Send-GuestRequest $guestB "GET" "/api/favorites" $false
  $isolatedJson = $isolated.Body | ConvertFrom-Json
  $deleted = Send-GuestRequest $guestA "DELETE" "/api/favorites/$productId" $true
  $afterDelete = Send-GuestRequest $guestA "GET" "/api/favorites" $false
  $afterDeleteJson = $afterDelete.Body | ConvertFrom-Json
  $firstMutation = Send-GuestRequest $guestC "POST" "/api/favorites/$productId" $true
  $cleanup = Send-GuestRequest $guestC "DELETE" "/api/favorites/$productId" $true

  [pscustomobject]@{
    ProductId = $productId
    GuestGetStatus = [int]$initial.Response.StatusCode
    GuestInitiallyEmpty = $initialJson.totalCount -eq 0
    FirstPostStatus = [int]$added.Response.StatusCode
    ContainsAfterPost = $afterAddJson.productIds -contains $productId
    DuplicatePostStatus = [int]$duplicate.Response.StatusCode
    IsolatedSecondGuest = -not ($isolatedJson.productIds -contains $productId)
    DeleteStatus = [int]$deleted.Response.StatusCode
    AbsentAfterDelete = -not ($afterDeleteJson.productIds -contains $productId)
    PostWithoutPriorGetStatus = [int]$firstMutation.Response.StatusCode
    CleanupDeleteStatus = [int]$cleanup.Response.StatusCode
    CacheControl = [string]$afterAdd.Response.Headers.CacheControl
    GetMilliseconds = $afterAdd.Milliseconds
    PostMilliseconds = $added.Milliseconds
    DuplicateMilliseconds = $duplicate.Milliseconds
    DeleteMilliseconds = $deleted.Milliseconds
  }
} finally {
  $guestA.Client.Dispose()
  $guestA.Handler.Dispose()
  $guestB.Client.Dispose()
  $guestB.Handler.Dispose()
  $guestC.Client.Dispose()
  $guestC.Handler.Dispose()
}

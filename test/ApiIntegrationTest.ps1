$baseUrl = "http://localhost:8080"
$headers = @{ Accept = "application/json"}

################# Create application
$url = $baseUrl + "/Application/test"
$response = Invoke-RestMethod $url -Method Post -ContentType "application/json" -Headers $headers

################# Modify application, add file exclusions...
$url = $baseUrl + "/Application/test"
$body = @{ FileExclusionExpressions=("canary.html","health.html"); Enabled = $true; }
$response = Invoke-RestMethod $url -Method Put -ContentType "application/json" -Headers $headers -Body (ConvertTo-Json $body)

################# Get the application.
$url = $baseUrl + "/Application/test"
$response = Invoke-RestMethod $url -Headers $headers
$response.Name

############## Create a deployment.
$url = $baseUrl + "/Application/test/Deployment/local"
$body = @{ networkPath = "C:\inetpub" }
$response = Invoke-RestMethod $url -Method Post -ContentType "application/json" -Headers $headers -Body (ConvertTo-Json $body)

############# Get the deployment.
$url = $baseUrl + "/Application/test/Deployment/local"
$response = Invoke-RestMethod $url -Headers $headers
$response.Deployments

############# Create an audit.
$url = $baseUrl + "/Application/test/audit/local"
$response = Invoke-RestMethod $url -Headers $headers -Method Post -ContentType "application/json" 


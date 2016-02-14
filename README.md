# FileAuditManager
Creates and stores a hash of a windows directory and verifies that the contents have not changed.

``` powershell
PS C:\Windows\system32> $headers = @{ Accept = "application/json"; "Content-Type"="application/json"}
PS C:\Windows\system32> $baseUrl = "http://localhost:8080"
PS C:\Windows\system32> ## Applications.  Applications are just containers for deployments. Almost like a namespace.
PS C:\Windows\system32> $newApp = "MongoDB"
PS C:\Windows\system32> ##Create applications... When creating, you do NOT send a body.
PS C:\Windows\system32> Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application/$($newApp)" -Method Post
PS C:\Windows\system32> ## Update applications to ignore log files. Each File Exclusion Expression is a regular expression. If the regex generates a match, the file is excluded from the hash.
## Side note: the back-tick escapes the quote for powershell, the second backslash escapes the period(.) for RegEx, and the first backslash escapes the second backslash for JSON.
PS C:\Windows\system32> $body = " { Enabled:true, FileExclusionExpressions:[`".*logile\\.*$`"] }"
PS C:\Windows\system32> ## Use PUT to update the application:
PS C:\Windows\system32> Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application/$($newApp) " -Method Put -Body $body
PS C:\Windows\system32> ## Now we can get a list of applications...
PS C:\Windows\system32> $apps = Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application"
PS C:\Windows\system32> $apps.Count

1

PS C:\Windows\system32> $apps.Applications | ft

Name                                                                                                                    Enabled FileExclusionExpressions                                       
----                                                                                                                    ------- ------------------------                                       
MongoDB                                                                                                                    True {.*logile\.*$}                                                 



PS C:\Windows\system32>     ## Get a specific application
PS C:\Windows\system32> Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application/$($newApp)"

Name                                                                                                                    Enabled FileExclusionExpressions                                       
----                                                                                                                    ------- ------------------------                                       
MongoDB                                                                                                                    True {.*logile\.*$}                                                 

PS C:\Windows\system32> ## Now we have an application, we can create several deployments....
PS C:\Windows\system32> ## usually we would point to different servers, but for this example I'm just pointing these two deployments at the same path....
PS C:\Windows\system32> $deploymentServers = ("local1", "local2")
PS C:\Windows\system32> $deploymentServers | % {
    $server = $_
    ## Again, double backslashes needed for json escaping. Network path can actually be any valid path to a folder, even on the local machine.
    $body = "{ NetworkPath : `"c:\\Program Files\\$($newApp)`" }"
    ## The server name at the end of this URL does NOT need to match the server in the network path. Even though this parameter is identified as "ServerName", it can be any string you like.
    Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application/$($newApp)/deployment/$($server)" -Method Post -Body $body -TimeoutSec 500
}

PS C:\Windows\system32> ##Note: when you create a deployment, it will hash the folder. If the folder is big, it could take a while, which is why there's a 500 sec timeout.
PS C:\Windows\system32> ## Now we can GET our deployments...
PS C:\Windows\system32> $response = Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application/$($newApp)/deployment"
PS C:\Windows\system32> $response.Deployments | ft

DeploymentId            ApplicationName         ServerName              NetworkPath             Hash                    StartDateTime           EndDateTime             MostRecentAudit        
------------            ---------------         ----------              -----------             ----                    -------------           -----------             ---------------        
92ec7e5e-2fb5-4897-8... MongoDB                 local2                  c:\Program Files\Mon... 0E27B7A681909E47B861... 2016-02-09T14:35:33.... 9999-12-31T23:59:59.... 00000000-0000-0000-0...
49756b17-cc08-464f-b... MongoDB                 local1                  c:\Program Files\Mon... 0E27B7A681909E47B861... 2016-02-09T14:35:21.... 9999-12-31T23:59:59.... 00000000-0000-0000-0...

PS C:\Windows\system32> ## Once a deployment exists, the timer in the application will create an audit for the deployment based on the timer in the config.
PS C:\Windows\system32> ## However you can also manually create an audit for an application:
PS C:\Windows\system32> $deploymentServers | % {
    $server = $_
    Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application/$($newApp)/audit/$($server)" -Method Post -TimeoutSec 500
}

PS C:\Windows\system32> ## Now we can view the audits for an application
## When you don't specify a server in the url, it returns the most recent audit for each server in the application.

PS C:\Windows\system32> $audits = Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application/$($newApp)/audit"
PS C:\Windows\system32> $audits.Audits | ft

DeploymentId            ServerName              NetworkPath             DeploymentStartDateTime DeploymentHash          AuditDateTime           AuditHash                             ValidHash
------------            ----------              -----------             ----------------------- --------------          -------------           ---------                             ---------
92ec7e5e-2fb5-4897-8... local2                  c:\Program Files\Mon... 2016-02-09T14:35:33.... 0E27B7A681909E47B861... 2016-02-09T14:36:28.... 0E27B7A681909E47B861...                    True
49756b17-cc08-464f-b... local1                  c:\Program Files\Mon... 2016-02-09T14:35:21.... 0E27B7A681909E47B861... 2016-02-09T14:36:24.... 0E27B7A681909E47B861...                    True

PS C:\Windows\system32> ## However if you do specify a server, it will return all the audits for that application+server in reverse chrono order.
PS C:\Windows\system32> $audits = Invoke-RestMethod -Headers $headers -Uri "$($baseUrl)/Application/$($newApp)/audit/$($deploymentServers[0])"
PS C:\Windows\system32> $audits.Audits | ft

DeploymentId            ServerName              NetworkPath             DeploymentStartDateTime DeploymentHash          AuditDateTime           AuditHash                             ValidHash
------------            ----------              -----------             ----------------------- --------------          -------------           ---------                             ---------
49756b17-cc08-464f-b... local1                  c:\Program Files\Mon... 2016-02-09T14:35:21.... 0E27B7A681909E47B861... 2016-02-09T14:36:24.... 0E27B7A681909E47B861...                    True


PS C:\Windows\system32> 

```

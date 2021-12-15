param (
    [switch]
    $Automated = $false
)

Push-Location

Set-Location .\Project
try {
    & dotnet publish

    Pop-Location
    $popped = $true

    $page = Get-Item .\global.html

    if (-not $Automated) {
        & MicrosoftEdge.exe $page.FullName

        $title = 'Confirmation'
        $question = 'Was publishing successful?'

        $choices = New-Object Collections.ObjectModel.Collection[Management.Automation.Host.ChoiceDescription]
        $choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList '&Yes'))
        $choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList '&No'))

        $decision = $Host.UI.PromptForChoice($title, $question, $choices, 1)
    }
    else {
        $decision = 0;
    }

    if ($decision -eq 0) {
        & node common.js

        if($LASTEXITCODE -ne 0) {
            throw "`$LASTEXITCODE from common.js is $LASTEXITCODE.";
        }

        & node es.mjs

        if($LASTEXITCODE -ne 0) {
            throw "`$LASTEXITCODE from es.mjs is $LASTEXITCODE.";
        }
    }
    else {
        ""
        "---"
        "Cancelled by user input."
        ""        
    }

    $LASTEXITCODE = 0;
}
finally {
    if (!$popped) {
        Pop-Location
    }
}
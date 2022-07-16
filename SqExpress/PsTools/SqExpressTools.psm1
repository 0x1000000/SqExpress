$ErrorActionPreference = 'Stop'

function Gen-Tables
{
    [CmdletBinding(PositionalBinding = $false)]
    param(
        [ValidateSet('mssql','mysql','pgsql')]
        [Parameter(Position = 0, Mandatory = $true)]
        [string] $DbType,
        [Parameter(Position = 1, Mandatory = $true)]
        [string] $ConnectionString,
        [string] $OutputDir,
        [string] $TableClassPrefix,
        [string] $Namespace
    )

    CheckSqExpressReference

    $args = "gentables $DbType ""$ConnectionString"""

    #OutputDir
    if(!$OutputDir)
    {
        $OutputDir = "Tables"
    }
    if($OutputDir -ne "")
    {
        $args = $args + " -o " + $OutputDir
    }

    #TableClassPrefix
    if($TableClassPrefix)
    {
        $args = $args + " --table-class-prefix " + $TableClassPrefix
    }

    #Namespace
    if(!$Namespace)
    {
        $Namespace = GetCurrentProjectProperty "ProjectName"

        if($Namespace)
        {
            if($OutputDir -and $OutputDir -ne "")
            {
                $Namespace = $Namespace + "." + $OutputDir.Replace('\','.').Replace('/','.')
            }
            else
            {
                $Namespace = $Namespace + ".Tables"
            }            
        }
    }
    if($Namespace)
    {
        $args = $args + " -n " + $Namespace
    }

    CodeGenUtil $args
}

function Gen-Models
{
    [CmdletBinding(PositionalBinding = $false)]
    param(
        [string] $InputDir,
        [string] $OutputDir,
        [string] $Namespace,
        [switch] $NoRwClasses,
        [string] $NullRefTypes,
        [switch] $CleanOutput,
        [ValidateSet('ImmutableClass','Record')]
        [string] $ModelType
    )

    CheckSqExpressReference

    $args = "genmodels"

    #InputDir
    if(!$InputDir)
    {
        $InputDir = GetCurrentProjectProperty "SqModelGenInput"
        if(!$InputDir)
        {
            $InputDir = "."
        }
    }
    $args = $args + " -i " + $InputDir

    #OutputDir
    if(!$OutputDir)
    {
        $OutputDir = GetCurrentProjectProperty "SqModelGenOutput"
        if(!$OutputDir)
        {
            $OutputDir = "Models"
        }
    }
    $args = $args + " -o " + $OutputDir

    #Namespace
    if(!$Namespace)
    {
        $Namespace = GetCurrentProjectProperty "SqModelGenNamespace"
        if($Namespace) {
            $args = $args + " -n " + $Namespace
        }
    }

    #NoRwClasses
    if(!($NoRwClasses.IsPresent))
    {
        $args = $args + " --rw-classes"
    }

    #NullRefTypes
    if($NullRefTypes.IsPresent -or (GetCurrentProjectProperty "Nullable") -eq "enable")
    {
        $args = $args + " --null-ref-types"
    }
  
    #CleanOutput
    if($CleanOutput.IsPresent -or (GetCurrentProjectProperty "SqModelGenCleanOutput") -eq "True")
    {
        $args = $args + " --clean-output"
    }

    if(!$ModelType)
    {
        $ModelType = GetCurrentProjectProperty "SqModelGenType"
    }
    if($ModelType)
    {
        $args = $args + " --model-type " + $ModelType
    }

    CodeGenUtil $args
}

function CodeGenUtil($arguments){

    $cmd = [IO.Path]::Combine($PSScriptRoot, "codegen", "SqExpress.CodeGenUtil.dll")

    if(!([IO.File]::Exists($exePath)))
    {
        #WriteErrorMessage ("Could not find SqExpress codegen util at " + $cmd)
        #exit
    }

    $cmd = "C:\Users\x1000\.nuget\packages\sqexpress\0.3.3\tools\codegen\SqExpress.CodeGenUtil.dll"

    $cmd = """$cmd"" " + $arguments

    $dotnetCommand = Get-Command "dotnet"

    if(!$dotnetCommand)
    {
        WriteErrorMessage "Could not find .Net Core"
        exit
    }

    if($dotnetCommand.Version -lt "3.1")
    {
        WriteErrorMessage ".Net Core 3.1 or higher is required"
        exit
    }

    $startInfo = New-Object 'System.Diagnostics.ProcessStartInfo' -Property @{
        FileName = $dotnetCommand.Path;
        Arguments = $cmd;
        UseShellExecute = $false;
        CreateNoWindow = $true;
        RedirectStandardOutput = $true;
        StandardOutputEncoding = [Text.Encoding]::UTF8;
        RedirectStandardError = $true;
        WorkingDirectory = (GetCurrentProjectDir);
    }

    Write-Host "dotnet $cmd"

    $process = [Diagnostics.Process]::Start($startInfo)

    while (($line = $process.StandardOutput.ReadLine()) -ne $null)
    {
        Write-Host $line
    }

    $process.WaitForExit()

    if ($process.ExitCode)
    {
        while (($line = $process.StandardError.ReadLine()) -ne $null)
        {
            WriteErrorMessage $line $true
        }
        exit
    }
}

function CheckSqExpressReference
{
    $pReferences = (GetCurrentProjectServices).PackageReferences.GetItemsAsync().Result

    $sqExpressRef = $pReferences | where {$_.EvaluatedInclude -eq "SqExpress"} | Select -First 1

    if(!$sqExpressRef)
    {
        WriteErrorMessage "Could not find a Package References to SqExpress in the selected project"
        exit
    }
}

function GetCurrentProjectProperty($propertyName)
{
    $projectServices = GetCurrentProjectServices
    $properties = $projectServices.ProjectPropertiesProvider.GetCommonProperties()

    return $properties.GetEvaluatedPropertyValueAsync($propertyName).Result
}

function GetCurrentProjectServices
{
    return (Get-Project).UnconfiguredProject.GetSuggestedConfiguredProjectAsync().Result.Services
}

function GetCurrentProjectDir
{
    return Split-Path (Get-Project).FileName -Parent
}

function WriteErrorMessage($message, $noPrefix)
{
    if(!$noPrefix)
    {
        $message = "Error: " + $message
    } 

    Write-Host $message -ForegroundColor DarkRed    
}


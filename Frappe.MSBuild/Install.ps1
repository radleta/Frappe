param (
    $InstallPath,
    $ToolsPath,
    $Package,
    $Project
)

$TargetsFile = 'Frappe.targets'
$TargetsPath = $ToolsPath | Join-Path -ChildPath $TargetsFile

Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

$MSBProject = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($Project.FullName) |
    Select-Object -First 1

$ProjectUri = New-Object -TypeName Uri -ArgumentList "file://$($Project.FullName)"
$TargetUri = New-Object -TypeName Uri -ArgumentList "file://$TargetsPath"

$RelativePath = $ProjectUri.MakeRelativeUri($TargetUri) -replace '/','\'

$ExistingImports = $MSBProject.Xml.Imports |
    Where-Object { $_.Project -like "*\$TargetsFile" }
if ($ExistingImports) {
    $ExistingImports | 
        ForEach-Object {
            $MSBProject.Xml.RemoveChild($_) | Out-Null
        }
}
$MSBProject.Xml.AddImport($RelativePath) | Out-Null
$Project.Save()

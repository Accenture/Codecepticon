function %_FUNCTION_% {
    param(
        [string] $%_TEXT_%
    )
    
    $%_MAPPING_VAR_% = New-Object System.Collections.Hashtable
    %MAPPING%
    
    $%_OUTPUT_% = ""
    
    $%_MAPLENGTH_% = $($%_MAPPING_VAR_%.Keys)[0].length
    for ($%_i_% = 0; $%_i_% -lt $%_TEXT_%.length; $%_i_% += $%_MAPLENGTH_%) {
        $%_OUTPUT_% += [char]$%_MAPPING_VAR_%[$%_TEXT_%.SubString($%_i_%, $%_MAPLENGTH_%)]
    }
    
    $%_OUTPUT_%
}

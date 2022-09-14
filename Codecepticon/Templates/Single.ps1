function %_FUNCTION_% {
    param(
        [string] $%_TEXT_%
    )
    
    $%_MAPPING_VAR_% = New-Object System.Collections.Hashtable
    %MAPPING%
    
    $%_OUTPUT_% = ""
    for ($%_i_% = 0; $%_i_% -lt $%_TEXT_%.length; $%_i_%++) {
        $%_c_% = $%_TEXT_%.SubString($%_i_%, 1)
        if ($%_MAPPING_VAR_%.ContainsKey($%_c_%)) {
            $%_OUTPUT_% += $%_MAPPING_VAR_%[$%_c_%]
        } else {
            $%_OUTPUT_% += $%_c_%
        }
    }
    
    $%_OUTPUT_%
}

function %_FUNCTION_% {
    param(
        [string] $%_text_%,
        [string] $%_key_%
    )
    
    $%_data_% = [System.Convert]::FromBase64String($%_text_%)
    $%_output_% = @()
    for ($%_i_% = 0; $%_i_% -lt $%_data_%.length; $%_i_%++) {
        $%_output_% += $%_data_%[$%_i_%] -bxor ([byte]($%_key_%[$%_i_% % $%_key_%.length]))
    }
    
    [System.Text.Encoding]::UTF8.GetString($%_output_%)
}
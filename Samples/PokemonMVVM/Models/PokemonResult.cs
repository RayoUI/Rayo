using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Pokemon.Models;

public class PokemonResult 
{ 
    [JsonPropertyName("name")] 
    public string Name { get; set; } 

    [JsonPropertyName("url")] 
    public string Url { get; set; } 
}

public class PokemonApiResponse
{
    [JsonPropertyName("results")]
    public List<PokemonResult> Results { get; set; }
}
using Pokemon.Services.Interfaces;
using Rayo.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Pokemon.Services;

public class PokeService : IPokeService
{
    public async Task<IEnumerable<string>> GetPokemonsAsync()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("https://pokeapi.co/api/v2/pokemon?limit=15&offset=0");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = System.Text.Json.JsonSerializer.Deserialize<Models.PokemonApiResponse>(json);
        var names = new List<string>();

        if (result?.Results != null)
        {
            foreach (var p in result.Results)
            {
                if (!string.IsNullOrEmpty(p.Name))
                    names.Add(p.Name);
            }
        }

        return names;
    }

    public async Task<string> GetPokemonSpriteUrl(string pokemonName)
    {
        string url = $"https://pokeapi.co/api/v2/pokemon/{pokemonName.ToLower()}";

        using HttpClient client = new HttpClient();
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);
            string spriteUrl = doc.RootElement
                                  .GetProperty("sprites")
                                  .GetProperty("front_default")
                                  .GetString();

            return spriteUrl ?? string.Empty;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error al obtener datos: {e.Message}");
            throw;
        }
    }
}

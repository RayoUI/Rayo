using System;
using System.Collections.Generic;
using System.Text;

namespace Pokemon.Services.Interfaces;

public interface IPokeService
{
    public Task<IEnumerable<string>> GetPokemonsAsync();
    public Task<string> GetPokemonSpriteUrl(string pokemonName);
}

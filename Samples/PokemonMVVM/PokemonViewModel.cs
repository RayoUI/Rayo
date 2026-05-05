#nullable enable

using Pokemon.Services.Interfaces;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Reactivity;

namespace Pokemon;

public class PokemonViewModel : ViewModelBase
{
    [Inject] public ILoggerService? Logger { get; private set; }
    [Inject] public IPokeService? PokeService { get; private set; }


    // Reactive properties for UI binding
    public SignalList<string> Items { get; private set; } = new();
    public Signal<ImageSource?> Avatar { get; private set; } = new(null);
    public Signal<bool> IsLoading { get; private set; } = new(false);
    public Signal<bool> IsLoadingSprite { get; private set; } = new(false);
    public Signal<string?> SelectedPokemon { get; private set; } = new(null);

    // Computed properties for UI display
    public Computed<string> DisplayTitle { get; private set; } = new(() => "Select a Pokemon");
    public Computed<bool> ShowLoading { get; private set; } = new(() => false);
    public Computed<bool> ShowImage { get; private set; } = new(() => true);

    protected override void OnInitialized()
    {
        Logger?.LogInfo("PokemonViewModel initialized");

        // Initialize computed properties after signals are ready
        DisplayTitle = new Computed<string>(() =>
        {
            var name = SelectedPokemon.Value;
            return string.IsNullOrEmpty(name) ? "Select a Pokemon" : name.ToUpperInvariant();
        });

        ShowLoading = new Computed<bool>(() => IsLoadingSprite.Value);
        ShowImage = new Computed<bool>(() => !IsLoadingSprite.Value);
    }

    public async Task LoadPokemonsAsync()
    {
        if (IsLoading.Value) return;

        try
        {
            IsLoading.Value = true;
            Logger?.LogInfo("Loading pokemons...");

            if (PokeService != null)
            {
                var pokemons = await PokeService.GetPokemonsAsync().ConfigureAwait(false);
                Logger?.Log($"Before updating Items. Current count: {Items.Count}");
                Items.Clear();
                foreach (var pokemon in pokemons)
                {
                    Items.Add(pokemon);
                }
                Logger?.Log($"After updating Items. New count: {Items.Count}");
                Logger?.LogInfo($"Loaded {pokemons.Count()} pokemons");
            }
            else
            {
                Logger?.LogWarn("PokeService is not available");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"Error loading pokemons: {ex.Message}");
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    public async Task LoadPokemonSpriteAsync(string pokemonName)
    {
        try
        {
            SelectedPokemon.Value = pokemonName;
            IsLoadingSprite.Value = true;
            Avatar.Value = null;

            Logger?.LogInfo($"Loading sprite for {pokemonName}...");

            // Simulate loading delay
            await Task.Delay(500).ConfigureAwait(false);

            if (PokeService != null)
            {
                var spriteUrl = await PokeService.GetPokemonSpriteUrl(pokemonName).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(spriteUrl))
                {
                    Avatar.Value = new UriImageSource(spriteUrl)
                    {
                        CacheValidity = TimeSpan.FromDays(1),
                        CachingEnabled = true
                    };
                    Logger?.LogInfo($"Sprite loaded for {pokemonName}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"Error loading sprite for {pokemonName}: {ex.Message}");
        }
        finally
        {
            IsLoadingSprite.Value = false;
        }
    }

    public void ClearAll()
    {
        Items.Clear();
        Avatar.Value = null;
        SelectedPokemon.Value = null;
        Logger?.LogInfo("Cleared all data");
    }

    public void RemoveFirstItem()
    {
        if (Items.Count > 0)
        {
            Items.RemoveAt(0);
            Avatar.Value = null;
            SelectedPokemon.Value = null;
        }
    }
}

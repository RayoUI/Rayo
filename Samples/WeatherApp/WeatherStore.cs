namespace WeatherApp;

public sealed record HourForecast(string Time, string Icon, int Temperature);
public sealed record DayForecast(string Day, string Icon, int High, int Low, int RainChance);
public sealed record FavoriteLocation(string Name, string Country, string Icon, int Fahrenheit, int Rain, int Humidity);
public sealed record WeatherMetric(string Title, string Value, string Station, string Icon);

public sealed class WeatherStore
{
    public string Location { get; set; } = "St. Louis, Missouri USA";
    public string Units { get; set; } = "imperial";
    public bool IsDark { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public List<HourForecast> Hours { get; } =
    [
        new("9 AM", "fluent_weather_rain_showers_day_20_filled.svg", 47),
        new("10 AM", "fluent_weather_rain_showers_day_20_filled.svg", 47),
        new("11 AM", "fluent_weather_rain_showers_day_20_filled.svg", 48),
        new("12 PM", "fluent_weather_cloudy_20_filled.svg", 49),
        new("1 PM", "fluent_weather_cloudy_20_filled.svg", 52),
        new("2 PM", "fluent_weather_cloudy_20_filled.svg", 53),
        new("3 PM", "fluent_weather_partly_cloudy.svg", 58),
        new("4 PM", "fluent_weather_sunny_20_filled.svg", 63),
        new("5 PM", "fluent_weather_sunny_20_filled.svg", 64),
        new("6 PM", "fluent_weather_sunny_20_filled.svg", 65),
        new("7 PM", "fluent_weather_moon_16_filled.svg", 60),
        new("8 PM", "fluent_weather_moon_16_filled.svg", 58)
    ];

    public List<DayForecast> Week { get; } =
    [
        new("Thu", "fluent_weather_sunny_high_20_filled.svg", 77, 52, 13),
        new("Fri", "fluent_weather_partly_cloudy.svg", 82, 61, 8),
        new("Sat", "fluent_weather_rain_showers_day_20_filled.svg", 77, 62, 46),
        new("Sun", "fluent_weather_thunderstorm_20_filled.svg", 80, 57, 61),
        new("Mon", "fluent_weather_thunderstorm_20_filled.svg", 61, 49, 72),
        new("Tue", "fluent_weather_partly_cloudy.svg", 68, 49, 18),
        new("Wed", "fluent_weather_rain_showers_day_20_filled.svg", 67, 47, 43)
    ];

    public List<FavoriteLocation> Favorites { get; } =
    [
        new("Redmond, WA", "USA", "fluent_weather_rain_showers_day_20_filled.svg", 64, 13, 45),
        new("St. Louis, MO", "USA", "fluent_weather_moon_16_filled.svg", 72, 8, 52),
        new("Boston, MA", "USA", "fluent_weather_cloudy_20_filled.svg", 54, 22, 63),
        new("Madrid", "Spain", "fluent_weather_sunny_20_filled.svg", 71, 2, 31)
    ];

    public List<WeatherMetric> Metrics { get; } =
    [
        new("Humidity", "78%", "Pond Elementary", "humidity_icon.svg"),
        new("Rain", "0.2 in", "Pond Elementary", "rain_icon.svg"),
        new("Chance of rain", "2%", "County Library", "umbrella_icon.svg"),
        new("Wind", "9 mph", "Pond Elementary", "wind_icon.svg"),
        new("Humidity", "78%", "City Hall", "humidity_icon.svg"),
        new("Rain", "0.2 in", "Rockwood Reservation", "rain_icon.svg")
    ];

    public int DisplayTemperature(int fahrenheit) =>
        Units == "imperial" ? fahrenheit : (int)Math.Round((fahrenheit - 32) * 5d / 9d);

    public string TemperatureUnit => Units == "imperial" ? "°F" : "°C";
}

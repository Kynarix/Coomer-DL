using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoomerDownloader.Services
{
    public class FavoritesManager
    {
        private static readonly string FavoritesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CoomerDownloader",
            "favorites.json"
        );

        public FavoritesManager()
        {
            // Klasörü oluştur
            var directory = Path.GetDirectoryName(FavoritesFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
        }

        public List<Creator> GetFavorites()
        {
            try
            {
                if (System.IO.File.Exists(FavoritesFilePath))
                {
                    var json = System.IO.File.ReadAllText(FavoritesFilePath);
                    return JsonConvert.DeserializeObject<List<Creator>>(json) ?? new List<Creator>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Favorites] Error loading favorites: {ex.Message}");
            }
            
            return new List<Creator>();
        }

        public void AddFavorite(Creator creator)
        {
            var favorites = GetFavorites();
            
            // Zaten favori listesinde mi kontrol et
            if (!favorites.Any(f => f.id == creator.id && f.service == creator.service))
            {
                favorites.Add(creator);
                SaveFavorites(favorites);
                Console.WriteLine($"[Favorites] Added: {creator.name} ({creator.service})");
            }
        }

        public void RemoveFavorite(Creator creator)
        {
            var favorites = GetFavorites();
            var toRemove = favorites.FirstOrDefault(f => f.id == creator.id && f.service == creator.service);
            
            if (toRemove != null)
            {
                favorites.Remove(toRemove);
                SaveFavorites(favorites);
                Console.WriteLine($"[Favorites] Removed: {creator.name} ({creator.service})");
            }
        }

        public bool IsFavorite(Creator creator)
        {
            var favorites = GetFavorites();
            return favorites.Any(f => f.id == creator.id && f.service == creator.service);
        }

        private void SaveFavorites(List<Creator> favorites)
        {
            try
            {
                var json = JsonConvert.SerializeObject(favorites, Formatting.Indented);
                System.IO.File.WriteAllText(FavoritesFilePath, json);
                Console.WriteLine($"[Favorites] Saved {favorites.Count} favorites");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Favorites] Error saving favorites: {ex.Message}");
            }
        }
    }
}

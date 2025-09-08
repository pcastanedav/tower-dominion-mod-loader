using System.Reflection;
using System.Security.Cryptography;

namespace TDModLoader.Handlers.Utils;
using MelonLoader;

public static class FileSystem
{
   private static readonly SHA256 Hasher = SHA256.Create();
   private static readonly string GameRoot = MelonLoader.Utils.MelonEnvironment.GameRootDirectory;
   private static readonly string ModsRoot = Path.Combine(GameRoot, "Mods");
   private static readonly string SourcesRoot = Path.Combine(ModsRoot, "Mods", "Sources");
   private static readonly Bijection<string, string> FilesIndex = new();
   private static bool _indexing;
   private static bool _completeIndex;
   
   static FileSystem()
   {
      if (!Directory.Exists(SourcesRoot)) Directory.CreateDirectory(SourcesRoot);
      else _ = FileSystem.LoadIndex();
   }
   private static async Task LoadIndex()
   {
      var dir = new DirectoryInfo(SourcesRoot);
      var files = dir.GetFiles();
      if (files.Length == 0) return;
      _indexing = true;
      _completeIndex = false;
      foreach (var fileInfo in dir.GetFiles())
      {
         await using var file = fileInfo.OpenRead();
         Hasher.Initialize();
         var hash = await Hasher.ComputeHashAsync(file);
         var hashString = BitConverter.ToString(hash).Replace("-", "");
         FilesIndex.Add(fileInfo.FullName, hashString);
      }
      _completeIndex = true;
      _indexing = false;
   }

   public static bool IsIndexing => _indexing;
   public static bool IsIndexed => _completeIndex;
   
}
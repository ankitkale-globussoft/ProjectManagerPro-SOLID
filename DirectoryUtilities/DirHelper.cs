using Newtonsoft.Json;
using ProjectManagerPro_SOLID.Models;
using System.IO;


namespace ProjectManagerPro_SOLID.DirectoryUtilities
{
    public static class DirHelper
    {
        private static string BaseDir => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).CombinePath("ProjectManagerPro");
        public static User GetUser()
        {
            CreateDir(BaseDir);
            var file = BaseDir.CombinePath("UserInfo.json");
            if (File.Exists(file))
                return File.ReadAllText(file).Deserialize<User>();
            return null;
        }
        public static void SaveInfo(User user)
        {
            File.WriteAllText(BaseDir.CombinePath("UserInfo.json"), user.Serialize());
        }
        public static string CombinePath(this string RootPath, params string[] Paths)
        {
            return Path.Combine(RootPath, Path.Combine(Paths));
        }
        public static bool CreateDir(string Path)
        {
            try
            {
                if (!Directory.Exists(Path))
                    Directory.CreateDirectory(Path);
                return true;
            }
            catch { return false; }
        }
        public static T Deserialize<T>(this string json)
            => JsonConvert.DeserializeObject<T>(json);
        public static string Serialize<T>(this T obj)
            => JsonConvert.SerializeObject(obj);
        public static T DeepClone<T>(this T obj) =>
            obj.Serialize().Deserialize<T>();
    }
}

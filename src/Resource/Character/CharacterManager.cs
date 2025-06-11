using EmeralEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmeralEngine.Resource.Character
{
    public class CharacterManager
    {
        public string baseDir;
        public CharacterManager()
        {
            baseDir = Path.Combine(MainWindow.pmanager.ProjectResourceDir, "Characters");
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
        }

        public string Combine(string name)
        {
            return Path.Combine(baseDir, name);
        } 

        public string Format(string path)
        {
            return Path.GetRelativePath(baseDir, path);
        }

        public string[] GetCharacterNames()
        {
            return Directory.GetDirectories(baseDir)
                            .Select(n => Path.GetFileName(n)).ToArray();
        }
        public string[] GetCharacterPictureFiles(string name)
        {
            return Directory.GetFiles(Path.Combine(baseDir, name))
                            .Where(n => !n.EndsWith("description.txt")).ToArray();
        }
        public void NewCharacter(string name)
        {
            Directory.CreateDirectory(Path.Combine(baseDir , name));
        }
        public void AddPicture(string name, string path)
        {
            var cropped = ImageUtils.CropTransparentEdges(ImageUtils.LoadImage(path));
            if (cropped is not null)
            {
                ImageUtils.SaveImage(cropped, Utils.GetUnusedFileName(Path.Combine(baseDir, name, Path.GetFileName(path))));
            }
        }
        public void RemovePicture(string name, string path)
        {
            File.Copy(path, Utils.GetUnusedFileName(Path.Combine(baseDir, name, Path.GetFileName(path))));
        }
        public void MovePicture(string name, string from, string to)
        {
            File.Move(Path.Combine(baseDir, name, from), Path.Combine(baseDir, name, to));
        }
        public bool ExistsPictureName(string name, string n)
        {
            return File.Exists(Path.Combine(baseDir, name, n));
        }
        public string GetCharacterPicturePath(string name, string pic)
        {
            return Path.Combine(baseDir, name, pic);
        }
    }
}

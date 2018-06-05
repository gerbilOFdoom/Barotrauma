﻿using Facepunch.Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Barotrauma
{
    public enum ContentType
    {
        None, 
        Jobs, 
        Item, 
        Character, 
        Structure, 
        Executable, 
        LocationTypes, 
        LevelGenerationParameters,
        RandomEvents, 
        Missions, 
        BackgroundCreaturePrefabs, BackgroundSpritePrefabs,
        Sounds,
        RuinConfig,
        Particles,
        Decals,
        NPCConversations,
        Afflictions,
        Tutorials,
        UIStyle
    }

    public class ContentPackage
    {
        public static string Folder = "Data/ContentPackages/";

        public static List<ContentPackage> List = new List<ContentPackage>();

        //these types of files are included in the MD5 hash calculation,
        //meaning that the players must have the exact same files to play together
        private HashSet<ContentType> multiplayerIncompatibleContent = new HashSet<ContentType>
        {
            ContentType.Jobs,
            ContentType.Item,
            ContentType.Character,
            ContentType.Structure,
            ContentType.LocationTypes,
            ContentType.LevelGenerationParameters,
            ContentType.RandomEvents, //TODO: is it enough if only the server has the random event configs?
            ContentType.Missions,
            ContentType.BackgroundSpritePrefabs,
            ContentType.RuinConfig,
            ContentType.Afflictions
        };
        
        private string name;
        public string Name
        {
            get { return name; }
        }

        public string Path
        {
            get;
            private set;
        }

        private Md5Hash md5Hash;
        public Md5Hash MD5hash
        {
            get 
            {
                if (md5Hash == null) CalculateHash();
                return md5Hash; 
            }
        }

        public List<ContentFile> files;

        private ContentPackage()
        {
            files = new List<ContentFile>();
        }

        public ContentPackage(string filePath)
            : this()
        {
            XDocument doc = XMLExtensions.TryLoadXml(filePath);

            Path = filePath;

            if (doc?.Root == null)
            {
                DebugConsole.ThrowError("Couldn't load content package \"" + filePath + "\"!");
                return;
            }

            name = doc.Root.GetAttributeString("name", "");

            foreach (XElement subElement in doc.Root.Elements())
            {
                if (!Enum.TryParse(subElement.Name.ToString(), true, out ContentType type))
                {
                    DebugConsole.ThrowError("Error in content package \"" + name + "\" - \"" + subElement.Name.ToString() + "\" is not a valid content type.");
                    continue;
                }
                files.Add(new ContentFile(subElement.GetAttributeString("file", ""), type));
            }
        }

        public override string ToString()
        {
            return name;
        }

        public static ContentPackage CreatePackage(string name)
        {
            ContentPackage newPackage = new ContentPackage("Content/Data/" + name)
            {
                name = name,
                Path = Folder + name
            };
            List.Add(newPackage);

            return newPackage;
        }

        public ContentFile AddFile(string path, ContentType type)
        {
            if (files.Find(file => file.Path == path && file.Type == type) != null) return null;

            ContentFile cf = new ContentFile(path, type);
            files.Add(cf);

            return cf;
        }

        public void RemoveFile(ContentFile file)
        {
            files.Remove(file);
        }

        public void Save(string filePath)
        {
            XDocument doc = new XDocument();
            doc.Add(new XElement("contentpackage",
                new XAttribute("name", name),
                new XAttribute("path", Path)));

            foreach (ContentFile file in files)
            {
                doc.Root.Add(new XElement(file.Type.ToString(), new XAttribute("file", file.Path)));
            }

            doc.Save(System.IO.Path.Combine(filePath, name + ".xml"));
        }

        private void CalculateHash()
        {
            List<byte[]> hashes = new List<byte[]>();
            
            var md5 = MD5.Create();
            foreach (ContentFile file in files)
            {
                if (!multiplayerIncompatibleContent.Contains(file.Type)) continue;

                try 
                {
                    using (var stream = File.OpenRead(file.Path))
                    {
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, (int)stream.Length);
                        if (file.Path.EndsWith(".xml", true, System.Globalization.CultureInfo.InvariantCulture))
                        {
                            string text = System.Text.Encoding.UTF8.GetString(data);
                            text = text.Replace("\n", "").Replace("\r", "");
                            data = System.Text.Encoding.UTF8.GetBytes(text);
                        }
                        hashes.Add(md5.ComputeHash(data));
                    }
                }

                catch (Exception e)
                {
                    DebugConsole.ThrowError("Error while calculating content package hash: ", e);
                }
             
            }
            
            byte[] bytes = new byte[hashes.Count * 16];
            for (int i = 0; i < hashes.Count; i++)
            {
                hashes[i].CopyTo(bytes, i * 16);
            }

            md5Hash = new Md5Hash(bytes);
        }

        public List<string> GetFilesOfType(ContentType type)
        {
            List<ContentFile> contentFiles = files.FindAll(f => f.Type == type);

            List<string> filePaths = new List<string>();
            foreach (ContentFile contentFile in contentFiles)
            {
                filePaths.Add(contentFile.Path);
            }
            return filePaths;
        }

        public static void LoadAll(string folder)
        {
            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception e)
                {
                    DebugConsole.ThrowError("Failed to create directory \"" + folder + "\"", e);
                    return;
                }
            }

            string[] files = Directory.GetFiles(folder, "*.xml");

            List.Clear();

            foreach (string filePath in files)
            {
                ContentPackage package = new ContentPackage(filePath);
                List.Add(package);
            }
        }
    }

    public class ContentFile
    {
        public readonly string Path;
        public readonly ContentType Type;

        public Workshop.Item WorkShopItem;

        public ContentFile(string path, ContentType type, Workshop.Item workShopItem = null)
        {
            Path = path;
            Type = type;
            WorkShopItem = workShopItem;
        }

        public override string ToString()
        {
            return Path;
        }
    }

}

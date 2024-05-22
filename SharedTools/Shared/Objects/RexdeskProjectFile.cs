﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Influx.Shared.Objects
{
    public enum EntryFileType : byte
    {
        General,
        ConfigXml,
        Library,
        ModuleXml,
    }

    public class FileDescription
    {
        public string Name { get; set; }
        public EntryFileType FileType { get; set; }
        public byte Module { get; set; }
    }

    public  class RexdeskProjectFile
    {        

        ZipArchive Rpf;

        public string FullFileName { get; set; } = "";

        FileDescription GetFileDescription(string filename)
        {
            if (Path.GetExtension(filename).ToLower() == ".isf")
            {
                return new FileDescription() { Name = filename, FileType = EntryFileType.Library};
            }
            else if (Path.GetExtension(filename).ToLower() == ".xml")
            {
                foreach (var entry in Rpf.Entries)
                {
                    if (entry.FullName == filename)
                    {
                        Stream file = entry.Open();
                        try
                        {
                            byte[] bytes = new byte[1000];
                            file.Read(bytes, 0, bytes.Length);
                            string xmlstring = System.Text.Encoding.Default.GetString(bytes);
                            if (xmlstring.Contains("<REXGENCONFIG "))
                                return new FileDescription() { Name = filename, FileType = EntryFileType.ConfigXml };
                            else if (xmlstring.Contains("<REXMODULE "))
                            {
                                byte.TryParse(xmlstring.Substring(xmlstring.IndexOf("<CAN_BUS>") + 9, 1), out byte can_bus);
                                return new FileDescription() { Name = filename, FileType = EntryFileType.ModuleXml, Module = can_bus };

                            }
                        }
                        finally
                        {
                            file.Close();
                        }
                    }                    
                }
                
            }
            return new FileDescription() { Name = filename, FileType = EntryFileType.General };
        }
   

        public static EntryFileType GetXmlType(string filePath)
        {
            using (FileStream file = new FileStream(filePath, FileMode.Open))
            {
                byte[] bytes = new byte[1000];
                file.Read(bytes, 0, bytes.Length);
                string xmlstring = System.Text.Encoding.Default.GetString(bytes);
                if (xmlstring.Contains("<REXGENCONFIG "))
                    return EntryFileType.ConfigXml;
                else if (xmlstring.Contains("<REXMODULE "))
                {
                    byte.TryParse(xmlstring.Substring(xmlstring.IndexOf("<CAN_BUS>") + 9, 1), out byte can_bus);
                    return EntryFileType.ModuleXml;
                }
                return EntryFileType.General;
            }
            
        }

        public Stream GetFile(EntryFileType fileType, byte module, out string fileName)
        {
            fileName = "";
            if (FullFileName != "")
                try
                {
                    foreach (var item in Rpf.Entries)
                    {
                        var descr = GetFileDescription(item.FullName);
                        if (descr.FileType == fileType)
                        {
                            if (fileType != EntryFileType.ModuleXml || (fileType == EntryFileType.ModuleXml && module == descr.Module))
                            {
                                fileName = item.FullName;
                                return item.Open();
                            }                            
                        }
                    }                     
                }
                catch (Exception)
                {
                    return null;
                }
            return null;
        }

        public EntryFileType GetFileType(string fileName)
        {
            if (FullFileName != "")
                try
                {
                    foreach (var item in Rpf.Entries)
                    {
                        if (item.FullName == fileName)
                        {
                            var descr = GetFileDescription(item.FullName);
                            return descr.FileType;
                        }
                    }
                }
                catch (Exception)
                {
                    return EntryFileType.General;
                }
            return EntryFileType.General; 
        }

        public string GetFileName(EntryFileType fileType, byte module)
        {
            if (FullFileName != "")
                try
                {
                    foreach (var item in Rpf.Entries)
                    {
                        var descr = GetFileDescription(item.FullName);
                        if (descr.FileType == fileType)
                        {
                            if (fileType != EntryFileType.ModuleXml || (fileType == EntryFileType.ModuleXml && module == descr.Module))
                            {
                                return item.FullName; 
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return "";
                }
            return "";
        }

        public Stream GetFile(string fileName)
        {
            if (fileName != "")
                try
                {
                    foreach (var item in Rpf.Entries)
                    {
                        if (item.Name == fileName)
                        {
                            var file = Rpf.GetEntry(item.Name);
                            return file?.Open();
                        }                       
                    }

                }
                catch (Exception)
                {
                    return null;
                }
            return null;
        }

        public bool AddFile(Stream stream, string fileName)
        {
            //EntryFileType fileType = GetFileType(stream, fileName);
            if (fileName != "")
                try
                {
                    if (Rpf.Mode != ZipArchiveMode.Create)
                    {
                        for (int i = 0; i < Rpf.Entries?.Count; i++)
                        {
                            if (Rpf.Entries[i].Name == fileName)
                            {
                                Rpf.Entries[i].Delete();
                                break;
                            }
                        }
                    }                    
                    var entry = Rpf.CreateEntry(fileName);
                    using (Stream entryStream = entry.Open())
                    {
                        stream.CopyTo(entryStream);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            return true;
        }

        public RexdeskProjectFile(string fullFileName)
        {
            FullFileName = fullFileName;
            if (!File.Exists(fullFileName))
                Rpf = ZipFile.Open(fullFileName, ZipArchiveMode.Create);
            else 
                Rpf = ZipFile.Open(fullFileName, ZipArchiveMode.Update);
        }

        public List<FileDescription> GetFilesList()
        {
            var list = new List<FileDescription>();
            foreach (var entry in Rpf.Entries)
            {
                var stream = entry.Open();
                var type = GetFileDescription(entry.FullName);
                list.Add(type);
            }
            return list;
        }

        

        public void Close()
        {   
            Rpf?.Dispose();
        }
    }
}

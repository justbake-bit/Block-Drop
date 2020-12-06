﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using MHLab.Patch.Core.Admin.Exceptions;
using MHLab.Patch.Core.Compressing;
using MHLab.Patch.Core.IO;
using MHLab.Patch.Core.Utilities;

namespace MHLab.Patch.Core.Admin
{
    public sealed class UpdaterBuilder
    {
        private readonly AdminPatcherUpdateContext _context;

        public UpdaterBuilder(AdminPatcherUpdateContext context)
        {
            _context = context;
        }

        public void Build()
        {
            if (DirectoriesManager.IsEmpty(_context.Settings.GetUpdaterFolderPath())) throw new UpdaterFolderIsEmptyException();

            _context.LogProgress(string.Format(_context.LocalizedMessages.UpdaterCollectingOldDefinition));
            var oldDefinition = GetCurrentDefinition();

            _context.LogProgress(string.Format(_context.LocalizedMessages.UpdaterCollectingFiles));
            var files = GetFiles();

            var definition = BuildDefinition(files, oldDefinition);

            FilesManager.Delete(_context.Settings.GetUpdaterIndexPath());

            FilesManager.Delete(_context.Settings.GetUpdaterDeployPath(_context.LauncherArchiveName));

            _context.LogProgress(string.Format(_context.LocalizedMessages.UpdaterCompressingArchive));
            Compressor.Compress(_context.Settings.GetUpdaterFolderPath(), _context.Settings.GetUpdaterDeployPath(_context.LauncherArchiveName), null, _context.CompressionLevel);
            _context.ReportProgress(string.Format(_context.LocalizedMessages.UpdaterCompressedArchive));

            File.WriteAllText(_context.Settings.GetUpdaterIndexPath(), _context.Serializer.Serialize(definition));
            _context.ReportProgress(string.Format(_context.LocalizedMessages.UpdaterSavedDefinition));
        }

        private UpdaterDefinition BuildDefinition(LocalFileInfo[] files, UpdaterDefinition oldDefinition)
        {
            var definition = new UpdaterDefinition();
            var entries = new List<UpdaterDefinitionEntry>();

            for (var i = 0; i < files.Length; i++)
            {
                var currentInfo = files[i];

                var entry = new UpdaterDefinitionEntry();
                entry.RelativePath = currentInfo.RelativePath;
                entry.Attributes = currentInfo.Attributes;
                entry.LastWriting = currentInfo.LastWriting;
                entry.Size = currentInfo.Size;
                entry.Operation = GetOperation(entry, oldDefinition);

                entries.Add(entry);

                _context.ReportProgress(string.Format(_context.LocalizedMessages.UpdaterProcessedFile, currentInfo.RelativePath));
            }

            foreach (var oldDefinitionEntry in oldDefinition.Entries)
            {
                if (entries.All(e => e.RelativePath != oldDefinitionEntry.RelativePath))
                {
                    entries.Add(new UpdaterDefinitionEntry()
                    {
                        RelativePath = oldDefinitionEntry.RelativePath,
                        Operation = PatchOperation.Deleted
                    });
                }

                _context.ReportProgress(string.Format(_context.LocalizedMessages.UpdaterProcessedFile, oldDefinitionEntry.RelativePath));
            }

            definition.Entries = entries.ToArray();
            return definition;
        }

        public int GetCurrentFilesToProcessAmount()
        {
            return FilesManager.GetFiles(_context.Settings.GetUpdaterFolderPath()).Count(f => !f.EndsWith(_context.Settings.UpdaterIndexFileName));
        }

        public string GetCurrentFilesToProcessSize()
        {
            var files = FilesManager.GetFilesInfo(_context.Settings.GetUpdaterFolderPath()).Where(f => !f.RelativePath.EndsWith(_context.Settings.UpdaterIndexFileName));
            long size = 0;

            foreach (var fileInfo in files)
            {
                size += fileInfo.Size;
            }

            return FormatUtility.FormatSizeDecimal(size, 2);
        }

        private LocalFileInfo[] GetFiles()
        {
            var files = FilesManager.GetFilesInfo(_context.Settings.GetUpdaterFolderPath());
            return files.Where(f => !f.RelativePath.EndsWith(_context.Settings.UpdaterIndexFileName)).ToArray();
        }

        private UpdaterDefinition GetCurrentDefinition()
        {
            if (FilesManager.Exists(_context.Settings.GetUpdaterIndexPath())) 
                return _context.Serializer.Deserialize<UpdaterDefinition>(File.ReadAllText(_context.Settings.GetUpdaterIndexPath()));

            return new UpdaterDefinition()
            {
                Entries = new UpdaterDefinitionEntry[0]
            };
        }

        private PatchOperation GetOperation(UpdaterDefinitionEntry current, UpdaterDefinition oldDefinition)
        {
            if (oldDefinition.Entries.All(e => e.RelativePath != current.RelativePath)) return PatchOperation.Added;

            var oldEntry = oldDefinition.Entries.FirstOrDefault(e => e.RelativePath == current.RelativePath);

            if (oldEntry.Size == current.Size)
            {
                if (oldEntry.Attributes == current.Attributes)
                {
                    return PatchOperation.Unchanged;
                }

                return PatchOperation.ChangedAttributes;
            }

            return PatchOperation.Updated;
        }
    }
}

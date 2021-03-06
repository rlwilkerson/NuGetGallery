﻿using Microsoft.WindowsAzure.Storage;
using NuGetGallery.Operations.Common;

namespace NuGetGallery.Operations
{
    [Command("deletepackagefile", "Deletes a specific package file", AltName = "dpf")]
    public class DeletePackageFileTask : PackageVersionTask
    {
        [Option("Storage account in which to place audit records and backups, usually provided by the environment")]
        public CloudStorageAccount BackupStorage { get; set; }

        public override void ValidateArguments()
        {
            base.ValidateArguments();
            ArgCheck.Required(PackageId, "PackageId");
            ArgCheck.Required(PackageVersion, "PackageVersion");
            ArgCheck.Required(PackageHash, "PackageHash");

            if (BackupStorage == null && CurrentEnvironment != null)
            {
                BackupStorage = CurrentEnvironment.BackupStorage;
            }
            ArgCheck.RequiredOrConfig(BackupStorage, "BackupStorage");
        }

        public override void ExecuteCommand()
        {
            new BackupPackageFileTask
                {
                    BackupStorage = BackupStorage,
                    StorageAccount = StorageAccount,
                    PackageId = PackageId,
                    PackageVersion = PackageVersion,
                    PackageHash = PackageHash,
                    WhatIf = WhatIf
                }.ExecuteCommand();

            var blobClient = CreateBlobClient();
            var packagesBlobContainer = Util.GetPackagesBlobContainer(blobClient);
            var packageFileBlob = Util.GetPackageFileBlob(
                packagesBlobContainer,
                PackageId,
                PackageVersion);
            var fileName = Util.GetPackageFileName(
                PackageId,
                PackageVersion);
            if (packageFileBlob.Exists())
            {
                Log.Info("Deleting package file '{0}'.", fileName);
                if (!WhatIf)
                {
                    packageFileBlob.DeleteIfExists();
                }
            }
            else
            {
                Log.Warn("Package file does not exist '{0}'.", fileName);
            }
        }
    }
}

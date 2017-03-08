﻿namespace Cursed.Base

open System
open System.IO
open System.Collections
open Common
open Hopac
open HttpFs.Client

open ICSharpCode.SharpZipLib.Core
open ICSharpCode.SharpZipLib.Zip

module ModpackController =
    let CreateMultiMcInstance minecraftVersion version name author forge =
        [ "InstanceType", "OneSix"
          "IntendedVersion", minecraftVersion
          "LogPrePostOutput", "true"
          "OverrideCommands", "false"
          "OverrideConsole", "false"
          "OverrideJavaArgs", "false"
          "OverrideJavaLocation", "false"
          "OverrideMemory", "false"
          "OverrideWindow", "false"
          "iconKey", "default"
          "lastLaunchTime", "0"
          "name", sprintf "%s %s" name version
          "notes", sprintf "Modpack by %s. Generated by Cursed. Using %s" author forge
          "totalTimePlayed", "0" ]
          |> Map.ofSeq

    let UpdateProgressBarAmount previousState =
        let previousProgress =
            match previousState with
            | Progress numberCompleted -> numberCompleted
            | _ -> 0
        ProgressBarState.Progress (previousProgress + 1)

    let private ofType<'a> (source : System.Collections.IEnumerable) : seq<'a> =
        let resultType = typeof<'a>
        seq {
            for item in source do
                match item with
                    | null -> ()
                    | _ ->
                    if resultType.IsAssignableFrom (item.GetType ())
                    then
                        yield (downcast item)
        }

    let ExtractZip location ((zipName: string), (zipLocation: string)) =
        let modpackSubdirectory = zipName.Substring(0, zipName.LastIndexOf('.'))
        let extractLocation = location @@ modpackSubdirectory @@ "minecraft"

        use zipFile = new ZipFile(zipLocation @@ zipName)

        ofType<ZipEntry> zipFile
        |> Seq.iter (fun zf ->
            try
                let unzipPath = extractLocation @@ zf.Name
                let directoryPath = Path.GetDirectoryName(unzipPath)

                if directoryPath.Length > 0 then
                    Directory.CreateDirectory(directoryPath) |> ignore

                let zipStream = zipFile.GetInputStream(zf)
                let buffer = Array.create 4096 (new Byte())

                use unzippedFileStream = File.Create(unzipPath)
                StreamUtils.Copy(zipStream, unzippedFileStream, buffer)
            with
            | _ -> ()
        )

        let fileInfo = new FileInfo(zipLocation @@ zipName)
        fileInfo.Delete()

        extractLocation

    let rec DirectoryCopy sourcePath destinationPath =
        Directory.CreateDirectory(destinationPath) |> ignore

        let sourceDirectory = new DirectoryInfo(sourcePath)
        sourceDirectory.GetFiles()
        |> Seq.iter (fun f ->
            f.CopyTo(destinationPath @@ f.Name, true) |> ignore
        )

        sourceDirectory.GetDirectories()
        |> Seq.iter (fun d ->
            DirectoryCopy d.FullName (destinationPath @@ d.Name) |> ignore
        )

    let DownloadZip (link: string) location =
        job {
            let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
            let fileUrl = modpackLink + "/files/latest"

            use! response =
                Request.create Get (Uri fileUrl)
                |> getResponse

            let zipName = Uri.UnescapeDataString(response.responseUri.Segments |> Array.last)

            use fileStream = new FileStream(HomePath @@ zipName, FileMode.Create)
            do! response.body.CopyToAsync fileStream |> Job.awaitUnitTask

            return zipName, HomePath
        }
        |> Job.catch
        |> run

    let CreateMultiMc location manifestFile =
        job {
            let directory = new DirectoryInfo(location)
            let outFile = new StreamWriter(directory.Parent.FullName @@ "instance.cfg")

            let manifest = ModpackManifest.Parse(manifestFile)
            let forge = manifest.Minecraft.ModLoaders.[0].Id

            CreateMultiMcInstance manifest.Minecraft.Version manifest.Version manifest.Name manifest.Author forge
            |> Seq.iter (fun setting ->
                outFile.WriteLine(sprintf "%s=%s" setting.Key setting.Value)
            )

            outFile.Flush()

            return forge
        }
        |> run

    let TryFindMod extractLocation fileName =
        let foundMod =
            Directory.GetFiles(extractLocation, fileName, SearchOption.AllDirectories)
        foundMod |> Array.tryHead

﻿namespace Cursed.Base

module ModpackController =
    let CreateMultiMcInstance version name author forge = 
        [ "InstanceType", "OneSix"
          "IntendedVersion", version
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
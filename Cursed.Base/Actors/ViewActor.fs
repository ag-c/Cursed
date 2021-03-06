﻿namespace Cursed.Base

module ViewActor =
    open System

    let UpdateLoop =
        let inboxHandler (inbox: MailboxProcessor<StateReplyMessage>) =
            let rec messageLoop oldState =
                async {
                    let! message = inbox.Receive()

                    match message with
                    | UpdateModpackLink (link, reply) ->
                        let newState = { oldState with ModpackLink = link }
                    
                        reply.Reply newState.ModpackLink
                        return! messageLoop newState
                    | SetExtractLocation (location, reply) ->
                        let newState = { oldState with ExtractLocation = location}
                    
                        reply.Reply newState.ExtractLocation
                        return! messageLoop newState
                    | UpdateProgress (projectId, reply) ->
                        let progress = ModpackController.UpdateProgressBarAmount oldState.ProgressBarState
                        let finishedMod = 
                            oldState.Mods
                            |> List.find (fun m ->
                                m.ProjectId = projectId
                            )

                        let updateMods = 
                            oldState.Mods
                            |> List.map (fun m ->
                                if m = finishedMod then
                                    { finishedMod with Completed = true }
                                else
                                    m
                            )

                        let newState = { oldState with ProgressBarState = progress; Mods = updateMods }

                        reply.Reply (newState.ProgressBarState, newState.Mods)
                        return! messageLoop newState
                    | AddMod (modName, projectId, reply) ->
                        let newState = { oldState with Mods = { Name = modName; Link = String.Empty; ProjectId = projectId; Completed = false } :: oldState.Mods }

                        reply.Reply newState.Mods
                        return! messageLoop newState
                    | UpdateModpackInformation (count, reply) ->
                        let newState = { oldState with ModCount = count; ProgressBarState = Indeterminate }

                        reply.Reply (count, Indeterminate)
                        return! messageLoop newState
                    | FinishDownload reply ->
                        let newState = { oldState with ProgressBarState = Disabled}

                        reply.Reply newState.ProgressBarState
                        return! messageLoop newState
                    | StateReplyMessage.Restart ->
                        return! messageLoop { ModpackLink = String.Empty
                                              ExtractLocation = String.Empty
                                              Mods = []
                                              ModCount = 0
                                              ProgressBarState = Disabled }
                }

            messageLoop { ModpackLink = String.Empty
                          ExtractLocation = String.Empty
                          Mods = []
                          ModCount = 0
                          ProgressBarState = Disabled }

        MailboxProcessor.Start(inboxHandler)
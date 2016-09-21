﻿namespace Cursed.Base

open System
open Eto.Forms
open Eto.Drawing

type MainForm(app: Application) = 
    inherit Form()
    let modpack = new Modpack(app)
    let layout = new TableLayout()

    let urlInputRow =
        let urlInputLabel = new Label(Text = "Curse Modpack URL")
        
        let urlInputTextBox = 
            let textBox = new TextBox()
            let onInput _ = 
                modpack.StateAgent.Post (UpdateModpackLink textBox.Text)
            
            Observable.subscribe onInput textBox.TextChanged |> ignore
            textBox

        let discoverButton = 
            let button = new Button(Text = "Discover")

            let addModpackLinkHandler _ =
                modpack.StateAgent.Post DownloadZip

            Observable.subscribe addModpackLinkHandler button.MouseDown |> ignore
            button

        new TableRow([new TableCell(urlInputLabel); new TableCell(urlInputTextBox, true); new TableCell(discoverButton)])

    let extractLocationRow =
        let extractLocationLabel = 
            let label = new Label()
            label.TextBinding.BindDataContext<Modpack>((fun (m: Modpack) ->
                m.ExtractLocation
            ), DualBindingMode.OneWay) |> ignore
            label
            

        let openSelectFolderButton = 
            let button = new Button(Text = "Extract Location")

            let openSelectFolderHandler _ =
                let folderDialog = new SelectFolderDialog()
                folderDialog.ShowDialog(layout.ParentWindow) |> ignore
                modpack.StateAgent.Post (SetExtractLocation folderDialog.Directory)
            
            Observable.subscribe openSelectFolderHandler button.MouseDown |> ignore

            button
        new TableRow([new TableCell(openSelectFolderButton); new TableCell(extractLocationLabel)])

    let modsListBox =
        let listBox = new ListBox()

        let modLabelList (modpack: Modpack) =
            modpack.Mods
            |> List.map (fun m ->
                new Label(Text=fst m) :> obj
            ) 
        let c = listBox.DataStore
            
        let dataStoreBinding = Binding.Property(fun (lb: ListBox) -> lb.DataStore) 
        let modsBinding = Binding.Property(fun (m: Modpack) -> m.Mods).Convert(fun mods -> mods |> Seq.map (fun m -> new Label(Text = fst m) :> obj))
        listBox.BindDataContext<seq<obj>>(dataStoreBinding, modsBinding) |> ignore
        
        listBox.Height <- 500
        new TableRow([new TableCell(listBox, true)])

    do 
        base.Title <- "Cursed"
        base.ClientSize <- new Size(900, 600)
        
        layout.Padding <- new Padding(10)
        layout.Spacing <- new Size(5, 5)
        layout.Rows.Add(extractLocationRow)
        layout.Rows.Add(urlInputRow)
        layout.Rows.Add(modsListBox)
        layout.Rows.Add(null)

        base.Content <- layout
        base.DataContext <- modpack

        let quitCommand = new Command(MenuText = "Quit")
        quitCommand.Shortcut <- Application.Instance.CommonModifier ||| Keys.Q
        quitCommand.Executed.Add(fun e -> Application.Instance.Quit())

        base.Menu <- new MenuBar()
        let fileItem = new ButtonMenuItem(Text = "&File")
        base.Menu.Items.Add(fileItem)

        base.Menu.ApplicationItems.Add(new ButtonMenuItem(Text = "&Preferences..."))
        base.Menu.QuitItem <- quitCommand.CreateMenuItem()

﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Threading

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Editor
open Microsoft.CodeAnalysis.Formatting
open Microsoft.CodeAnalysis.Host.Mef
open Microsoft.CodeAnalysis.Text

[<Shared>]
[<ExportLanguageService(typeof<ISynchronousIndentationService>, FSharpConstants.FSharpLanguageName)>]
type internal FSharpIndentationService() =

    static member GetDesiredIndentation(sourceText: SourceText, lineNumber: int, tabSize: int): Option<int> =        
        // Match indentation with previous line
        let rec tryFindPreviousNonEmptyLine l =
            if l <= 0 then None
            else
                let previousLine = sourceText.Lines.[l - 1]
                if not (String.IsNullOrEmpty(previousLine.ToString())) then                    
                    Some previousLine
                else
                    tryFindPreviousNonEmptyLine (l - 1)
        // No indentation on the first line of a document
        if lineNumber = 0 then None
        else
            match tryFindPreviousNonEmptyLine lineNumber with
            | None -> Some 0
            | Some previousLine ->          
                let rec loop column spaces =
                    if previousLine.Start + column >= previousLine.End then
                        spaces
                    else 
                        match previousLine.Text.[previousLine.Start + column] with
                        | ' ' -> loop (column + 1) (spaces + 1)
                        | '\t' -> loop (column + 1) (((spaces / tabSize) + 1) * tabSize)
                        | _ -> spaces
                Some (loop 0 0)

    interface ISynchronousIndentationService with
        member this.GetDesiredIndentation(document: Document, lineNumber: int, cancellationToken: CancellationToken): Nullable<IndentationResult> =
            async {
                let! cancellationToken = Async.CancellationToken
                let! sourceText = document.GetTextAsync(cancellationToken) |> Async.AwaitTask
                let! options = document.GetOptionsAsync(cancellationToken) |> Async.AwaitTask
                let tabSize = options.GetOption(FormattingOptions.TabSize, FSharpConstants.FSharpLanguageName)
                 
                return 
                    match FSharpIndentationService.GetDesiredIndentation(sourceText, lineNumber, tabSize) with
                    | None -> Nullable()
                    | Some(indentation) -> Nullable<IndentationResult>(IndentationResult(sourceText.Lines.[lineNumber].Start, indentation))
            } |> (fun c -> Async.RunSynchronously(c,cancellationToken=cancellationToken))

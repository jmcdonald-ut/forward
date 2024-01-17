# Forward (fwd)

Forward is a personal command line tool to help with dev work. This is my first F# project, and I'm using it as a springboard to learn the language.

```
USAGE: fwd [--help] [--project <string>] [--project-path <string>] [--root <string>] [--squelch] [--format <standard|json>]
           <subcommand> [<options>]

SUBCOMMANDS:

    init                  initialize a project.
    explain               explains the current context.
    config, c <options>   gets or sets variables in dotenv file.
    counts <options>      gets other table counts
    backup <options>      backs up a DB.
    backup-all            backs up all DBs.
    restore <options>     restores a DB backup.
    list, ls <options>    list project dotenv files.
    rm <options>          remove project dotenv file.
    switch, s <options>   switch the project's dotenv file.

    Use 'fwd <subcommand> --help' for additional information.

OPTIONS:

    --project <string>    specify the project name.
    --project-path <string>
                          specify the project's directory.
    --root <string>       specify the path to `fwd` artifacts.
    --squelch             squelch errors
    --format <standard|json>
                          format
    --help                display this list of options.
```

## Setup / Build / Release

This requires dotnet. All instructions are written for/on macOS 13.2.1. The [VSCode integration](https://learn.microsoft.com/en-us/dotnet/fsharp/get-started/install-fsharp#install-f-with-visual-studio-code) is superb.

```
# Install tooling/packages
dotnet tool restore
dotnet paket restore

# Clean/build the project
dotnet clean
dotnet build

# Release
dotnet publish src/ForwardCli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
cp src/ForwardCli/bin/Release/net8.0/osx-arm64/publish/ForwardCli [wherever]
```

## Development

```
# Format
dotnet fantomas src/**/*.fs tests/**/*.fs

# Test
dotnet test

# Run w/o full build
dotnet run --project src/ForwardCli -- [command] ...
dotnet run --project src/ForwardCli -- init
dotnet run --project src/ForwardCli -- switch -b new-env
dotnet run --project src/ForwardCli -- list
```

I don't do this as often, but [F# interactive](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/) is handy, too. A killer feature is the ability to install nuget packages in the interactive session.

```
dotnet fsi
> #r "src/Forward/bin/Debug/net8.0/Forward.dll";;
> open Forward;;
> Forward.FileHelpers.getEnvironmentVariableOpt "PWD";;
```

Here's a rundown of the layout:

```
.
├── Forward.sln
├── README.md
├── ...
├── src
│   ├── Forward       # Primary code for app/biz logic.
│   ├── ForwardCli    # Command line interface code for Forward.
│   └── Prelude       # Support for base functions and types.
└── tests
    └── Forward.Tests # Holds tests.
```

## Completions (zsh)

I'm still sorting out how to automate completions setup. Until then, copy `completions/zsh/_fwd` into `$fpath`. Completions are still a WIP as I continue slowly making sense of the [official docs](https://zsh.sourceforge.io/Doc/Release/Completion-System.html).

P.S. I had to add a standalone folder that I stick the completions in (`~/.zsh_completions`) since the file wasn't being picked up if placed in one of the default `$fpath` folders. I'm not sure why this was necessary, I'm likely missing something. This required the extra step of adding this to my `~/.zshrc` **before** calling `compinit`.

```
fpath=($HOME/.zsh_completions $fpath)
```

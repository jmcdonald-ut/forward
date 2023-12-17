# Forward (fwd)

Forward is a personal command line tool to help with dev work. This is my first F# project, and I'm using it as a springboard to learn the language.

```sh
# Initialize a Forward project. This will create or replace
# `.env` with a symlink to the "active" `.env` file now
# managed by Forward.
fwd init

# Use --help for the top-level command or any subcommands
# to get more info.
fwd --help
fwd ls --help

# Explain the context used by Forward commands. Here's a
# brief explanation of each key in the output.
#
# RootPath: Holds all Forward projects.
# ProjectName: Name of the active Forward project.
# ProjectPath: Path to the project codebase.
# DotEnvSymLinkPath: .env in the codebase points to this.
# DotEnvPath: Complete path to active .env
#
# Note that DotEnvSymLinkPath is itself a link.
#
#   ~/rails/.env
#   -> ~/.forward/rails/.env.current
#   -> ~/.forward/rails/dotenvs/.env.development.fresh
fwd explain
#=> { RootPath = "/Users/jon/.forward"
#=>   ProjectName = "rails"
#=>   ProjectPath = "/Users/jon/.forward/rails"
#=>   DotEnvSymLinkPath = Some "/Users/jon/.forward/rails/.env.current"
#=>   DotEnvPath = Some "/Users/jon/.forward/rails/dotenvs/.env.development.fresh" }

# Switch out the .env file used in the current project.
# Pass `-b` to create a new .env file and switch to it.
fwd s -b lots-of-data
fwd s main

# View a list of available .env files
fwd ls

# Remove a .env file that's no longer needed.
fwd rm lots-of-data

# Get the value of an environment variable (taking into
# consideration the current .env file)
fwd config get db_name

# Backup or restore a MySQL DB based on the current .env.
fwd backup
fwd restore
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
dotnet fantomas src/**/*.fs

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
│   └── ForwardCli    # Command line interface code for Forward.
└── tests
    └── Forward.Tests # Holds tests for src/Forward.
```

## Completions (zsh)

I'm still sorting out how to automate completions setup. Until then, copy `completions/zsh/_fwd` into `$fpath`. Completions are still a WIP as I continue slowly making sense of the [official docs](https://zsh.sourceforge.io/Doc/Release/Completion-System.html).

P.S. I had to add a standalone folder that I stick the completions in (`~/.zsh_completions`) since the file wasn't being picked up if placed in one of the default `$fpath` folders. I'm not sure why this was necessary, I'm likely missing something. This required the extra step of adding this to my `~/.zshrc` **before** calling `compinit`.

```
fpath=($HOME/.zsh_completions $fpath)
```

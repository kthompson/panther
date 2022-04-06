#!/bin/bash

# Vars
slndir="$(dirname "${BASH_SOURCE[0]}")/src"

# Restore + Build
dotnet build "$slndir/Panther" --nologo || exit
dotnet build "$slndir/Panther.StdLib" --nologo || exit

# Run
dotnet run -p "$slndir/Panther" --no-build -- "$@"